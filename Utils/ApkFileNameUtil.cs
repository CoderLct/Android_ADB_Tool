using System.IO;
using System.Text.RegularExpressions;

namespace Android_ADB_Tool.Utils
{
    public static class ApkFileNameUtil
    {
        private static readonly Regex InfoScreenRegex = new Regex(
            @"^InfoScreen_V(\d+\.\d+)_\d{8}\.apk$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ReverseForCarRegex = new Regex(
            @"^.+_ReverseForCar_V(\d+\.\d+\.\d+)_\d{8}\.apk$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryParse(string filePath, out string version, out string type)
        {
            version = null;
            type = null;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }
            string name = Path.GetFileName(filePath);
            Match info = InfoScreenRegex.Match(name);
            if (info.Success)
            {
                type = OtaProtocol.DeviceTypeInfoScreen;
                version = info.Groups[1].Value;
                return OtaProtocol.IsValidVersionForType(type, version);
            }
            Match reverse = ReverseForCarRegex.Match(name);
            if (reverse.Success)
            {
                type = OtaProtocol.DeviceTypeReverseForCar;
                version = reverse.Groups[1].Value;
                return OtaProtocol.IsValidVersionForType(type, version);
            }
            return false;
        }
    }
}
