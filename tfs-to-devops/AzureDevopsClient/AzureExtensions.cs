using Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;

namespace AzureDevopsClient
{
    public enum WorkItemType
    {
        Bug,
        Task,
        UserStory
    };

    public static class AzureExtensions
    {
        public static JsonPatchDocument GetParentLinkPatch(this WorkItem workItem)
        {
            var patchDocument = new JsonPatchDocument
            {
                // Relation from Parent -> Child
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Forward",
                        url = workItem.Url,
                        attributes = new
                        {
                            comment = "Child user story"
                        }
                    }
                }
            };

            return patchDocument;
        }

        public static JsonPatchDocument ToPatchDocument(this AttachmentReference attachment)
        {
            var patchDocument = new JsonPatchDocument()
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "AttachedFile",
                        url = attachment.Url,
                        attributes = new
                        {
                            comment = $"Ported by tfs-to-devops @ {DateTime.Now:dd-MMM-yyyy HH:mm:ss}"
                        }
                    }
                }
            };

            return patchDocument;
        }

        public static JsonPatchDocument ToPatchDocument(this WorkItemModel model, string ProjectName, WorkItem parent = null)
        {
            var workItemType = "User Story";
            if (model.Type == "Bug")
                workItemType = "Bug";
            if (parent != null)
                workItemType = "Task";

            var patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path = "/fields/System.Title", Value = model.Title
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.AreaPath", Value = $"{ProjectName}\\{model.AreaPath}"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.IterationPath", Value = $"{ProjectName}\\{model.IterationPath}"
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.WorkItemType", Value = workItemType
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.State", Value = model.State
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.AssignedTo", Value = model.AssignedTo
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.CreatedBy", Value = model.CreatedBy
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.CreatedDate", Value = model.CreatedDate
                },
                new JsonPatchOperation()
                {
                    Operation = Operation.Add, Path="/fields/System.Tags", Value = model.Tags
                }
            };

            if (model.Type == "Bug")
            {
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.TCM.ReproSteps",
                    Value =
                            $"<pre>Ported by tfs-to-devops @ {DateTime.Now:dd-MMM-yyyy HH:mm:ss}{Environment.NewLine}</pre><p><hr></p>{model.HtmlDescription}"
                });
            }
            else if (model.Type == "Product Backlog Item")
            {
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value =
                        $"<pre>Ported by tfs-to-devops @ {DateTime.Now:dd-MMM-yyyy HH:mm:ss}{Environment.NewLine}</pre><p><hr></p>{model.HtmlDescription}"
                });
            }
            //else if (model.Type == "Test Case")
            //{
            //    patchDocument.Add(new JsonPatchOperation()
            //    {
            //        Operation = Operation.Add,
            //        Path = "/fields/Microsoft.VSTS.TCM.Steps",
            //        Value =
            //            $"<pre>Ported by tfs-to-devops @ {DateTime.Now:dd-MMM-yyyy HH:mm:ss}{Environment.NewLine}</pre><p><hr></p>{model.HtmlDescription}"
            //    });
            //}
            if (parent != null)
            {
                // Relation from Child -> Parent
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = parent.Url,
                        attributes = new
                        {
                            comment = "Parent user story"
                        }
                    }
                });
            }

            return patchDocument;
        }
    }
}
