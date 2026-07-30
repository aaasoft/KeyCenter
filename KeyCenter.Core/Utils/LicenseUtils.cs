using System.Net.NetworkInformation;
using System.Text;
using Quick.Shell.PowerShell;
using Quick.Shell.Utils;

namespace KeyCenter.Core.Utils
{
    public class LicenseUtils
    {
        private static string[] ignoreNetworkInterfaceDescriptionPrefixs = new[]
                {
            "docker",
            "veth"
        };

        private static string[] ignoreNetworkInterfaceDescriptions = new[]
        {
            "VirtualBox","VMware","Virtual","Bluetooth","Mobile","Xiaomi"
        };

        private static string GetWmicProgramPath()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wbem", "WMIC.exe");
            if (File.Exists(path))
                return path;
            return null;
        }

        private static string ExecuteWmicCommand(string cmd)
        {
            var wmicProgramPath = GetWmicProgramPath();
            if(string.IsNullOrEmpty(wmicProgramPath))
                return null;
            var ret = ProcessUtils.ExecuteShell($"{wmicProgramPath} {cmd}");
            if(ret.ExitCode==0)
            {
                var lines = ret.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2)
                    return string.Join(',', lines.Skip(1));
            }
            return null;
        }

        private static string ExecutePowerShellCommand(string cmd)
        {
            using (var powerShellCommandContext = new PowerShellCommandContext())
            {
                powerShellCommandContext.Open();
                var ret = powerShellCommandContext.ExecuteCommand(cmd, true);
                powerShellCommandContext.Close();
                if (ret.ExitCode == 0)
                {
                    var lines = ret.Output;
                    if (lines.Length >= 3)
                        return string.Join(',', lines.Skip(2));
                }
            }
            return null;
        }

        private static string ExecuteFunctions(params Func<string>[] funcs)
        {
            if (funcs == null)
                return null;
            foreach (var func in funcs)
            {
                var ret = func();
                if (string.IsNullOrEmpty(ret))
                    continue;
                return ret;
            }
            return null;
        }

        /// <summary>
        /// 获取CPU序列号
        /// </summary>
        /// <returns></returns>
        public static string GetCpuSerial()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return ExecuteFunctions(
                        ()=>ExecuteWmicCommand("cpu get ProcessorId"),
                        ()=>ExecutePowerShellCommand("Get-CimInstance -ClassName Win32_Processor | Select-Object ProcessorId")                        
                    );
                }
                else
                {
                    var file = "/proc/cpuinfo";
                    if (File.Exists(file))
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line))
                                continue;
                            var strs = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                            if (strs.Length < 2)
                                continue;
                            var key = strs[0].Trim();
                            var value = strs[1].Trim();
                            if (key == "Serial")
                                return value;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public static string GetBoardSerial()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return ExecuteFunctions(
                        ()=>ExecuteWmicCommand("baseboard get SerialNumber"),
                        ()=>ExecutePowerShellCommand("Get-CimInstance -ClassName Win32_BaseBoard | Select-Object SerialNumber")
                    );
                }
                else
                {
                    var file = "/sys/class/dmi/id/board_serial";
                    if (File.Exists(file))
                        return File.ReadAllText(file).Trim();
                }
            }
            catch { }
            return null;
        }

        public static string GetDiskSerial()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return ExecuteFunctions(
                        ()=>ExecuteWmicCommand("diskdrive get SerialNumber"),
                        ()=>ExecutePowerShellCommand("Get-CimInstance -ClassName Win32_DiskDrive | Select-Object SerialNumber")
                    );
                }
                else
                {
                    var ret = ProcessUtils.ExecuteShell("lsblk -d -o SERIAL");
                    if (ret.ExitCode == 0)
                    {
                        var lines = ret.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length >= 2)
                            return string.Join(',', lines.Skip(1));
                    }
                }
            }
            catch { }
            return null;
        }
        
        public static string GetSysInfo()
        {
            var list = new List<string>();
            //CPU序列号
            var cpuSerial = GetCpuSerial();
            if (!string.IsNullOrEmpty(cpuSerial))
                list.Add(cpuSerial);
            //主板序列号
            var boardSerial = GetBoardSerial();
            if (!string.IsNullOrEmpty(boardSerial))
                list.Add(boardSerial);
            //磁盘序列号
            var diskSerial = GetDiskSerial();
            if (!string.IsNullOrEmpty(diskSerial))
                list.Add(diskSerial);
            //如果硬件信息少于2，则添加网卡信息
            if (list.Count < 2)
            {
                var sb = new StringBuilder();
                NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces()
                .OrderBy(t => t.Description)
                .ToArray();
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
                list.Add(sb.ToString());
            }
            return string.Join(Environment.NewLine, list);
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
                throw new ApplicationException("Cann't found any hardware info.");
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
