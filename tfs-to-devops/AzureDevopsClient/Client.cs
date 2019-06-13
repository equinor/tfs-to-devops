using System;
using System.Collections.Generic;
using Common;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevopsClient
{
    public class Client : ImportClient
    {
        private VssClientCredentials credentials;
        private VssConnection connection;
        public Client(string serverUrl, string projectName) : base(serverUrl, projectName)
        {

        }

        public override bool Initialize()
        {
            return true;
        }

        public override Dictionary<string, WorkItemModel[]> GetWorkitems()
        {
            throw new NotImplementedException();
        }

        public override void AddIteration(Iteration iteration)
        {
            throw new NotImplementedException();
        }

        public override void AddArea(Area area)
        {
            throw new NotImplementedException();
        }

        public override void AddWorkitems(IEnumerable<WorkItemModel> workitems)
        {
            throw new NotImplementedException();
        }

        public override bool Connect()
        {
            try
            {
                credentials = new VssClientCredentials();
                credentials.Storage = new VssClientCredentialStorage();
                connection = new VssConnection(Url, credentials);
                
                Logger.Info($"{this.GetType()} connected, authenticated: {connection.HasAuthenticated}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            return false;
        }
    }
}
