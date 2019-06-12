using Common;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem;

namespace Tfs2015Client
{
    public class Client : BaseClient
    {
        static TfsTeamProjectCollection project;
        static WorkItemStore store;
        private List<Area> areas;
        private List<Iteration> iterations;
        private List<WorkItemModel> workItems;

        public Client(string serverUrl, string projectName) : base(serverUrl, projectName)
        {
            project = new TfsTeamProjectCollection(Url);
            store = new WorkItemStore(project);

            Initialize();
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
            areas = new List<Area>();
            iterations = new List<Iteration>();
            workItems = new List<WorkItemModel>();

            var workItemLinks = getAllLinks();

            var query = $"SELECT * FROM WorkItems WHERE [Team Project]='{ProjectName}' AND [State] <> 'Removed' AND [State] <> 'Done' AND [State] <> 'Rejected'";
            var res = store.Query(query);

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

            setTasks();
        }

        private static WorkItemLinkInfo[] getAllLinks()
        {
            var queryString = "Select Id From WorkItemLinks WHERE Source.[System.TeamProject] = 'Statoil.Dispatch'";
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

