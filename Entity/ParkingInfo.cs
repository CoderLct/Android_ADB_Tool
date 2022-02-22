using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Android_ADB_Tool.Entity
{
    class ParkingInfo
    {
        public string ltdCode { get; set; }
        public string parkName { get; set; }
        public List<PortInfo> ports { get; set; }
    }
}
