using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Android_ADB_Tool.Entity;

namespace Android_ADB_Tool.Utils
{
    public class UdpSearchService : IDisposable
    {
        public delegate void DiscoverHandler(SearchDeviceInfo device);
        public delegate void StatusHandler(string ip, string ver, string code, string msg);
        public delegate void LogHandler(string ip, string code, string msg, string url);

        public event DiscoverHandler OnDiscover;
        public event StatusHandler OnStatus;
        public event LogHandler OnLog;

        private UdpClient _client;
        private Thread _thread;
        private volatile bool _running;

        public bool IsRunning { get { return _running; } }

        public void Start(int port)
        {
            if (_running)
            {
                return;
            }
            _client = new UdpClient(port);
            _client.EnableBroadcast = true;
            _running = true;
            _thread = new Thread(ReceiveLoop);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void ReceiveLoop()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    byte[] data = _client.Receive(ref remote);
                    string text = Encoding.UTF8.GetString(data);
                    SearchDeviceInfo device = OtaProtocol.ParseDiscover(text);
                    if (device != null)
                    {
                        DiscoverHandler h = OnDiscover;
                        if (h != null)
                        {
                            h(device);
                        }
                        continue;
                    }

                    string ip, ver, code, msg;
                    if (OtaProtocol.ParseStatus(text, out ip, out ver, out code, out msg))
                    {
                        StatusHandler sh = OnStatus;
                        if (sh != null)
                        {
                            sh(ip, ver, code, msg);
                        }
                        continue;
                    }

                    string logIp, logCode, logMsg, logUrl;
                    if (OtaProtocol.ParseLog(text, out logIp, out logCode, out logMsg, out logUrl))
                    {
                        LogHandler lh = OnLog;
                        if (lh != null)
                        {
                            lh(logIp, logCode, logMsg, logUrl);
                        }
                    }
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
                    // ignore malformed
                }
            }
        }

        public void SendUpgrade(string deviceIp, string ver, string url)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("UDP未启动");
            }
            string payload = OtaProtocol.BuildUpgrade(ver, url);
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            _client.Send(bytes, bytes.Length, deviceIp, OtaProtocol.UdpPort);
        }

        public void SendPullLog(string deviceIp)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("UDP未启动");
            }
            string payload = OtaProtocol.BuildPullLog();
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            _client.Send(bytes, bytes.Length, deviceIp, OtaProtocol.UdpPort);
        }

        public void Stop()
        {
            _running = false;
            try
            {
                if (_client != null)
                {
                    _client.Close();
                }
            }
            catch { }
            _client = null;
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
