using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Framework.Client;

namespace Common
{
    public abstract class ImportClient : BaseClient
    {
        public abstract bool Initialize();
        public abstract Dictionary<string, WorkItemModel[]> GetWorkitems();
        public abstract void CreateIterations(IEnumerable<Iteration> iterations);
        public abstract void CreateAreas(IEnumerable<Area> areas);
        public abstract void CreateWorkitems(Dictionary<string, WorkItemModel[]> workitems);
        protected ImportClient(string serverUrl, string projectName)
            : base(serverUrl, projectName) { }
    }
}
