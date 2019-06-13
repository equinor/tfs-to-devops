using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class ExportClient : BaseClient
    {   
        public abstract IEnumerable<Iteration> GetIterations();
        public abstract IEnumerable<Area> GetAreas();
        public abstract bool Initialize();
        public abstract Dictionary<string, WorkItemModel[]> GetWorkitems();
        public abstract void ExportHistory(string branchPath, DateTime dateFrom, DateTime dateTo, string path);
        protected ExportClient(string serverUrl, string projectName) 
            : base(serverUrl, projectName) { }
    }
}
