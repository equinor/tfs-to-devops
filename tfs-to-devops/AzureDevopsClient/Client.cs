using Common;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using AttachmentReference = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.AttachmentReference;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

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

        public override void CreateWorkitems(Dictionary<string, WorkItemModel[]> workitems)
        {
            CreateUserStories(workitems["Product Backlog Item"], "User Story");

            CreateUserStories(workitems["Bug"], "Bug");

            var projectId = projectClient.GetProject(ProjectName).Result.Id;
            var fields = workClient.GetFieldsAsync(projectId, GetFieldsExpand.ExtensionFields).Result;

            foreach (var field in fields)
            {
                Logger.Warning($"{field.Name};{field.ReferenceName};{field.Type}");
            }
        }

        private void CreateUserStories(WorkItemModel[] stories, string storyType)
        {
            Logger.Info($"{GetType()} Creating {storyType} entries {Environment.NewLine}");
            
            var projectId = projectClient.GetProject(ProjectName).Result.Id;
            foreach (var workItemModel in stories)
            {
                var patchDocument = workItemModel.ToPatchDocument(ProjectName);
                var newWorkItem = CreateWorkItemInAzure(patchDocument, projectId, storyType);

                if (workItemModel.Tasks.Any() && newWorkItem != null)
                {
                    foreach (var itemModel in workItemModel.Tasks)
                    {
                        var childPatchDocument = itemModel.ToPatchDocument(ProjectName, newWorkItem);
                        var childWorkItem = CreateWorkItemInAzure(childPatchDocument, projectId, "Task");
                        if (childWorkItem == null)
                            continue;
                        CreateWorkItemLinkToParentInAzure(newWorkItem, childWorkItem, projectId);
                    }
                }

                if (workItemModel.HasAttachments && newWorkItem != null)
                {
                    foreach (Attachment attachment in workItemModel.Attachments)
                    {
                        var attachmentRef = CreateAttachmentReference(attachment);
                        if(attachmentRef == null)
                            continue;
                        
                        var attachmentPatch = attachmentRef.ToPatchDocument();
                        UpdateWorkItem(attachmentPatch, projectId, newWorkItem.Id.Value);
                    }
                }

                if(newWorkItem != null)
                    Logger.Info($"{GetType()} Saved {workItemModel.Title} [{newWorkItem.Id}] with {workItemModel.Tasks.Count()} task(s) and {workItemModel.Attachments.Count} attachment(s)'");
            }
        }

        private AttachmentReference CreateAttachmentReference(Attachment attachment)
        {
            var fileName = Path.Combine(Path.GetTempPath(), attachment.Name);
            try
            {
                using (var client = new WebClient())
                {
                    client.UseDefaultCredentials = true;
                    client.DownloadFile(attachment.Uri, fileName);
                }

                var attachmentRef = workClient.CreateAttachmentAsync(fileName).Result;

                File.Delete(fileName);

                return attachmentRef;
            }
            catch (Exception e)
            {
                Logger.Error($"{GetType()} Failed downloading attachment from {attachment.Uri} to {fileName}{Environment.NewLine}\t{e.Message}");
            }

            return null;
        }

        private void CreateWorkItemLinkToParentInAzure(WorkItem parent, WorkItem child, Guid projectId)
        {
            try
            {
                var linkFromParentToChild = child.GetParentLinkPatch();
                UpdateWorkItem(linkFromParentToChild, projectId, parent.Id.Value, false, false);
            }
            catch (Exception e)
            {
                Logger.Error($"{GetType()} Failed saving workitem relation from '{parent.Id}' to '{child.Id}'{Environment.NewLine}\t{e.Message}");
            }
        }

        private WorkItem UpdateWorkItem(JsonPatchDocument patch, Guid projectId, int workItemId, bool validateOnly = false,
            bool ignoreRules = true)
        {
            try
            {
                return workClient.UpdateWorkItemAsync(patch, projectId, workItemId, validateOnly, ignoreRules).Result;
            }
            catch (Exception e)
            {
                var errorMessage = e.Message;
                if (e.InnerException != null && e.InnerException is RuleValidationException)
                {
                    var ruleValidationEx = (RuleValidationException)e.InnerException;
                    errorMessage = string.Join($"{Environment.NewLine}\t",
                        ruleValidationEx.RuleValidationErrors.Select(x => x.ErrorMessage));
                }

                Logger.Error($"{GetType()} Failed patching item with Id '{workItemId}'{Environment.NewLine}\t{errorMessage}");
            }

            return null;
        }

        private WorkItem CreateWorkItemInAzure(JsonPatchDocument patchDocument, Guid projectId, string workItemType, bool validateOnly = false,
            bool ignoreRules = true)
        {
            try
            {
                var res = workClient.CreateWorkItemAsync(patchDocument, projectId, workItemType, validateOnly, ignoreRules).Result;
                return res;
            }
            catch (Exception e)
            {
                var errorMessage = e.Message;
                if (e.InnerException != null && e.InnerException is RuleValidationException)
                {
                    var ruleValidationEx = (RuleValidationException) e.InnerException;
                    errorMessage = string.Join($"{Environment.NewLine}\t",
                        ruleValidationEx.RuleValidationErrors.Select(x => x.ErrorMessage));
                }

                Logger.Error($"{GetType()} Failed creating {workItemType} {patchDocument.FirstOrDefault(x => x.Path.Contains("System.Title"))?.Value}'{Environment.NewLine}\t{errorMessage}");
            }

            return null;
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
                                try
                                {
                                    UpdateWithChildNode(ref previousRoot, splitPath[i], nodeType);
                                    rootArea = previousRoot.Children.SingleOrDefault(x => x.Name == splitPath[i]);
                                }
                                catch (Exception e)
                                {
                                    Logger.Error($"{GetType()} Failed creating child area, error message below:{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}");
                                    Logger.Error($"{GetType()} Try to create the area manually in Azure and run the export again");
                                }
                            }
                        }

                        UpdateWithChildNode(ref rootArea, pathEnd, nodeType);
                    }
                }
            }

            foreach (var classificationNode in azureClassificationNodes)
            {
                var treeStructureGroup = TreeStructureGroup.Iterations;
                if (nodeType == TreeNodeStructureType.Area)
                    treeStructureGroup = TreeStructureGroup.Areas;

                SaveClassificationNodesRecursive(projectId, classificationNode, treeStructureGroup);
            }
        }

        private WorkItemClassificationNode UpdateWithChildNode(ref WorkItemClassificationNode parent, string childName,
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

        private void SaveClassificationNodesRecursive(Guid projectId, WorkItemClassificationNode classificationNode, TreeStructureGroup structureGroup, string pathToParent = null)
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
                SaveClassificationNodesRecursive(projectId, classificationNodeChild, structureGroup, newPathToParent);
            }
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
