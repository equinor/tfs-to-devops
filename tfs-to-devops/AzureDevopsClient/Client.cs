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
            //var project = projectClient.GetProject(ProjectName).Result;
            //var workClient = connection.GetClient<WorkHttpClient>();
            //workClient.PostTeamIterationAsync(new TeamSettingsIteration() {Name = "Bjarte"} , )
            //var teamSettings = workClient.GetTeamSettingsAsync(new TeamContext(project.Id, project.DefaultTeam.Id)).Result;
            
            return true;
        }

        public override Dictionary<string, WorkItemModel[]> GetWorkitems()
        {
            throw new NotImplementedException();
        }

        public override void CreateIterations(IEnumerable<Iteration> iterations)
        {
            throw new NotImplementedException();
        }
        List<AzureAreaWrapper> azureAreas = new List<AzureAreaWrapper>();
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

                        Logger.Info($"{GetType()} Added root area {rootArea}");
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
                            StructureType =  TreeNodeStructureType.Area
                        });

                        Logger.Info($"{GetType()} found area {childArea.AreaPath}");
                        rootArea.Children = children.ToArray();
                        rootArea.HasChildren = true;
                    }
                }
            }

            foreach (var classificationNode in azureAreas)
            {
                saveClassificationNodesRecursive(projectId, classificationNode);



                //try
                //{
                //    var result = workClient.CreateOrUpdateClassificationNodeAsync(
                //        classificationNode,
                //        projectId.ToString(),
                //        TreeStructureGroup.Areas).Result;

                //    if (result != null)
                //        Logger.Info($"{GetType()} Saved all areas...");
                //    else
                //    {
                //        Logger.Error($"{GetType()} Something went wrong");
                //    }
                //}
                //catch (Exception e)
                //{
                //    Logger.Error($"{GetType()} Failed saving areas: {e.Message}");
                //}
            }
            return;
        }

        private void saveClassificationNodesRecursive(Guid projectId, WorkItemClassificationNode classificationNode, string pathToParent = null)
        {
            try
            {
                var result = workClient.CreateOrUpdateClassificationNodeAsync(
                    classificationNode,
                    projectId.ToString(),
                    TreeStructureGroup.Areas,
                    pathToParent).Result;

                if (classificationNode.HasChildren == true)
                {
                    var newPathToParent = $"{pathToParent}\\{classificationNode.Name}";
                    foreach (var classificationNodeChild in classificationNode.Children)
                    {
                        saveClassificationNodesRecursive(projectId, classificationNodeChild, newPathToParent);
                    }
                }
            } 
            catch (Exception e)
            {
                Logger.Error($"{GetType()} Failed saving areas: {e.Message}");
            }
        }
        private void addChildToExistingArea(AzureAreaWrapper existingArea, Area area)
        {
            //var parent = azureAreas.FirstOrDefault(x => x.AreaPathFromTfs == existingArea.AreaPathFromTfs);
            //var children = parent.AreaNode.Children ?? new List<WorkItemClassificationNode>();
            //children.AddRange()


        }

        private WorkItemClassificationNode createAreaRecursive(List<string> areaPaths, WorkItemClassificationNode parentNode, Guid projectId)
        {
            

            //if (azureAreas.Contains(x => x.AreaPathFromTfs == ""))
            //{
                
            //}



            var node = new WorkItemClassificationNode();
            var isAddingChildArea = false;
            if (parentNode == null)
            {
                node = new WorkItemClassificationNode()
                {
                    Name = areaPaths[0],
                    StructureType = TreeNodeStructureType.Area
                };
            }
            else
            {
                node = parentNode;
                var existingChildren = node.Children?.ToList() ?? new List<WorkItemClassificationNode>();
                existingChildren.Add(new WorkItemClassificationNode()
                {
                    Name = areaPaths[0],
                    StructureType = TreeNodeStructureType.Area
                });

                node.Children = existingChildren;
                isAddingChildArea = true;
            }

            var newParent = getOrCreateArea(node, projectId, isAddingChildArea);

            if (areaPaths.Count > 1)
            {
                var newPaths = areaPaths.GetRange(1, areaPaths.Count - 1);
                return createAreaRecursive(newPaths, newParent, projectId);
            }

            return newParent;
        }

        private WorkItemClassificationNode getAreaNode(string nodeName, Guid projectId)
        {
            var result = workClient.GetClassificationNodeAsync(projectId, TreeStructureGroup.Areas, nodeName).Result;
            if (result == null)
            {
                return getOrCreateArea(
                    new WorkItemClassificationNode() {Name = nodeName, StructureType = TreeNodeStructureType.Area},
                    projectId);
            }

            return result;
        }


        private WorkItemClassificationNode getOrCreateArea(WorkItemClassificationNode classificationNode, Guid projectId, bool isChildArea = false)
        {
            if (!isChildArea)
            {
                try
                {
                    var existingNode = workClient.GetClassificationNodeAsync(projectId, TreeStructureGroup.Areas, classificationNode.Name)?.Result;
                    if (existingNode != null)
                        return existingNode;
                }
                catch (Exception)
                {
                    // Intentionally empty
                }
            }

            var result = workClient.CreateOrUpdateClassificationNodeAsync( 
                classificationNode,
                projectId.ToString(),
                TreeStructureGroup.Areas).Result;

            Logger.Info(isChildArea
                ? $"{GetType()} Added project area {result.Name}\\{classificationNode.Children.Last().Name}"
                : $"{GetType()} Created project area {result.Name}");
            
            return result;
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
