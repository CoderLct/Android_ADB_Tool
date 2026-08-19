using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Android_ADB_Tool.Utils
{
    /// <summary>
    /// 从设备 HTTP 下载 Anjubao zip，解压到目标目录（同名文件覆盖）。
    /// </summary>
    public static class LogDownloadHelper
    {
        public static void DownloadAndExtract(string url, string destDir, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("下载地址为空");
            }
            if (string.IsNullOrWhiteSpace(destDir))
            {
                throw new InvalidOperationException("保存目录为空");
            }
            Directory.CreateDirectory(destDir);
            string tempZip = Path.Combine(Path.GetTempPath(),
                "anjubao_" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                DownloadFile(url, tempZip, timeoutMs);
                ExtractOverwrite(tempZip, destDir);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempZip))
                    {
                        File.Delete(tempZip);
                    }
                }
                catch
                {
                }
            }
        }

        private static void DownloadFile(string url, string destFile, int timeoutMs)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException("HTTP " + (int)resp.StatusCode);
                }
                using (Stream src = resp.GetResponseStream())
                using (FileStream dst = File.Create(destFile))
                {
                    if (src == null)
                    {
                        throw new InvalidOperationException("空响应");
                    }
                    src.CopyTo(dst);
                }
            }
        }

        private static void ExtractOverwrite(string zipPath, string destDir)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string name = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    if (string.IsNullOrEmpty(name) || name.IndexOf("..", StringComparison.Ordinal) >= 0)
                    {
                        continue;
                    }
                    string full = Path.Combine(destDir, name);
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(full);
                        continue;
                    }
                    string parent = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }
                    using (Stream src = entry.Open())
                    using (FileStream dst = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        src.CopyTo(dst);
                    }
                }
            }
        }
    }
}
