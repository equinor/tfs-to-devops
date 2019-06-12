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

        public override IEnumerable<WorkItemModel> GetWorkitems()
        {
            return workItems;
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

            var query = $"SELECT * FROM WorkItems WHERE [Team Project]='{ProjectName}' AND ([Work Item Type]='Product Backlog Item' OR [Work Item Type]='Bug') AND [State] <> 'Removed' AND [State] <> 'Done'";
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

            int i = 0;
        }

        private static WorkItemLinkInfo[] getAllLinks()
        {
            var queryString = "Select Id From WorkItemLinks WHERE Source.[System.TeamProject] = 'Statoil.Dispatch'";
            var query = new Query(store, queryString);

            return query.RunLinkQuery();
        }
    }
}

