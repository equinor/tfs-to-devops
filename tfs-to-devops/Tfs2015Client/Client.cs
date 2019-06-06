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

            var query = $"SELECT * FROM WorkItems WHERE [Team Project]='{ProjectName}' AND ([Work Item Type]='Product Backlog Item' OR [Work Item Type]='Bug') AND [State] <> 'Removed' AND [State] <> 'Done'";
            var res = store.Query(query);

            foreach (WorkItem workItem in res)
            {
                workItems.Add(new WorkItemModel(workItem));

                var iterationId = workItem.IterationId;
                if(iterations.Any(x => x.IterationId == iterationId) == false)
                    iterations.Add(new Iteration(iterationId, workItem.IterationPath));

                var areaId = workItem.AreaId;
                if (areas.Any(x => x.AreaId == areaId) == false)
                    areas.Add(new Area(areaId, workItem.AreaPath));
            }

            int i = 0;
        }
    }
}

