using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevopsClient
{
    public class Client : ImportClient
    {
        private VssClientCredentials credentials;
        private VssConnection connection;
        private ProjectHttpClient projectClient;
        private TeamHttpClient teamClient;
        private WorkItemTrackingHttpClient workClient;

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

        public override void CreateIterations(IEnumerable<Iteration> iterations)
        {
            
        }

        public override void CreateAreas(IEnumerable<Area> areas)
        {
            var projectId = projectClient.GetProject(ProjectName).Result.Id;
            var groups = areas.GroupBy(x => x.AreaPath.Count(y => y == '\\')).ToDictionary(d => d.Key, d => d.ToList()).OrderBy(d => d.Key);
            var azureAreas = new List<WorkItemClassificationNode>();

            foreach (var areaGroup in groups)
            {
                if (areaGroup.Key == 0)
                {
                    // Root nodes
                    foreach (var rootArea in areaGroup.Value)
                    {
                        azureAreas.Add(new WorkItemClassificationNode()
                        {
                            Name = rootArea.AreaPath,
                            StructureType = TreeNodeStructureType.Area
                        });
                    }
                }
                else
                {
                    // These are child nodes
                    foreach (var childArea in areaGroup.Value)
                    {
                        // Split the path and start finding out where to put the area
                        var splitPath = childArea.AreaPath.Split('\\');
                        var pathEnd = splitPath[splitPath.Length - 1];
                        var pathStart = splitPath[0];

                        WorkItemClassificationNode rootArea = azureAreas.Single(x => x.Name == pathStart);

                        for (int i = 1; i < splitPath.Length - 1; i++)
                        {
                            rootArea = rootArea.Children.Single(x => x.Name == splitPath[i]);
                        }

                        var children = rootArea.Children?.ToList() ?? new List<WorkItemClassificationNode>();
                        children.Add(new WorkItemClassificationNode()
                        {
                            Name = pathEnd,
                            StructureType = TreeNodeStructureType.Area
                        });

                        rootArea.Children = children.ToArray();
                        rootArea.HasChildren = true;
                    }
                }
            }

            foreach (var classificationNode in azureAreas)
            {
                saveClassificationNodesRecursive(projectId, classificationNode, TreeStructureGroup.Areas);
            }
        }

        private void saveClassificationNodesRecursive(Guid projectId, WorkItemClassificationNode classificationNode, TreeStructureGroup structureGroup, string pathToParent = null)
        {
            try
            {
                var result = workClient.CreateOrUpdateClassificationNodeAsync(
                    classificationNode,
                    projectId.ToString(),
                    structureGroup,
                    pathToParent).Result;
                
                Logger.Info($"{GetType()} Saved type {structureGroup} '{pathToParent}\\{classificationNode.Name}'");
            }
            catch (Exception)
            {
                // Intentionally eating the e
                Logger.Error($"{GetType()} Failed saving type {structureGroup} '{pathToParent}\\{classificationNode.Name}', does it already exist?");
            }

            if (classificationNode.HasChildren != true) return;

            var newPathToParent = $"{pathToParent}\\{classificationNode.Name}";
            foreach (var classificationNodeChild in classificationNode.Children)
            {
                saveClassificationNodesRecursive(projectId, classificationNodeChild, structureGroup, newPathToParent);
            }
        }

        public override void CreateWorkitems(IEnumerable<WorkItemModel> workitems)
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

                teamClient = connection.GetClient<TeamHttpClient>();
                projectClient = connection.GetClient<ProjectHttpClient>();
                workClient = connection.GetClient<WorkItemTrackingHttpClient>();


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
