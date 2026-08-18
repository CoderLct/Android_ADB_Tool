using System;
using System.Security.Cryptography;
using System.Text;

namespace Android_ADB_Tool.Utils
{
    /// <summary>
    /// OTA UPGRADE 指令 HMAC 鉴权（与 A 端 UpgradeAuth 算法一致）。
    /// 签名原文：ver|url|ts|nonce
    /// </summary>
    public static class OtaAuth
    {
        /// <summary>与 A 端保持一致；泄露后需双侧同时更换</summary>
        public const string AuthSecret = "AJB_GuideScreen_OTA_HMAC_v1";

        public static string BuildSignPayload(string ver, string url, string ts, string nonce)
        {
            return Safe(ver) + "|" + Safe(url) + "|" + Safe(ts) + "|" + Safe(nonce);
        }

        public static string HmacSha256Hex(string secret, string payload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? "")))
            {
                byte[] raw = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload ?? ""));
                var sb = new StringBuilder(raw.Length * 2);
                for (int i = 0; i < raw.Length; i++)
                {
                    sb.Append(raw[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string NewNonce()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string NowTsMs()
        {
            // 不用 DateTimeOffset.ToUnixTimeMilliseconds（.NET 4.6+），以兼容 Server 2012 R2 自带的 4.5.1
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return ((long)(DateTime.UtcNow - epoch).TotalMilliseconds).ToString();
        }

        /// <summary>
        /// 生成带鉴权字段的 UPGRADE 报文。
        /// </summary>
        public static string BuildSignedUpgrade(string ver, string url)
        {
            string ts = NowTsMs();
            string nonce = NewNonce();
            string sign = HmacSha256Hex(AuthSecret, BuildSignPayload(ver, url, ts, nonce));
            return OtaProtocol.Magic + "|" + OtaProtocol.CmdUpgrade
                   + "|ver=" + ver
                   + "|url=" + url
                   + "|ts=" + ts
                   + "|nonce=" + nonce
                   + "|sign=" + sign;
        }

        private static string Safe(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
        }
    }
}
