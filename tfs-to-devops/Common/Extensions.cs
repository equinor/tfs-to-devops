using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Client;

namespace Common
{
    public class SerializableChange
    {
        public string ChangeType { get; set; }
        public string ServerItem { get; set; }
        public SerializableChange() { }
    }

    public class SerializableChangeset
    {
        public int ChangesetId { get; set; }
        public DateTime CreationDate { get; set; }
        public string ArtifactUri { get; set; }
        public string CheckinNote { get; set; }
        public string Comment { get; set; }
        public string Committer { get; set; }
        public string CommitterDisplayName { get; set; }
        public List<SerializableChange> ChangedFiles { get; set; }
        public SerializableChangeset(Microsoft.TeamFoundation.VersionControl.Client.Changeset c)
        {
            ChangesetId = c.ChangesetId;
            CreationDate = c.CreationDate;
            ArtifactUri = c.ArtifactUri.AbsoluteUri;
            CheckinNote = string.Join(Environment.NewLine, c.CheckinNote.Values.Select(x => $"{x.Name} - {x.Value}"));
            Comment = c.Comment;
            Committer = c.Committer;
            CommitterDisplayName = c.CommitterDisplayName;
            ChangedFiles = new List<SerializableChange>();
            ChangedFiles.AddRange(c.Changes.Select(x => new SerializableChange(){ ChangeType = x.ChangeType.ToString(), ServerItem = x.Item.ServerItem}));
        }

        public SerializableChangeset() { }
    }

    public static class Extensions
    {
        public static string CleanFilename(this string serverPath, string branchName)
        {
            var path = serverPath.Substring(serverPath.LastIndexOf(branchName));
            return path;
        }
        public static void SaveToFile(this Microsoft.TeamFoundation.VersionControl.Client.Changeset c, string outputFile)
        {
            Serialize(c.ToSerializable(), outputFile);
        }

        public static SerializableChangeset ToSerializable(
            this Microsoft.TeamFoundation.VersionControl.Client.Changeset c)
        {
            return new SerializableChangeset(c);
        }

        public static void Serialize<T>(T dataToSerialize, string filePath)
        {
            try
            {
                using (Stream stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    XmlTextWriter writer = new XmlTextWriter(stream, Encoding.Default);
                    writer.Formatting = Formatting.Indented;
                    serializer.Serialize(writer, dataToSerialize);
                    writer.Close();
                }
            }
            catch
            {
                throw;
            }
        }
    }
    
}
