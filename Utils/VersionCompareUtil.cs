using System;

namespace Android_ADB_Tool.Utils
{
    public static class VersionCompareUtil
    {
        /// <summary>
        /// 仅在同一设备类型、同一版本格式内比较：apkVer 高于 deviceVer 时可升级。
        /// InfoScreen 只比 x.y；ReverseForCar 只比 x.y.z。
        /// </summary>
        public static bool CanUpgrade(string deviceType, string deviceVer, string apkVer)
        {
            int[] deviceParts;
            int[] apkParts;
            if (!TryParseForType(deviceType, deviceVer, out deviceParts)
                || !TryParseForType(deviceType, apkVer, out apkParts))
            {
                return false;
            }
            for (int i = 0; i < deviceParts.Length; i++)
            {
                if (apkParts[i] != deviceParts[i])
                {
                    return apkParts[i] > deviceParts[i];
                }
            }
            return false;
        }

        private static bool TryParseForType(string type, string ver, out int[] parts)
        {
            parts = null;
            if (!OtaProtocol.IsValidVersionForType(type, ver))
            {
                return false;
            }
            string[] segs = ver.Trim().Split('.');
            parts = new int[segs.Length];
            for (int i = 0; i < segs.Length; i++)
            {
                if (!int.TryParse(segs[i], out parts[i]))
                {
                    parts = null;
                    return false;
                }
            }
            return true;
        }
    }
}
