using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevopsClient
{
    public class AzureAreaWrapper
    {
        public string AreaPathFromTfs { get; set; }
        public IEnumerable<AzureAreaWrapper> ChildWrappers { get; set; }
    }
}
