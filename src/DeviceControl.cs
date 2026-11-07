using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Management;

namespace HavegetDynamicIsland
{
    public static class AudioControl
    {
        [DllImport("winmm.dll")]
        private static extern int waveOutGetVolume(IntPtr device, out uint volume);

        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr device, uint volume);

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
            int GetDevice(string id, out IMMDevice device);
            int RegisterEndpointNotificationCallback(IntPtr client);
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid iid, uint clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
            int OpenPropertyStore(uint access, out IntPtr store);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            int GetState(out uint state);
        }

        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr pNotify);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr pNotify);
            [PreserveSig] int GetChannelCount(out uint pnChannelCount);
            [PreserveSig] int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
            [PreserveSig] int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
            [PreserveSig] int GetMasterVolumeLevel(out float pfLevelDB);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float pfLevel);
            [PreserveSig] int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
            [PreserveSig] int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
            [PreserveSig] int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            [PreserveSig] int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
            [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
            [PreserveSig] int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
            [PreserveSig] int VolumeStepUp(ref Guid pguidEventContext);
            [PreserveSig] int VolumeStepDown(ref Guid pguidEventContext);
            [PreserveSig] int QueryHardwareSupport(out uint pdwHardwareSupportMask);
            [PreserveSig] int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
        }

        private static IAudioEndpointVolume GetEndpointVolume()
        {
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
            IMMDevice device;
            int hr = enumerator.GetDefaultAudioEndpoint(0, 1, out device);
            Marshal.ThrowExceptionForHR(hr);
            Guid iid = typeof(IAudioEndpointVolume).GUID;
            object obj;
            hr = device.Activate(ref iid, 23, IntPtr.Zero, out obj);
            Marshal.ThrowExceptionForHR(hr);
            return (IAudioEndpointVolume)obj;
        }

        public static int GetVolume()
        {
            try
            {
                IAudioEndpointVolume endpoint = GetEndpointVolume();
                float value;
                int hr = endpoint.GetMasterVolumeLevelScalar(out value);
                Marshal.ReleaseComObject(endpoint);
                if (hr == 0) return (int)Math.Round(value * 100.0);
            }
            catch { }
            try
            {
                uint raw;
                if (waveOutGetVolume(IntPtr.Zero, out raw) == 0)
                {
                    uint left = raw & 0xFFFF;
                    uint right = (raw >> 16) & 0xFFFF;
                    return (int)Math.Round(((left + right) / 2) * 100.0 / 65535.0);
                }
            }
            catch { }
            return 50;
        }

        public static void SetVolume(int percent)
        {
            percent = Math.Max(0, Math.Min(100, percent));
            try
            {
                IAudioEndpointVolume endpoint = GetEndpointVolume();
                Guid context = Guid.Empty;
                int hr = endpoint.SetMasterVolumeLevelScalar(percent / 100.0f, ref context);
                Marshal.ReleaseComObject(endpoint);
                if (hr == 0) return;
            }
            catch { }
            try
            {
                uint value = (uint)Math.Round(percent * 65535.0 / 100.0);
                waveOutSetVolume(IntPtr.Zero, value | (value << 16));
            }
            catch { }
        }
    }
    public static class BrightnessControl
    {
        private static int _lastKnown = -1;

        public static int GetBrightness()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        object value = item["CurrentBrightness"];
                        if (value != null)
                        {
                            _lastKnown = Math.Max(0, Math.Min(100, Convert.ToInt32(value)));
                            return _lastKnown;
                        }
                    }
                }
            }
            catch { }
            return _lastKnown;
        }

        public static void SetBrightness(int percent)
        {
            try
            {
                percent = Math.Max(0, Math.Min(100, percent));
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        item.InvokeMethod("WmiSetBrightness", new object[] { 1, (byte)percent });
                        _lastKnown = percent;
                    }
                }
            }
            catch { }
        }
    }

    public static class DeviceControl
    {
        public static bool HasWifiAdapter()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool ToggleWifi()
        {
            if (!HasWifiAdapter()) return false;
            string script =
                "$a = Get-NetAdapter -Physical | Where-Object { $_.InterfaceDescription -match 'Wi-Fi|Wireless|WLAN|802.11' } | Select-Object -First 1; " +
                "if ($a) { if ($a.Status -eq 'Up') { Disable-NetAdapter -Name $a.Name -Confirm:$false } else { Enable-NetAdapter -Name $a.Name -Confirm:$false } }";
            return RunElevated(script);
        }

        public static bool HasBluetooth()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT PNPDeviceID FROM Win32_PnPEntity WHERE PNPClass='Bluetooth'"))
                {
                    int count = 0;
                    foreach (ManagementObject item in searcher.Get()) count++;
                    return count > 0;
                }
            }
            catch { return false; }
        }

        public static bool ToggleBluetooth()
        {
            if (!HasBluetooth()) return false;
            string script =
                "$d = Get-PnpDevice -Class Bluetooth | Where-Object { $_.FriendlyName -match 'Bluetooth' -and $_.InstanceId -match 'BTH|USB' } | Select-Object -First 1; " +
                "if ($d) { if ($d.Status -eq 'OK') { Disable-PnpDevice -InstanceId $d.InstanceId -Confirm:$false } else { Enable-PnpDevice -InstanceId $d.InstanceId -Confirm:$false } }";
            return RunElevated(script);
        }

        private static bool RunElevated(string script)
        {
            try
            {
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand " + encoded;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("DeviceControl.RunElevated", ex);
                return false;
            }
        }
    }
}
