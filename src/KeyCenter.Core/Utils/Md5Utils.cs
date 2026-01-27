using System.Text;
using System.Security.Cryptography;

namespace KeyCenter.Core.Utils
{
    internal class Md5Utils
    {
        public static string ComputeMD5Hash(string data)
        {
            var buffer = ComputeMD5Hash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(buffer).Replace("-", "");
        }

        public static byte[] ComputeMD5Hash(byte[] data)
        {
            var md5 = MD5.Create();
            return md5.ComputeHash(data);
        }
    }
}
