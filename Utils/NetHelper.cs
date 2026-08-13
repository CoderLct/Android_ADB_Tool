using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Android_ADB_Tool.Utils
{
    public static class NetHelper
    {
        /// <summary>
        /// 获取本机局域网 IPv4。若提供 remoteIp，优先返回与其同网段的网卡地址（多网卡场景）。
        /// </summary>
        public static string GetLocalLanIp(string remoteIp = null)
        {
            IPAddress remoteAddr = null;
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                IPAddress.TryParse(remoteIp.Trim(), out remoteAddr);
            }

            string fallback = null;
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork
                            || IPAddress.IsLoopback(ip.Address))
                        {
                            continue;
                        }

                        string local = ip.Address.ToString();
                        if (fallback == null)
                        {
                            fallback = local;
                        }

                        if (remoteAddr != null
                            && ip.IPv4Mask != null
                            && IsSameSubnet(ip.Address, remoteAddr, ip.IPv4Mask))
                        {
                            return local;
                        }
                    }
                }
            }
            catch
            {
            }
            return fallback;
        }

        private static bool IsSameSubnet(IPAddress local, IPAddress remote, IPAddress mask)
        {
            byte[] a = local.GetAddressBytes();
            byte[] b = remote.GetAddressBytes();
            byte[] m = mask.GetAddressBytes();
            if (a.Length != 4 || b.Length != 4 || m.Length != 4)
            {
                return false;
            }
            for (int i = 0; i < 4; i++)
            {
                if ((a[i] & m[i]) != (b[i] & m[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 尝试放行入站 TCP 端口（需管理员权限；失败则忽略）。
        /// </summary>
        public static void TryAllowInboundTcp(int port, string ruleName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall add rule name=\"" + ruleName
                        + "\" dir=in action=allow protocol=TCP localport=" + port,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        p.WaitForExit(3000);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
