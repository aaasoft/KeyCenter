using System.Net.NetworkInformation;
using System.Text;

namespace KeyCenter.Core.Utils
{
    internal class LicenseUtils
    {
        private const String INVALID_LICENSE_KEY =
            @"00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000
00000000000000000000000000000000";

        private static string[] ignoreNetworkInterfaceDescriptionPrefixs = new[]
                {
            "docker",
            "veth"
        };

        private static string[] ignoreNetworkInterfaceDescriptions = new[]
        {
            "VirtualBox","VMware","Virtual","Bluetooth","Mobile","Xiaomi"
        };

        internal static string GetSysInfo()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var cgroupFile = "/proc/self/cgroup";
                if (File.Exists(cgroupFile))
                {
                    var cgroupFileContent = File.ReadAllText(cgroupFile);
                    //是否在docker容器中运行
                    if (cgroupFileContent.Contains("/docker/"))
                        return cgroupFileContent;
                }
            }

            var nis = NetworkInterface.GetAllNetworkInterfaces()
                .OrderBy(t => t.Description)
                .ToArray();
            var sb = new StringBuilder();
            foreach (NetworkInterface ni in nis)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Ppp
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel
                    || ignoreNetworkInterfaceDescriptions.Any(t => ni.Description.Contains(t))
                    || ignoreNetworkInterfaceDescriptionPrefixs.Any(t => ni.Description.StartsWith(t)))
                    continue;
                if (ni.GetPhysicalAddress() == PhysicalAddress.None)
                    continue;
                String macAddress = ni.GetPhysicalAddress().ToString();
                sb.Append(macAddress + "->" + ni.Description + ";");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取机器码
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static string GetMachineId()
        {
            var text = GetSysInfo();
            if (string.IsNullOrEmpty(text))
                throw new ApplicationException("Cann't found any enabled network adapters.");
            return Md5Utils.ComputeMD5Hash(text);
        }

        /// <summary>
        /// 验证Key的有效性
        /// </summary>
        public static bool VerifyKey(string productName, string key, string publicKeyString, ref DateTime expireTime)
        {
            try
            {
                var license = new License(productName, key, publicKeyString);
                expireTime = license.ExpirationTime;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
