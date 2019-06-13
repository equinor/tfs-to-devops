using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class BaseClient
    {
        protected Uri Url;
        protected string ProjectName;
        public abstract bool Connect();
        protected BaseClient(string serverUrl, string projectName)
        {
            Url = new Uri(serverUrl);
            ProjectName = projectName;
        }
    }
}
