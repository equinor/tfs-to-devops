using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class BaseClient
    {
        protected Uri Url;
        protected string ProjectName;
        public abstract IEnumerable<Iteration> GetIterations();
        public abstract IEnumerable<Area> GetAreas();
        public abstract bool Initialize();
        public abstract Dictionary<string, WorkItemModel[]> GetWorkitems();
        protected BaseClient(string serverUrl, string projectName)
        {
            Url = new Uri(serverUrl);
            ProjectName = projectName;
        }
    }
}
