using System;

namespace Android_ADB_Tool.Entity
{
    public class SearchDeviceInfo
    {
        public string Ip { get; set; }
        public string Mac { get; set; }
        public string Type { get; set; }
        public string Ver { get; set; }
        /// <summary>最近一次收到 DISCOVER 的本地时间</summary>
        public DateTime LastSeenUtc { get; set; }
    }
}
