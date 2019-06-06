using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;

namespace Tfs2015
{
    public class Client : BaseClient
    {

        public Client(string url) : base(url)
        {
            
        }

        public override IEnumerable<Iteration> GetIterations()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Area> GetAreas()
        {
            var connection = new VssConnection(new Uri(Url), new VssCredentials());
            var client = connection.GetClient<WorkItemTrackingHttpClient>();
            var areas = client.GetClassificationNodeAsync("Statoil.Dispatch", TreeStructureGroup.Areas);

            return null;
        }
    }
}
