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
            var groups = iterations.GroupBy(x => x.IterationPath.Count(y => y == '\\')).ToDictionary(d => d.Key, d => d.Select(x => x.IterationPath).OrderBy(x => x).ToList()).OrderBy(d => d.Key);
            CreateAndSaveClassificationNodes(groups, TreeNodeStructureType.Iteration);
        }

        public override void CreateAreas(IEnumerable<Area> areas)
        {
            var groups = areas.GroupBy(x => x.AreaPath.Count(y => y == '\\')).ToDictionary(d => d.Key, d => d.Select(x => x.AreaPath).OrderBy(x => x).ToList()).OrderBy(d => d.Key);
            CreateAndSaveClassificationNodes(groups, TreeNodeStructureType.Area);
        }

        private void CreateAndSaveClassificationNodes(IOrderedEnumerable<KeyValuePair<int, List<string>>> groups, TreeNodeStructureType nodeType)
        {
            var azureClassificationNodes = new List<WorkItemClassificationNode>();
            var projectId = projectClient.GetProject(ProjectName).Result.Id;
            foreach (var nodeGroup in groups)
            {
                if (nodeGroup.Key == 0)
                {
                    // Root nodes
                    foreach (var rootArea in nodeGroup.Value)
                    {
                        azureClassificationNodes.Add(new WorkItemClassificationNode()
                        {
                            Name = rootArea,
                            StructureType = nodeType
                        });
                    }
                }
                else
                {
                    // These are child nodes
                    foreach (var childArea in nodeGroup.Value)
                    {
                        // Split the path and start finding out where to put the area
                        var splitPath = childArea.Split('\\');
                        var pathEnd = splitPath[splitPath.Length - 1];
                        var pathStart = splitPath[0];

                        var rootArea = azureClassificationNodes.SingleOrDefault(x => x.Name == pathStart);
                        if (rootArea == null)
                        {
                            Logger.Warning($"{GetType()} Root area not found, creating it anyway {pathStart}");
                            azureClassificationNodes.Add(new WorkItemClassificationNode()
                            {
                                Name = pathStart,
                                StructureType = nodeType
                            });

                            rootArea = azureClassificationNodes.SingleOrDefault(x => x.Name == pathStart);
                        }

                        for (int i = 1; i < splitPath.Length - 1; i++)
                        {
                            rootArea = rootArea.Children.SingleOrDefault(x => x.Name == splitPath[i]);
                            if (rootArea == null)
                            {
                                var previousRoot =
                                    azureClassificationNodes.SingleOrDefault(x => x.Name == splitPath[i - 1]);

                                Logger.Warning($"{GetType()} Child area not found, creating {splitPath[i]} under {previousRoot?.Name}");

                                updateWithChildNode(ref previousRoot, splitPath[i], nodeType);
                                rootArea = previousRoot.Children.SingleOrDefault(x => x.Name == splitPath[i]);
                            }
                        }

                        updateWithChildNode(ref rootArea, pathEnd, nodeType);
                        //var children = rootArea.Children?.ToList() ?? new List<WorkItemClassificationNode>();
                        //children.Add(new WorkItemClassificationNode()
                        //{
                        //    Name = pathEnd,
                        //    StructureType = nodeType
                        //});

                        //rootArea.Children = children.ToArray();
                        //rootArea.HasChildren = true;
                    }
                }
            }

            foreach (var classificationNode in azureClassificationNodes)
            {
                var treeStructureGroup = TreeStructureGroup.Iterations;
                if (nodeType == TreeNodeStructureType.Area)
                    treeStructureGroup = TreeStructureGroup.Areas;

                saveClassificationNodesRecursive(projectId, classificationNode, treeStructureGroup);
            }
        }

        private WorkItemClassificationNode updateWithChildNode(ref WorkItemClassificationNode parent, string childName,
            TreeNodeStructureType childType)
        {
            var children = parent.Children?.ToList() ?? new List<WorkItemClassificationNode>();
            children.Add(new WorkItemClassificationNode()
            {
                Name = childName,
                StructureType = childType
            });

            parent.Children = children.ToArray();
            parent.HasChildren = true;

            return parent;
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
