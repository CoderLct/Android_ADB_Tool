using System.IO;
using System.Text.RegularExpressions;

namespace Android_ADB_Tool.Utils
{
    public static class ApkFileNameUtil
    {
        private static readonly Regex FileNameRegex = new Regex(
            @"^InfoScreen_V(\d+\.\d+)_\d{8}\.apk$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryGetVersion(string filePath, out string version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }
            string name = Path.GetFileName(filePath);
            Match m = FileNameRegex.Match(name);
            if (!m.Success)
            {
                return false;
            }
            version = m.Groups[1].Value;
            return OtaProtocol.IsValidVersion(version);
        }
    }
}
