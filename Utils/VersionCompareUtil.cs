using System;

namespace Android_ADB_Tool.Utils
{
    public static class VersionCompareUtil
    {
        /// <summary>
        /// 返回 true 表示 apkVer 高于 deviceVer，可升级。
        /// </summary>
        public static bool CanUpgrade(string deviceVer, string apkVer)
        {
            if (!TryParse(deviceVer, out int x1, out int y1) || !TryParse(apkVer, out int x2, out int y2))
            {
                return false;
            }
            return x2 > x1 || (x2 == x1 && y2 > y1);
        }

        /// <summary>
        /// 返回 true 表示 left >= right（设备已是最新或更高）。
        /// </summary>
        public static bool IsGreaterOrEqual(string left, string right)
        {
            if (!TryParse(left, out int x1, out int y1) || !TryParse(right, out int x2, out int y2))
            {
                return false;
            }
            return x1 > x2 || (x1 == x2 && y1 >= y2);
        }

        public static bool TryParse(string ver, out int x, out int y)
        {
            x = y = 0;
            if (!OtaProtocol.IsValidVersion(ver))
            {
                return false;
            }
            string[] parts = ver.Trim().Split('.');
            return int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
        }
    }
}
