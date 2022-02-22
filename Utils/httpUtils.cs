using Android_ADB_Tool.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Android_ADB_Tool.Utils
{
    class HttpUtils
    {
        private const string SERVER_BASE_URL = "device-cloud.dyajb.com";  //api服务测试地址
        private const string APP_ID = "appid";  //分配ID
        private const string TIMESTAMP = "timestamp";  //时间戳
        private const string NONCE = "nonce";  //流水号
        private const string BODY = "body";  //请求体
        private const string SIGNATURE = "signature";  //签名

        private const string APP_ID_VALUE = "device_config_tool";  //分配ID值  divice_cloud_android_app
        private const string APP_SECRET = "7201b1dc-afd9-3b00-9dc2-a1dda52a3a3a";  //key  2ff9d4a4-1b10-44e3-aa8a-2bf383c84585
        
        /**
         * 测试接口
         */
        public static string test(string data)
        {
            string pathUrl = "https://" + SERVER_BASE_URL + "/deviceCloud/device/init";
            return HttpPost(pathUrl, data).ToString();

        }

        /**
         * 通过车场ID查询车场信息
         */
        public static ResultInfo<ParkingInfo> QueryParking(Hashtable ht)
        {
            string pathUrl = "https://" + SERVER_BASE_URL + "/deviceCloud/tool/ports";
            return HttpGet(pathUrl, ht);

        }

        /**
         * 通过车场ID查询车场信息
         */
        public static ResultInfo<string> BindPort(Hashtable ht)
        {
            string pathUrl = "https://" + SERVER_BASE_URL + "/deviceCloud/tool/bindport";
            JavaScriptSerializer jss = new JavaScriptSerializer();
            return HttpPost(pathUrl, jss.Serialize(ht));

        }

        private static ResultInfo<ParkingInfo> HttpGet(string url, Hashtable ht)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string currentTimeMillis = Convert.ToInt64(ts.TotalMilliseconds).ToString();
            Hashtable hs = new Hashtable();
            hs.Add(APP_ID, APP_ID_VALUE);
            hs.Add(TIMESTAMP, currentTimeMillis);
            hs.Add(NONCE, APP_ID_VALUE + currentTimeMillis);
            hs.Add(BODY, "");
            ICollection key = ht.Keys;
            foreach (string k in key)
            {
                hs.Add(k, ht[k]);
            }
            Console.WriteLine("hs：" + jss.Serialize(hs));

            ResultInfo<ParkingInfo> resultInfo = new ResultInfo<ParkingInfo>();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json; charset=utf-8";  //application/x-www-form-urlencoded
                request.Headers.Add(APP_ID, (string)hs[APP_ID]);
                request.Headers.Add(TIMESTAMP, (string)hs[TIMESTAMP]);
                request.Headers.Add(NONCE, (string)hs[NONCE]);
                request.Headers.Add(SIGNATURE, getSignature(hs));
                
                Stream myRequestStream = request.GetRequestStream();
                StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
                myStreamWriter.Write(jss.Serialize(ht));
                myStreamWriter.Close();

                Console.WriteLine("GET请求：" + jss.Serialize(request));
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream myResponseStream = response.GetResponseStream();
                string retString = "";
                using (StreamReader reader = new StreamReader(myResponseStream, Encoding.UTF8))
                {
                    retString = reader.ReadToEnd();
                }

                resultInfo = jss.Deserialize<ResultInfo<ParkingInfo>>(retString);

            }
            catch (WebException webException)
            {
                Console.WriteLine("请求错误WebException：" + webException.Message);
                resultInfo.message = webException.Message;

            }
            catch (Exception e)
            {
                Console.WriteLine("请求错误Exception：" + e.Message);
                resultInfo.message = e.Message;
            }

            return resultInfo;
        }

        private static ResultInfo<string> HttpPost(string url, string postDataStr)
        {
            Hashtable hs = new Hashtable();
            hs.Add(APP_ID, APP_ID_VALUE);
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string currentTimeMillis = Convert.ToInt64(ts.TotalMilliseconds).ToString();
            hs.Add(TIMESTAMP, currentTimeMillis);
            hs.Add(NONCE, APP_ID_VALUE + currentTimeMillis);
            hs.Add(BODY, postDataStr);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Console.WriteLine("hs：" + jss.Serialize(hs));

            ResultInfo<string> resultInfo = new ResultInfo<string>();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";  //application/x-www-form-urlencoded
                request.Headers.Add(APP_ID, (string)hs[APP_ID]);
                request.Headers.Add(TIMESTAMP, (string)hs[TIMESTAMP]);
                request.Headers.Add(NONCE, (string)hs[NONCE]);
                request.Headers.Add(SIGNATURE, getSignature(hs));

                request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
                Stream myRequestStream = request.GetRequestStream();
                StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
                myStreamWriter.Write(postDataStr);
                myStreamWriter.Close();
                
                Console.WriteLine("POST请求：" + jss.Serialize(request));
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();

                resultInfo = jss.Deserialize<ResultInfo<string>>(retString);

            }
            catch (WebException webException)
            {
                Console.WriteLine("请求错误WebException：" + webException.Message);
                resultInfo.message = webException.Message;

            }
            catch (Exception e)
            {
                Console.WriteLine("请求错误Exception：" + e.Message);
                resultInfo.message = e.Message;
            }
            
            return resultInfo;
        }

        private static string getSignature(Hashtable ht)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            ICollection keys = ht.Keys;
            ArrayList arrayList = new ArrayList();
            foreach (string key in keys)
            {
                arrayList.Add(key);
            }
            arrayList.Sort();
            StringBuilder sb = new StringBuilder();
            foreach (string key in arrayList)
            {
                sb.Append((string)ht[key]).Append(",");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(APP_SECRET);
            Console.WriteLine("签名前2：" + sb.ToString());
            return md5(sb.ToString());
        }

        private static string md5(string str)
        {
            try
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] bytValue, bytHash;
                bytValue = System.Text.Encoding.UTF8.GetBytes(str);
                bytHash = md5.ComputeHash(bytValue);
                md5.Clear();
                string sTemp = "";
                for (int i = 0; i < bytHash.Length; i++)
                {
                    sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
                }
                str = sTemp.ToLower();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return str;
        }
    }
}
