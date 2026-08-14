using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android_ADB_Tool.Entity;

namespace Android_ADB_Tool.Utils
{
    public static class OtaProtocol
    {
        public const string Magic = "AJB_GUIDE_OTA";
        public const int UdpPort = 19001;
        public const int HttpPort = 18080;
        /// <summary>与 A 端 DISCOVER 广播间隔一致（毫秒）</summary>
        public const int BeaconIntervalMs = 5000;
        /// <summary>连续未收到心跳次数，达到后从列表移除</summary>
        public const int OfflineMissedBeacons = 2;
        public const int DeviceOfflineTimeoutMs = BeaconIntervalMs * OfflineMissedBeacons;
        public const string DeviceTypeInfoScreen = "InfoScreen";
        public const string DeviceTypeReverseForCar = "ReverseForCar";
        public const string CmdDiscover = "DISCOVER";
        public const string CmdUpgrade = "UPGRADE";
        public const string CmdStatus = "STATUS";

        public const string CodeAlreadyLatest = "ALREADY_LATEST";
        public const string CodeBusy = "BUSY";
        public const string CodeReady = "READY";
        public const string CodeFail = "FAIL";
        public const string CodeSuccess = "SUCCESS";
        public const int UpgradeResultTimeoutMs = 180000;

        private static readonly Regex VersionXyRegex = new Regex(@"^\d+\.\d+$", RegexOptions.Compiled);
        private static readonly Regex VersionXyzRegex = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

        public static bool IsKnownDeviceType(string type)
        {
            return string.Equals(type, DeviceTypeInfoScreen, StringComparison.Ordinal)
                || string.Equals(type, DeviceTypeReverseForCar, StringComparison.Ordinal);
        }

        public static bool IsValidVersionForType(string type, string ver)
        {
            if (string.IsNullOrWhiteSpace(ver))
            {
                return false;
            }
            string v = ver.Trim();
            if (string.Equals(type, DeviceTypeInfoScreen, StringComparison.Ordinal))
            {
                return VersionXyRegex.IsMatch(v);
            }
            if (string.Equals(type, DeviceTypeReverseForCar, StringComparison.Ordinal))
            {
                return VersionXyzRegex.IsMatch(v);
            }
            return false;
        }

        public static string ToDisplayType(string type)
        {
            if (string.Equals(type, DeviceTypeInfoScreen, StringComparison.Ordinal))
            {
                return "引导屏";
            }
            if (string.Equals(type, DeviceTypeReverseForCar, StringComparison.Ordinal))
            {
                return "寻车机";
            }
            return "--";
        }

        public static Dictionary<string, string> ParseFields(string message, out string cmd)
        {
            cmd = null;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            string[] parts = message.Trim().Split('|');
            if (parts.Length < 2)
            {
                return null;
            }
            if (!string.Equals(parts[0], Magic, StringComparison.Ordinal))
            {
                return null;
            }

            cmd = parts[1];
            if (!string.Equals(cmd, CmdDiscover, StringComparison.Ordinal)
                && !string.Equals(cmd, CmdUpgrade, StringComparison.Ordinal)
                && !string.Equals(cmd, CmdStatus, StringComparison.Ordinal))
            {
                return null;
            }

            for (int i = 2; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                string key = parts[i].Substring(0, eq).Trim();
                string value = parts[i].Substring(eq + 1).Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    map[key] = value;
                }
            }
            return map;
        }

        public static SearchDeviceInfo ParseDiscover(string message)
        {
            Dictionary<string, string> map = ParseFields(message, out string cmd);
            if (map == null || !string.Equals(cmd, CmdDiscover, StringComparison.Ordinal))
            {
                return null;
            }
            if (!map.ContainsKey("ip") || !map.ContainsKey("mac") || !map.ContainsKey("type") || !map.ContainsKey("ver"))
            {
                return null;
            }
            string type = map["type"];
            if (!IsKnownDeviceType(type) || !IsValidVersionForType(type, map["ver"]))
            {
                return null;
            }
            return new SearchDeviceInfo
            {
                Ip = map["ip"],
                Mac = map["mac"],
                Type = type,
                Ver = map["ver"]
            };
        }

        public static bool ParseStatus(string message, out string ip, out string ver, out string code, out string msg)
        {
            ip = ver = code = msg = null;
            Dictionary<string, string> map = ParseFields(message, out string cmd);
            if (map == null || !string.Equals(cmd, CmdStatus, StringComparison.Ordinal))
            {
                return false;
            }
            if (!map.ContainsKey("ip") || !map.ContainsKey("ver") || !map.ContainsKey("code") || !map.ContainsKey("msg"))
            {
                return false;
            }
            ip = map["ip"];
            ver = map["ver"];
            code = map["code"];
            msg = map["msg"];
            return true;
        }

        public static string BuildUpgrade(string ver, string url)
        {
            return OtaAuth.BuildSignedUpgrade(ver, url);
        }
    }
}
