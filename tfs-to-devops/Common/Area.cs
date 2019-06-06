using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class Area
    {
        public Area(int areaId, string areaPath)
        {
            AreaId = areaId;
            AreaPath = areaPath;
        }

        public int AreaId { get; }
        public string AreaPath { get; }
    }
}
