using System;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace HavegetDynamicIsland
{
    public sealed class SystemSnapshot
    {
        public bool HasCpuSample;
        public double CpuPercent;
        public double MemoryPercent;
        public ulong MemoryUsed;
        public ulong MemoryTotal;
        public double DiskPercent;
        public ulong DiskFree;
        public ulong DiskTotal;
        public bool Online;
        public string NetworkLabel = "离线";
        public string NetworkDetail = "未检测到可用网络";
        public bool BatteryPresent;
        public bool Charging;
        public int BatteryPercent;
        public ulong UptimeMilliseconds;
        public string WindowsName = "";
    }

    /// <summary>
    /// Reads CPU, memory, disk, network and battery data locally. No network
    /// requests and no third-party service are involved.
    /// </summary>
    public sealed class SystemMonitor
    {
        private ulong _previousIdle;
        private ulong _previousKernel;
        private ulong _previousUser;
        private bool _hasCpuBaseline;

        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime
        {
            public uint Low;
            public uint High;
            public ulong Value { get { return ((ulong)High << 32) | Low; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        public SystemSnapshot Sample()
        {
            SystemSnapshot s = new SystemSnapshot();

            // CPU ---------------------------------------------------------
            FileTime idle, kernel, user;
            if (GetSystemTimes(out idle, out kernel, out user))
            {
                if (_hasCpuBaseline)
                {
                    ulong idleDiff = idle.Value - _previousIdle;
                    ulong kernelDiff = kernel.Value - _previousKernel;
                    ulong userDiff = user.Value - _previousUser;
                    ulong total = kernelDiff + userDiff;
                    if (total > 0)
                    {
                        s.CpuPercent = Math.Min(100.0, Math.Max(0.0, (1.0 - ((double)idleDiff / total)) * 100.0));
                        s.HasCpuSample = true;
                    }
                }
                _previousIdle = idle.Value;
                _previousKernel = kernel.Value;
                _previousUser = user.Value;
                _hasCpuBaseline = true;
            }

            // Memory ------------------------------------------------------
            MemoryStatusEx mem = new MemoryStatusEx();
            mem.Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            if (GlobalMemoryStatusEx(ref mem))
            {
                s.MemoryTotal = mem.TotalPhys;
                s.MemoryUsed = mem.TotalPhys > mem.AvailPhys ? mem.TotalPhys - mem.AvailPhys : 0;
                s.MemoryPercent = mem.TotalPhys > 0 ? (s.MemoryUsed * 100.0 / mem.TotalPhys) : 0;
            }

            // Disk --------------------------------------------------------
            try
            {
                string root = Path.GetPathRoot(Environment.SystemDirectory);
                DriveInfo drive = new DriveInfo(root);
                s.DiskTotal = (ulong)Math.Max(0, drive.TotalSize);
                s.DiskFree = (ulong)Math.Max(0, drive.AvailableFreeSpace);
                s.DiskPercent = s.DiskTotal > 0 ? ((s.DiskTotal - s.DiskFree) * 100.0 / s.DiskTotal) : 0;
            }
            catch { }

            // Network -----------------------------------------------------
            try
            {
                s.Online = NetworkInterface.GetIsNetworkAvailable();
                bool foundNetwork = false;
                if (s.Online)
                {
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                            ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                        bool hasAddress = false;
                        try
                        {
                            foreach (UnicastIPAddressInformation addr in ni.GetIPProperties().UnicastAddresses)
                            {
                                if (addr.Address != null && !System.Net.IPAddress.IsLoopback(addr.Address))
                                {
                                    hasAddress = true;
                                    break;
                                }
                            }
                        }
                        catch { }
                        if (!hasAddress) continue;
                        string kind;
                        switch (ni.NetworkInterfaceType)
                        {
                            case NetworkInterfaceType.Wireless80211: kind = "Wi-Fi"; break;
                            case NetworkInterfaceType.Ethernet:
                            case NetworkInterfaceType.GigabitEthernet:
                            case NetworkInterfaceType.FastEthernetT:
                            case NetworkInterfaceType.FastEthernetFx:
                                kind = "以太网"; break;
                            case NetworkInterfaceType.Wwanpp:
                            case NetworkInterfaceType.Wwanpp2:
                                kind = "移动网络"; break;
                            default: kind = "网络"; break;
                        }
                        s.NetworkLabel = "在线";
                        string speed = "";
                        if (ni.Speed >= 1000000000L)
                            speed = (ni.Speed / 1000000000.0).ToString("0.#", CultureInfo.InvariantCulture) + " Gbps";
                        else if (ni.Speed > 0)
                            speed = (ni.Speed / 1000000.0).ToString("0", CultureInfo.InvariantCulture) + " Mbps";
                        s.NetworkDetail = kind + (speed.Length > 0 ? " · " + speed : "");
                        foundNetwork = true;
                        break;
                    }
                    s.Online = foundNetwork;
                    if (!foundNetwork)
                    {
                        s.NetworkLabel = "离线";
                        s.NetworkDetail = "未检测到可用网络";
                    }
                }
            }
            catch { }

            // Battery -----------------------------------------------------
            BatteryInfo battery = BatteryInfo.Read();
            s.BatteryPresent = battery.HasBattery;
            s.BatteryPercent = battery.Percent;
            s.Charging = battery.Charging;

            // Uptime ------------------------------------------------------
            try { s.UptimeMilliseconds = GetTickCount64(); } catch { }

            s.WindowsName = "Windows " + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor;
            return s;
        }
    }
}
