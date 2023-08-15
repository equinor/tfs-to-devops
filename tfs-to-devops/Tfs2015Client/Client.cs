using Common;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Diff;
using Microsoft.TeamFoundation.VersionControl.Client;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem;

namespace Tfs2015Client
{
    public class Client : ExportClient
    {
        static TfsTeamProjectCollection project;
        static WorkItemStore store;
        private List<Area> areas;
        private List<Iteration> iterations;
        private List<WorkItemModel> workItems;

        public Client(string serverUrl, string projectName) : base(serverUrl, projectName)
        {
            
        }

        public sealed override bool Initialize()
        {   
            InitializeRemote();
            return true;
        }

        public override Dictionary<string, WorkItemModel[]> GetWorkitems()
        {
            return workItems.Where(i => i.Title != "Standard user story").GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.ToArray());
        }

        public override void ExportHistory(string branchPath, DateTime dateFrom, DateTime dateTo, string path)
        {
            Logger.Info($"{this.GetType()} fetching changesets");

            path = Path.Combine(path, branchPath);

            var versionService = project.GetService<VersionControlServer>();
            var qparms = new QueryHistoryParameters($"$/{ProjectName}/{branchPath}", RecursionType.Full)
            {
                VersionStart = new DateVersionSpec(dateFrom),
                VersionEnd = new DateVersionSpec(dateTo),
                SlotMode = false,
                IncludeChanges = true
            };

            var changesets = versionService.QueryHistory(qparms)?.OrderBy(x => x.CreationDate).ToList();

            if (changesets == null)
            {
                Logger.Error($"{this.GetType()} unable to get changesets, aborting");
                return;
            }

            Logger.Info($"{this.GetType()} found {changesets.Count()} changesets");

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Logger.Info($"{this.GetType()} creating output folder (deleting of not empty)"); 
            Directory.CreateDirectory(path);

            foreach (var changeset in changesets)
            {
                Logger.Info($"{this.GetType()} fetching details for {changeset.ChangesetId}");
                var changeDir = Path.Combine(path, $"{changeset.CreationDate:yyy-MM-dd} - {changeset.ChangesetId}");

                Directory.CreateDirectory(changeDir);
                Thread.Sleep(100);

                changeset.SaveToFile(Path.Combine(changeDir, changeset.ChangesetId.ToString() + ".xml"));

                var changes = versionService.GetChangesForChangeset(changeset.ChangesetId, true, Int32.MaxValue, null, null)?.Where(c =>
                    c.Item.ItemType == ItemType.File).ToList();
                Logger.Info($"{this.GetType()} \tdownloading {changes?.Count() ?? 0} changed file(s) including previous version...");

                if (changes == null || changes?.Any() == false)
                    continue;

                Parallel.ForEach(changes, (change) =>
                {
                    var changesetlist = (IEnumerable<Changeset>)versionService.QueryHistory(change.Item.ServerItem, VersionSpec.Latest, 0,
                        RecursionType.None, null, null, new ChangesetVersionSpec(change.Item.ChangesetId), int.MaxValue, true, false);

                    changesetlist = changesetlist.OrderByDescending(x => x.ChangesetId).ToList();

                    if (!change.Item.ServerItem.Contains(branchPath))
                        return;

                    var filePath = Path.Combine(changeDir, change.Item.ServerItem.CleanFilename(branchPath));
                    var fileName = Path.GetFileName(filePath);

                    for (var i = 0; i < (changesetlist.Count() > 2 ? 2 : changesetlist.Count()); i++)
                    {
                        var hist = changesetlist.ElementAt(i);
                        var historicChanges = hist.Changes.Where(x => x.Item.ServerItem.Contains(fileName)).ToList();
                        if (!historicChanges.Any()) continue;

                        var historicChange = historicChanges.First();
                        var historicFilePath = filePath + "." + historicChange.Item.ChangesetId;
                        historicChange.Item.DownloadFile(historicFilePath);
                    }
                });
            }
        }

        public override bool Connect()
        {
            try
            {
                project = new TfsTeamProjectCollection(Url);
                store = new WorkItemStore(project);

                Logger.Info($"{this.GetType()} connected {store != null}");

                return store != null;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            return false;
        }

        public override IEnumerable<Iteration> GetIterations()
        {
            return iterations;
        }

        public override IEnumerable<Area> GetAreas()
        {
            return areas;
        }

        private void InitializeRemote()
        {
            Logger.Info($"{this.GetType()} reading from server");
            areas = new List<Area>();
            iterations = new List<Iteration>();
            workItems = new List<WorkItemModel>();

            var workItemLinks = getAllLinks();
            
            var query = $"SELECT * FROM WorkItems WHERE [Team Project]='{ProjectName}' AND [State] <> 'Removed' AND [State] <> 'Done' AND [State] <> 'Rejected'";
            var res = store.Query(query);

            Logger.Info($"{this.GetType()} Found {res.Count} workitems");
            Logger.Info($"{this.GetType()} Found {workItemLinks.Length} links");

            var sourceTable = workItemLinks.GroupBy(l => l.SourceId).ToDictionary(g => g.Key, g => g.ToArray());
            var targetTable = workItemLinks.GroupBy(l => l.TargetId).ToDictionary(g => g.Key, g => g.ToArray());

            foreach (WorkItem item in res)
            {
                var sourceLinks = sourceTable.ContainsKey(item.Id) ? sourceTable[item.Id] : Enumerable.Empty<WorkItemLinkInfo>();
                var targetLinks = targetTable.ContainsKey(item.Id) ? targetTable[item.Id] : Enumerable.Empty<WorkItemLinkInfo>();

                workItems.Add(new WorkItemModel(item, sourceLinks, targetLinks));

                var iterationId = item.IterationId;
                if (iterations.Any(x => x.IterationId == iterationId) == false)
                    iterations.Add(new Iteration(iterationId, item.IterationPath));

                var areaId = item.AreaId;
                if (areas.Any(x => x.AreaId == areaId) == false)
                    areas.Add(new Area(areaId, item.AreaPath));
            }

            Logger.Info($"{this.GetType()} Found {areas.Count} areas");
            Logger.Info($"{this.GetType()} Found {iterations.Count} iterations");
            setTasks();
        }

        private WorkItemLinkInfo[] getAllLinks()
        {
            var queryString = $"Select Id From WorkItemLinks WHERE Source.[System.TeamProject] = '{ProjectName}'";
            var query = new Query(store, queryString);

            return query.RunLinkQuery();
        }

        private void setTasks()
        {
            var grouped = workItems.Where(i => i.Title != "Standard user story").GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.ToArray());
            var ProductBacklogItems = grouped.ContainsKey("Product Backlog Item")
                ? grouped["Product Backlog Item"]
                : Enumerable.Empty<WorkItemModel>();

            var Bugs = grouped.ContainsKey("Bug")
                ? grouped["Bug"]
                : Enumerable.Empty<WorkItemModel>();

            var Tasks = grouped.ContainsKey("Task")
                ? grouped["Task"]
                : Enumerable.Empty<WorkItemModel>();

            foreach (var item in ProductBacklogItems)
            {
                var taskIds = item.Sources.Where(t => t.LinkTypeId == 2).Select(l => l.TargetId).ToArray();
                var storyTasks = Tasks.Where(t => taskIds.Contains(t.Id)).ToArray();

                var testIds = item.Sources.Where(t => t.LinkTypeId == 5).Select(l => l.TargetId).ToArray();

                var testTasks = grouped["Test Case"].Where(t => testIds.Contains(t.Id)).ToArray();

                item.SetTasks(storyTasks, testTasks);
            }

            foreach (var item in Bugs)
            {
                var taskIds = item.Sources.Where(t => t.LinkTypeId == 2).Select(l => l.TargetId).ToArray();
                var storyTasks = Tasks.Where(t => taskIds.Contains(t.Id)).ToArray();

                var testIds = item.Sources.Where(t => t.LinkTypeId == 5).Select(l => l.TargetId).ToArray();
                var testTasks = grouped["Test Case"].Where(t => testIds.Contains(t.Id)).ToArray();

                item.SetTasks(storyTasks, testTasks);
            }
        }
    }
}

