using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace Android_ADB_Tool.Utils
{
    /// <summary>
    /// 从设备 HTTP 下载 zip 并解压（同名覆盖）。
    /// 解压不用 System.IO.Compression.dll（4.2.0.0 在 2012 R2 / 4.5.1 上不存在），
    /// 只用 System.dll 里的 DeflateStream。
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
            using (FileStream fs = File.OpenRead(zipPath))
            {
                int eocd = FindEocd(fs);
                if (eocd < 0)
                {
                    throw new InvalidOperationException("无效的zip文件");
                }
                fs.Position = eocd + 10;
                int entryCount = ReadUInt16(fs);
                fs.Position = eocd + 16;
                int cdOffset = ReadInt32(fs);
                fs.Position = cdOffset;
                for (int i = 0; i < entryCount; i++)
                {
                    if (ReadInt32(fs) != 0x02014b50)
                    {
                        throw new InvalidOperationException("zip目录损坏");
                    }
                    fs.Position += 4;
                    int flags = ReadUInt16(fs);
                    int method = ReadUInt16(fs);
                    fs.Position += 8;
                    int compSize = ReadInt32(fs);
                    int uncompSize = ReadInt32(fs);
                    int nameLen = ReadUInt16(fs);
                    int extraLen = ReadUInt16(fs);
                    int commentLen = ReadUInt16(fs);
                    fs.Position += 8;
                    int localOff = ReadInt32(fs);
                    byte[] nameBytes = ReadBytes(fs, nameLen);
                    fs.Position += extraLen + commentLen;
                    Encoding enc = (flags & 0x800) != 0 ? Encoding.UTF8 : Encoding.Default;
                    string name = enc.GetString(nameBytes).Replace('/', Path.DirectorySeparatorChar);
                    if (string.IsNullOrEmpty(name) || name.IndexOf("..", StringComparison.Ordinal) >= 0)
                    {
                        continue;
                    }
                    string full = Path.Combine(destDir, name);
                    bool isDir = name.EndsWith(Path.DirectorySeparatorChar.ToString())
                        || (uncompSize == 0 && compSize == 0 && (name.EndsWith("/") || name.EndsWith("\\")));
                    if (isDir)
                    {
                        Directory.CreateDirectory(full);
                        continue;
                    }
                    string parent = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }
                    long nextCd = fs.Position;
                    ExtractOne(fs, localOff, method, compSize, full);
                    fs.Position = nextCd;
                }
            }
        }

        private static void ExtractOne(FileStream fs, int localOff, int method, int compSize, string destFile)
        {
            fs.Position = localOff;
            if (ReadInt32(fs) != 0x04034b50)
            {
                throw new InvalidOperationException("zip条目损坏");
            }
            fs.Position += 22;
            int nameLen = ReadUInt16(fs);
            int extraLen = ReadUInt16(fs);
            fs.Position += nameLen + extraLen;
            using (FileStream dst = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (compSize <= 0)
                {
                    return;
                }
                LimitedStream limited = new LimitedStream(fs, compSize);
                if (method == 0)
                {
                    limited.CopyTo(dst);
                }
                else if (method == 8)
                {
                    using (DeflateStream deflate = new DeflateStream(limited, CompressionMode.Decompress, true))
                    {
                        deflate.CopyTo(dst);
                    }
                }
                else
                {
                    throw new InvalidOperationException("不支持的zip压缩方式: " + method);
                }
            }
        }

        private static int FindEocd(FileStream fs)
        {
            long len = fs.Length;
            int maxBack = (int)Math.Min(len, 65557);
            byte[] tail = new byte[maxBack];
            fs.Position = len - maxBack;
            int n = fs.Read(tail, 0, maxBack);
            for (int i = n - 22; i >= 0; i--)
            {
                if (tail[i] == 0x50 && tail[i + 1] == 0x4B && tail[i + 2] == 0x05 && tail[i + 3] == 0x06)
                {
                    return (int)(len - maxBack + i);
                }
            }
            return -1;
        }

        private static int ReadUInt16(Stream s)
        {
            int a = s.ReadByte();
            int b = s.ReadByte();
            if (a < 0 || b < 0)
            {
                throw new EndOfStreamException();
            }
            return a | (b << 8);
        }

        private static int ReadInt32(Stream s)
        {
            int a = ReadUInt16(s);
            int b = ReadUInt16(s);
            return a | (b << 16);
        }

        private static byte[] ReadBytes(Stream s, int count)
        {
            byte[] buf = new byte[count];
            int off = 0;
            while (off < count)
            {
                int n = s.Read(buf, off, count - off);
                if (n <= 0)
                {
                    throw new EndOfStreamException();
                }
                off += n;
            }
            return buf;
        }

        private sealed class LimitedStream : Stream
        {
            private readonly Stream _inner;
            private int _left;

            public LimitedStream(Stream inner, int length)
            {
                _inner = inner;
                _left = length;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_left <= 0)
                {
                    return 0;
                }
                if (count > _left)
                {
                    count = _left;
                }
                int n = _inner.Read(buffer, offset, count);
                _left -= n;
                return n;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }
    }
}
