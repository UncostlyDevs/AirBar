using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FloatingTaskbarMenu.Core;

public class WifiNetwork
{
    public string Ssid { get; set; } = "";
    public string Bssid { get; set; } = "";
    public int SignalStrength { get; set; }
    public bool IsSecure { get; set; }
    public bool IsConnected { get; set; }
}

public class WifiManager
{
    private IntPtr _clientHandle = IntPtr.Zero;
    private uint _negotiatedVersion = 0;

    #region Native WiFi API P/Invoke

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanScan(IntPtr hClientHandle, IntPtr pInterfaceGuid, IntPtr pDot11Ssid, IntPtr pIeData, IntPtr pReserved);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanGetAvailableNetworkList(IntPtr hClientHandle, IntPtr pInterfaceGuid, uint dwFlags, IntPtr pReserved, out IntPtr ppAvailableNetworkList);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanConnect(IntPtr hClientHandle, IntPtr pInterfaceGuid, ref WLAN_CONNECTION_PARAMETERS pConnectionParameters, IntPtr pReserved);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanDisconnect(IntPtr hClientHandle, IntPtr pInterfaceGuid, IntPtr pReserved);

    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern void WlanFreeMemory(IntPtr pMemory);

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_INTERFACE_INFO
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string InterfaceDescription;
        public WLAN_INTERFACE_STATE isState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_INTERFACE_INFO_LIST
    {
        public uint dwNumberOfItems;
        public uint dwIndex;
        public WLAN_INTERFACE_INFO[] InterfaceInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_AVAILABLE_NETWORK
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] dot11Ssid;
        public uint dot11BssType;
        public uint dot11Bssid;
        public uint dot11NetworkType;
        public uint dot11SignalQuality;
        public bool bSecurityEnabled;
        public uint dwReserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] dot11SsidMac;
        public uint bInfrastructure;
        public uint dot11PhyType;
        public uint dot11PhyIndex;
        public uint wlanSignalQuality;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_AVAILABLE_NETWORK_LIST
    {
        public uint dwNumberOfItems;
        public uint dwIndex;
        public WLAN_AVAILABLE_NETWORK[] Network;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_CONNECTION_PARAMETERS
    {
        public WLAN_CONNECTION_MODE wlanConnectionMode;
        public string strProfile;
        public DOT11_SSID dot11Ssid;
        public DOT11_BSSID_LIST pDot11BssidList;
        public DOT11_BSS_TYPE dot11BssType;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DOT11_SSID
    {
        public uint uSSIDLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ucSSID;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DOT11_BSSID_LIST
    {
        public uint uCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public DOT11_MAC_ADDRESS[] BSSID;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DOT11_MAC_ADDRESS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] ucAddress;
    }

    private enum WLAN_INTERFACE_STATE
    {
        wlan_interface_state_not_ready,
        wlan_interface_state_connected,
        wlan_interface_state_ad_hoc_network_formed,
        wlan_interface_state_disconnecting,
        wlan_interface_state_disconnected,
        wlan_interface_state_interface_deleted
    }

    private enum WLAN_CONNECTION_MODE
    {
        wlan_connection_mode_profile,
        wlan_connection_mode_temporary_profile,
        wlan_connection_mode_discovery_secure,
        wlan_connection_mode_discovery_unsecure,
        wlan_connection_mode_auto,
        wlan_connection_mode_invalid
    }

    private enum DOT11_BSS_TYPE
    {
        dot11_BSS_type_infrastructure,
        dot11_BSS_type_independent,
        dot11_BSS_type_any
    }

    #endregion

    public bool Initialize()
    {
        try
        {
            uint result = WlanOpenHandle(2, IntPtr.Zero, out _negotiatedVersion, out _clientHandle);
            return result == 0;
        }
        catch { return false; }
    }

    public void Cleanup()
    {
        if (_clientHandle != IntPtr.Zero)
        {
            WlanCloseHandle(_clientHandle, IntPtr.Zero);
            _clientHandle = IntPtr.Zero;
        }
    }

    public List<WifiNetwork> GetAvailableNetworks()
    {
        var networks = new List<WifiNetwork>();

        try
        {
            if (_clientHandle == IntPtr.Zero)
                return networks;

            uint result = WlanEnumInterfaces(_clientHandle, IntPtr.Zero, out IntPtr interfaceListPtr);
            if (result != 0)
                return networks;

            var interfaceList = Marshal.PtrToStructure<WLAN_INTERFACE_INFO_LIST>(interfaceListPtr);

            for (uint i = 0; i < interfaceList.dwNumberOfItems; i++)
            {
                var interfaceInfo = interfaceList.InterfaceInfo[i];
                IntPtr interfaceGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                Marshal.StructureToPtr(interfaceInfo.InterfaceGuid, interfaceGuidPtr, false);

                result = WlanGetAvailableNetworkList(_clientHandle, interfaceGuidPtr, 2, IntPtr.Zero, out IntPtr networkListPtr);
                if (result == 0)
                {
                    var networkList = Marshal.PtrToStructure<WLAN_AVAILABLE_NETWORK_LIST>(networkListPtr);

                    for (uint j = 0; j < networkList.dwNumberOfItems; j++)
                    {
                        var wlanNetwork = networkList.Network[j];
                        var ssid = Encoding.ASCII.GetString(wlanNetwork.dot11Ssid, 0, (int)wlanNetwork.dot11Ssid[0]).TrimEnd('\0');

                        var network = new WifiNetwork
                        {
                            Ssid = ssid,
                            SignalStrength = (int)wlanNetwork.dot11SignalQuality,
                            IsSecure = wlanNetwork.bSecurityEnabled,
                            IsConnected = wlanNetwork.wlanSignalQuality == 100 // Simplified check
                        };
                        networks.Add(network);
                    }

                    WlanFreeMemory(networkListPtr);
                }

                Marshal.FreeHGlobal(interfaceGuidPtr);
            }

            WlanFreeMemory(interfaceListPtr);
        }
        catch { }

        return networks;
    }

    public void Scan()
    {
        try
        {
            if (_clientHandle == IntPtr.Zero)
                return;

            uint result = WlanEnumInterfaces(_clientHandle, IntPtr.Zero, out IntPtr interfaceListPtr);
            if (result != 0)
                return;

            var interfaceList = Marshal.PtrToStructure<WLAN_INTERFACE_INFO_LIST>(interfaceListPtr);

            for (uint i = 0; i < interfaceList.dwNumberOfItems; i++)
            {
                var interfaceInfo = interfaceList.InterfaceInfo[i];
                IntPtr interfaceGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                Marshal.StructureToPtr(interfaceInfo.InterfaceGuid, interfaceGuidPtr, false);

                WlanScan(_clientHandle, interfaceGuidPtr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                Marshal.FreeHGlobal(interfaceGuidPtr);
            }

            WlanFreeMemory(interfaceListPtr);
        }
        catch { }
    }
}
