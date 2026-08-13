using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Android_ADB_Tool.Utils
{
    /// <summary>
    /// 简易 HTTP 文件服务，使用 TcpListener 绑定 0.0.0.0，避免 HttpListener 的 URLACL 问题。
    /// </summary>
    public class LocalApkHttpServer : IDisposable
    {
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private string _apkPath;
        private readonly object _pathLock = new object();

        public int Port { get; private set; }
        public bool IsRunning { get { return _running; } }

        public void SetApkPath(string path)
        {
            lock (_pathLock)
            {
                _apkPath = path;
            }
        }

        /// <summary>
        /// 启动服务。端口占用时抛出 InvalidOperationException。
        /// </summary>
        public void Start(int port)
        {
            if (_running)
            {
                return;
            }
            Port = port;
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
            }
            catch (SocketException ex)
            {
                _listener = null;
                throw new InvalidOperationException(
                    "HTTP服务启动失败，端口 " + port + " 冲突或无法绑定。", ex);
            }

            _running = true;
            _thread = new Thread(AcceptLoop);
            _thread.IsBackground = true;
            _thread.Name = "ota-http";
            _thread.Start();
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
                }
                catch (SocketException)
                {
                    if (!_running)
                    {
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch
                {
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    client.ReceiveTimeout = 15000;
                    client.SendTimeout = 60000;

                    string requestLine = ReadRequestLine(stream);
                    // 读完剩余请求头，避免客户端阻塞
                    DrainHeaders(stream);

                    if (string.IsNullOrEmpty(requestLine) || !requestLine.StartsWith("GET ", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteSimpleResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Bad Request"));
                        return;
                    }

                    string pathPart = requestLine.Substring(4);
                    int sp = pathPart.IndexOf(' ');
                    if (sp > 0)
                    {
                        pathPart = pathPart.Substring(0, sp);
                    }
                    // 去掉 query
                    int q = pathPart.IndexOf('?');
                    if (q >= 0)
                    {
                        pathPart = pathPart.Substring(0, q);
                    }

                    if (!pathPart.Equals("/upgrade.apk", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteSimpleResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("Not Found"));
                        return;
                    }

                    string apkPath;
                    lock (_pathLock)
                    {
                        apkPath = _apkPath;
                    }
                    if (string.IsNullOrEmpty(apkPath) || !File.Exists(apkPath))
                    {
                        WriteSimpleResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("APK not set"));
                        return;
                    }

                    byte[] data = File.ReadAllBytes(apkPath);
                    WriteSimpleResponse(stream, 200, "OK",
                        "application/vnd.android.package-archive", data);
                }
            }
            catch
            {
            }
        }

        private static string ReadRequestLine(NetworkStream stream)
        {
            StringBuilder sb = new StringBuilder(256);
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0)
                {
                    break;
                }
                if (b == '\n')
                {
                    break;
                }
                if (b != '\r')
                {
                    sb.Append((char)b);
                }
                if (sb.Length > 2048)
                {
                    break;
                }
            }
            return sb.ToString();
        }

        private static void DrainHeaders(NetworkStream stream)
        {
            int empty = 0;
            while (empty < 2)
            {
                int b = stream.ReadByte();
                if (b < 0)
                {
                    break;
                }
                if (b == '\n')
                {
                    empty++;
                }
                else if (b != '\r')
                {
                    empty = 0;
                }
            }
        }

        private static void WriteSimpleResponse(NetworkStream stream, int code, string reason,
            string contentType, byte[] body)
        {
            string header =
                "HTTP/1.1 " + code + " " + reason + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Connection: close\r\n" +
                "\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        public void Stop()
        {
            _running = false;
            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                }
            }
            catch { }
            _listener = null;
            try
            {
                if (_thread != null && _thread.IsAlive)
                {
                    _thread.Join(500);
                }
            }
            catch { }
            _thread = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
