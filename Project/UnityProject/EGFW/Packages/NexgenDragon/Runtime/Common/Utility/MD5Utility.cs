// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable IdentifierTypo

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Debug = UnityEngine.Debug;

namespace NexgenDragon
{
    public class MD5Utility
    {
        public static bool CheckFileMd5(string filePath, string md5)
        {
            return ComputeFileMd5(filePath) == md5;
        }

        public static string ComputeFileMd5(string filePath)
        {
            var stopwatch = new Stopwatch();
            using (var md5Hash = MD5.Create())
            {
                stopwatch.Start();

                var fileStream = new FileStream(filePath, FileMode.Open);
                byte[] data = md5Hash.ComputeHash(fileStream);
                fileStream.Close();

                var sBuilder = new StringBuilder();
                foreach (var b in data)
                {
                    sBuilder.Append(b.ToString("x2"));
                }

                stopwatch.Stop();

                Debug.Log("Time cost: " + stopwatch.ElapsedMilliseconds);

                return sBuilder.ToString();
            }
        }

        /// <summary>
        /// 计算str的md5 返回16进制字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ComputeMd5(string str)
        {
            var ue = new UTF8Encoding();
            byte[] bytes = ue.GetBytes(str);
            return ComputeMd5(bytes);
        }
        
        /// <summary>
        /// 计算bytes的md5 返回16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ComputeMd5(byte[] bytes)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);
            var builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                var b = hashBytes[i];
                if (s_byte2HexStr.TryGetValue(b, out var s))
                {
                    builder.Append(s);
                }
                else
                {
                     s = b.ToString("x2");
                     s_byte2HexStr[b] = s;
                     builder.Append(s);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// byte转为16进制string的缓存map
        /// </summary>
        private static readonly Dictionary<byte, string> s_byte2HexStr = new Dictionary<byte, string>(256); //byte取值范围
    }
}