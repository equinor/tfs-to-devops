using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Common
{
    [DebuggerDisplay("{Id} [{Type}] [{State}] {Title}")]
    public class WorkItemModel
    {
        private readonly List<string> KnownTextExtensions = new List<string>()
        {
            ".SHTML",
            ".ASP",
            ".BSH",
            ".HPP",
            ".CXX",
            ".CS",
            ".CSS",
            ".JAVA",
            ".PL",
            ".PM",
            ".PHP",
            ".PHP3",
            ".RC",
            ".VB",
            ".XML",
            ".INI",
            ".AU3",
            ".TXT",
            ".SH",
            ".C",
            ".H",
            ".CPP",
            ".HTM",
            ".HTML",
            ".JS",
            ".VBS",
            ".LOG",
            ".BAT",
            ".NFO",
            ".SQL",
            ".TRG",
            ".PKB",
            ".PKS"
        };


        public IEnumerable<WorkItemLinkInfo> Targets;
        public IEnumerable<WorkItemLinkInfo> Sources;
        public IEnumerable<WorkItemModel> Tasks { get; set; }
        public IEnumerable<WorkItemModel> Tests { get; set; }

        public string AreaPath => WorkItem?.AreaPath;
        public string IterationPath => WorkItem?.IterationPath;

        public string WorkItemTags => WorkItem.Tags;
        public string Tags { get; set; }
        private WorkItem _workItem;
        private WorkItem WorkItem
        {
            get => _workItem;
            set
            {
                _workItem?.Close();
                _workItem = value;
                InitializeProperties();
            }
        }
        public int Id { get; private set; }
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ChangedBy { get; set; }

        public DateTime ChangedDate { get; private set; }

        public string Title { get; private set; }

        public string NodeName { get; private set; }

        private string _assignedTo;
        public string AssignedTo
        {
            get => _assignedTo ?? (_assignedTo = WorkItem?.Fields["Assigned To"].Value.ToString());
            private set => _assignedTo = value;
        }

        public string State { get; private set; }
        public double? RemainingWork { get; private set; }

        public int? BusinessValue { get; set; }

        public long BacklogPriority { get; private set; }

        public string Type => WorkItem?.Type.Name;
        public string InitialHtmlDescription { get; private set; }

        public string HtmlDescription { get; set; }

        public bool HasAttachments => Attachments.Count > 0;
        public AttachmentCollection Attachments { get; private set; }

        private bool HasWorkItem => WorkItem != null;

        public WorkItemModel(WorkItem workItem)
        {
            WorkItem = workItem;
        }

        private void InitializeProperties()
        {
            Id = WorkItem.Id;
            Title = WorkItem.Title;
            CreatedBy = WorkItem.CreatedBy;
            CreatedDate = WorkItem.CreatedDate;
            ChangedBy = WorkItem.ChangedBy;

            if (ChangedDate < WorkItem.ChangedDate) // ChangedDate may be set indirectly by children
                ChangedDate = WorkItem.ChangedDate;

            NodeName = WorkItem.NodeName;
            AssignedTo = WorkItem.Fields["Assigned To"].Value.ToString();
            State = WorkItem.State;
            Tags = WorkItem.Tags;
            Attachments = WorkItem.Attachments;

            if (WorkItem.Fields.Contains("Backlog Priority"))
                BacklogPriority = Convert.ToInt64(WorkItem.Fields["Backlog Priority"].Value);
            else
                BacklogPriority = 0;

            if (WorkItem.Fields.Contains("Description HTML"))
                HtmlDescription = WorkItem.Fields["Description HTML"].Value.ToString();
            else if (WorkItem.Fields.Contains("Repro Steps"))
                HtmlDescription = WorkItem.Fields["Repro Steps"].Value.ToString();
            else
                HtmlDescription = null;

            if (WorkItem.Fields.Contains("Remaining Work"))
                RemainingWork = (double?)WorkItem.Fields["Remaining Work"].Value;
            else
                RemainingWork = null;
        }

        public override bool Equals(object obj)
        {
            var other = obj as WorkItemModel;

            if (other == null)
                return false;

            return Id == other.Id
                && ChangedDate == other.ChangedDate;
        }

        public override int GetHashCode()
        {
            return new
            {
                Id,
                ChangedDate
            }.GetHashCode();
        }
    }
}

