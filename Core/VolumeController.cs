using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace FloatingTaskbarMenu.Core;

public class AudioDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
}

public class VolumeController
{
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState dwState, out IMMDeviceCollection ppDevices);
        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        [PreserveSig]
        int GetDeviceId([In] string pwstrId, out IMMDevice ppDevice);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, CLSCTX dwClsCtx, nint pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        [PreserveSig]
        int GetId([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [PreserveSig]
        int GetState(out DeviceState pdwState);
        [PreserveSig]
        int OpenPropertyStore(uint stgmAccess, out IPropertyStore ppProperties);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(nint pNotify);
        [PreserveSig]
        int UnregisterControlChangeNotify(nint pNotify);
        [PreserveSig]
        int GetChannelCount(out uint pnChannelCount);
        [PreserveSig]
        int SetMasterVolumeLevel(float fLevelDB, nint pguidEventContext);
        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, nint pguidEventContext);
        [PreserveSig]
        int GetMasterVolumeLevel(out float pfLevelDB);
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);
        [PreserveSig]
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, nint pguidEventContext);
        [PreserveSig]
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, nint pguidEventContext);
        [PreserveSig]
        int GetMute(out bool pbMute);
        [PreserveSig]
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
        [PreserveSig]
        int VolumeStepUp(nint pguidEventContext);
        [PreserveSig]
        int VolumeStepDown(nint pguidEventContext);
        [PreserveSig]
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);
        [PreserveSig]
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out uint pcDevices);
        [PreserveSig]
        int Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out uint cProps);
        [PreserveSig]
        int GetAt(uint iProp, out PROPERTYKEY pkey);
        [PreserveSig]
        int GetValue(ref PROPERTYKEY key, out PropVariant pv);
        [PreserveSig]
        int SetValue(ref PROPERTYKEY key, ref PropVariant pv);
        [PreserveSig]
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)]
        public ushort vt;
        [FieldOffset(8)]
        public IntPtr pointer;
    }

    [ComImport, Guid("1DE5E2FA-A693-4121-A8D8-000000000000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig]
        int SetDefaultEndpoint([In, MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, ERole role);
    }

    private enum EDataFlow { eRender, eCapture, eAll }
    private enum ERole { eConsole, eMultimedia, eCommunications }
    private enum DeviceState { Active = 0x01, Disabled = 0x02, NotPresent = 0x04, Unplugged = 0x08, All = 0x0F }
    private enum CLSCTX { INPROC_SERVER = 0x01, INPROC_HANDLER = 0x02, LOCAL_SERVER = 0x04 }
    private enum STGM { READ = 0, WRITE = 1, READWRITE = 2 }

    private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid PKEY_Device_FriendlyName = new Guid("a45c254e-df1c-4efd-8767-2d8701591ac1");
    private static readonly Guid IID_IPolicyConfig = new Guid("1DE5E2FA-A693-4121-A8D8-000000000000");
    private static readonly Guid CLSID_CPolicyConfigClient = new Guid("870AF99C-171D-4F5E-A44D-EF9367E1282C");

    public List<AudioDevice> GetAudioDevices()
    {
        var devices = new List<AudioDevice>();
        try
        {
            var enumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
            if (enumerator == null) return devices;

            int hr = enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.Active, out var collection);
            if (hr != 0 || collection == null) return devices;

            collection.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out var device);
                if (device == null) continue;

                device.GetId(out string deviceId);
                string deviceName = GetDeviceName(device);

                var audioDevice = new AudioDevice
                {
                    Id = deviceId,
                    Name = deviceName,
                    IsDefault = IsDefaultDevice(deviceId)
                };
                devices.Add(audioDevice);
            }

            return devices;
        }
        catch { return devices; }
    }

    public bool SetDefaultDevice(string deviceId)
    {
        try
        {
            var policyConfigType = Type.GetTypeFromCLSID(CLSID_CPolicyConfigClient);
            if (policyConfigType == null) return false;

            if (Activator.CreateInstance(policyConfigType) is not IPolicyConfig policyConfig)
                return false;

            int hr = policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            return hr == 0;
        }
        catch { return false; }
    }

    private string GetDeviceName(IMMDevice device)
    {
        try
        {
            device.OpenPropertyStore((uint)STGM.READ, out var props);
            if (props == null) return "Unknown Device";

            props.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                props.GetAt(i, out var key);
                if (key.fmtid == PKEY_Device_FriendlyName && key.pid == 14)
                {
                    props.GetValue(ref key, out var prop);
                    if (prop.pointer != nint.Zero)
                    {
                        return Marshal.PtrToStringUni(prop.pointer) ?? "Unknown Device";
                    }
                }
            }
            return "Unknown Device";
        }
        catch { return "Unknown Device"; }
    }

    private bool IsDefaultDevice(string deviceId)
    {
        try
        {
            var enumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
            if (enumerator == null) return false;

            int hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out var defaultDevice);
            if (hr != 0 || defaultDevice == null) return false;

            defaultDevice.GetId(out string defaultId);
            return defaultId == deviceId;
        }
        catch { return false; }
    }

    private IAudioEndpointVolume? GetVolumeInterface(string? deviceId = null)
    {
        try
        {
            var enumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
            if (enumerator == null) return null;

            IMMDevice device;
            if (string.IsNullOrEmpty(deviceId))
            {
                int hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
                if (hr != 0 || device == null) return null;
            }
            else
            {
                int hr = enumerator.GetDeviceId(deviceId, out device);
                if (hr != 0 || device == null) return null;
            }

            var iid = IID_IAudioEndpointVolume;
            int hr2 = device.Activate(ref iid, CLSCTX.INPROC_SERVER, nint.Zero, out var obj);
            if (hr2 != 0 || obj == null) return null;

            return obj as IAudioEndpointVolume;
        }
        catch { return null; }
    }

    public float? GetVolume(string? deviceId = null)
    {
        var vol = GetVolumeInterface(deviceId);
        if (vol == null) return null;
        vol.GetMasterVolumeLevelScalar(out float level);
        return level;
    }

    public void SetVolume(float level, string? deviceId = null)
    {
        var vol = GetVolumeInterface(deviceId);
        if (vol == null) return;
        vol.SetMasterVolumeLevelScalar(Math.Clamp(level, 0f, 1f), nint.Zero);
    }

    public bool? GetMute(string? deviceId = null)
    {
        var vol = GetVolumeInterface(deviceId);
        if (vol == null) return null;
        vol.GetMute(out bool mute);
        return mute;
    }

    public void SetMute(bool mute, string? deviceId = null)
    {
        var vol = GetVolumeInterface(deviceId);
        if (vol == null) return;
        vol.SetMute(mute, nint.Zero);
    }
}
