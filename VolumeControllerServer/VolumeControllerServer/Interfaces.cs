using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VolumeControllerServer
{
    public class Interfaces
    {
        [DllImport("ole32.dll")]
        internal static extern int PropVariantClear(ref PROPVARIANT pvar);

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        internal class MMDeviceEnumerator
        {
        }

        [Flags]
        internal enum CLSCTX
        {
            CLSCTX_INPROC_SERVER = 0x1,
            CLSCTX_INPROC_HANDLER = 0x2,
            CLSCTX_LOCAL_SERVER = 0x4,
            CLSCTX_REMOTE_SERVER = 0x10,
            CLSCTX_ALL = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
        }

        internal enum STGM
        {
            STGM_READ = 0x00000000,
        }

        internal enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
        }

        internal enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications,
        }

        internal enum DEVICE_STATE
        {
            ACTIVE = 0x00000001,
            DISABLED = 0x00000002,
            NOTPRESENT = 0x00000004,
            UNPLUGGED = 0x00000008,
            MASK_ALL = 0x0000000F
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROPERTYKEY
        {
            public Guid fmtid;
            public int pid;

            public override string ToString()
            {
                return fmtid.ToString("B") + " " + pid;
            }
        }

        // NOTE: we only define what we handle
        [Flags]
        internal enum VARTYPE : short
        {
            VT_I4 = 3,
            VT_BOOL = 11,
            VT_UI4 = 19,
            VT_LPWSTR = 31,
            VT_BLOB = 65,
            VT_CLSID = 72,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROPVARIANT
        {
            public VARTYPE vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public PROPVARIANTunion union;

            public object GetValue()
            {
                switch (vt)
                {
                    case VARTYPE.VT_BOOL:
                        return union.boolVal != 0;

                    case VARTYPE.VT_LPWSTR:
                        return Marshal.PtrToStringUni(union.pwszVal);

                    case VARTYPE.VT_UI4:
                        return union.lVal;

                    case VARTYPE.VT_CLSID:
                        return (Guid)Marshal.PtrToStructure(union.puuid, typeof(Guid));

                    default:
                        return vt.ToString() + ":?";
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct PROPVARIANTunion
        {
            [FieldOffset(0)]
            public int lVal;
            [FieldOffset(0)]
            public ulong uhVal;
            [FieldOffset(0)]
            public short boolVal;
            [FieldOffset(0)]
            public IntPtr pwszVal;
            [FieldOffset(0)]
            public IntPtr puuid;
        }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(EDataFlow dataFlow, DEVICE_STATE dwStateMask, out IMMDeviceCollection ppDevices);

            [PreserveSig]
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

            [PreserveSig]
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

            [PreserveSig]
            int RegisterEndpointNotificationCallback(IMMNotificationClient pClient);

            [PreserveSig]
            int UnregisterEndpointNotificationCallback(IMMNotificationClient pClient);
        }

        [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMNotificationClient
        {
            void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, DEVICE_STATE dwNewState);
            void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
            void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
            void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId);
            void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, PROPERTYKEY key);
        }

        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDeviceCollection
        {
            [PreserveSig]
            int GetCount(out int pcDevices);

            [PreserveSig]
            int Item(int nDevice, out IMMDevice ppDevice);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDevice
        {
            [PreserveSig]
            int Activate([MarshalAs(UnmanagedType.LPStruct)] Guid riid, CLSCTX dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

            [PreserveSig]
            int OpenPropertyStore(STGM stgmAccess, out IPropertyStore ppProperties);

            [PreserveSig]
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

            [PreserveSig]
            int GetState(out DEVICE_STATE pdwState);
        }

        [Guid("6f79d558-3e96-4549-a1d1-7d75d2288814"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyDescription
        {
            [PreserveSig]
            int GetPropertyKey(out PROPERTYKEY pkey);

            [PreserveSig]
            int GetCanonicalName(out IntPtr ppszName);

            [PreserveSig]
            int GetPropertyType(out short pvartype);

            [PreserveSig]
            int GetDisplayName(out IntPtr ppszName);

            // WARNING: the rest is undefined. you *can't* implement it, only use it.
        }

        [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPropertyStore
        {
            [PreserveSig]
            int GetCount(out int cProps);

            [PreserveSig]
            int GetAt(int iProp, out PROPERTYKEY pkey);

            [PreserveSig]
            int GetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);

            [PreserveSig]
            int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);

            [PreserveSig]
            int Commit();
        }

        [Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager
        {
            [PreserveSig]
            int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

            [PreserveSig]
            int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);
        }

        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionManager2
        {
            [PreserveSig]
            int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

            [PreserveSig]
            int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);

            [PreserveSig]
            int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

            [PreserveSig]
            int RegisterSessionNotification(IAudioSessionNotification SessionNotification);

            [PreserveSig]
            int UnregisterSessionNotification(IAudioSessionNotification SessionNotification);

            int RegisterDuckNotificationNotImpl();
            int UnregisterDuckNotificationNotImpl();
        }

        [Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionNotification
        {
            void OnSessionCreated(IAudioSessionControl NewSession);
        }

        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionEnumerator
        {
            [PreserveSig]
            int GetCount(out int SessionCount);

            [PreserveSig]
            int GetSession(int SessionCount, out IAudioSessionControl2 Session);
        }

        [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionControl2
        {
            // IAudioSessionControl
            [PreserveSig]
            int GetState(out AudioSessionState pRetVal);

            [PreserveSig]
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetGroupingParam(out Guid pRetVal);

            [PreserveSig]
            int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

            [PreserveSig]
            int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

            // IAudioSessionControl2
            [PreserveSig]
            int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int GetProcessId(out int pRetVal);

            [PreserveSig]
            int IsSystemSoundsSession();

            [PreserveSig]
            int SetDuckingPreference(bool optOut);
        }

        [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionControl
        {
            [PreserveSig]
            int GetState(out AudioSessionState pRetVal);

            [PreserveSig]
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetGroupingParam(out Guid pRetVal);

            [PreserveSig]
            int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

            [PreserveSig]
            int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);
        }

        [Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionEvents
        {
            void OnDisplayNameChanged([MarshalAs(UnmanagedType.LPWStr)] string NewDisplayName, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            void OnIconPathChanged([MarshalAs(UnmanagedType.LPWStr)] string NewIconPath, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            void OnSimpleVolumeChanged(float NewVolume, bool NewMute, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            void OnChannelVolumeChanged(int ChannelCount, IntPtr NewChannelVolumeArray, int ChangedChannel, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            void OnGroupingParamChanged([MarshalAs(UnmanagedType.LPStruct)] Guid NewGroupingParam, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            void OnStateChanged(AudioSessionState NewState);
            void OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason);
        }

        [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ISimpleAudioVolume
        {
            [PreserveSig]
            int SetMasterVolume(float fLevel, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetMasterVolume(out float pfLevel);

            [PreserveSig]
            int SetMute(bool bMute, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetMute(out bool pbMute);
        }

        // http://netcoreaudio.codeplex.com/SourceControl/latest#trunk/Code/CoreAudio/Interfaces/IAudioEndpointVolume.cs
        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioEndpointVolume
        {
            [PreserveSig]
            int NotImpl1();

            [PreserveSig]
            int NotImpl2();

            /// <summary>
            /// Gets a count of the channels in the audio stream.
            /// </summary>
            /// <param name="channelCount">The number of channels.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetChannelCount(
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

            /// <summary>
            /// Sets the master volume level of the audio stream, in decibels.
            /// </summary>
            /// <param name="level">The new master volume level in decibels.</param>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int SetMasterVolumeLevel(
                [In] [MarshalAs(UnmanagedType.R4)] float level,
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Sets the master volume level, expressed as a normalized, audio-tapered value.
            /// </summary>
            /// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int SetMasterVolumeLevelScalar(
                [In] [MarshalAs(UnmanagedType.R4)] float level,
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Gets the master volume level of the audio stream, in decibels.
            /// </summary>
            /// <param name="level">The volume level in decibels.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetMasterVolumeLevel(
                [Out] [MarshalAs(UnmanagedType.R4)] out float level);

            /// <summary>
            /// Gets the master volume level, expressed as a normalized, audio-tapered value.
            /// </summary>
            /// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetMasterVolumeLevelScalar(
                [Out] [MarshalAs(UnmanagedType.R4)] out float level);

            /// <summary>
            /// Sets the volume level, in decibels, of the specified channel of the audio stream.
            /// </summary>
            /// <param name="channelNumber">The channel number.</param>
            /// <param name="level">The new volume level in decibels.</param>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int SetChannelVolumeLevel(
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
                [In] [MarshalAs(UnmanagedType.R4)] float level,
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
            /// </summary>
            /// <param name="channelNumber">The channel number.</param>
            /// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int SetChannelVolumeLevelScalar(
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
                [In] [MarshalAs(UnmanagedType.R4)] float level,
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Gets the volume level, in decibels, of the specified channel in the audio stream.
            /// </summary>
            /// <param name="channelNumber">The zero-based channel number.</param>
            /// <param name="level">The volume level in decibels.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetChannelVolumeLevel(
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
                [Out] [MarshalAs(UnmanagedType.R4)] out float level);

            /// <summary>
            /// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
            /// </summary>
            /// <param name="channelNumber">The zero-based channel number.</param>
            /// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetChannelVolumeLevelScalar(
                [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
                [Out] [MarshalAs(UnmanagedType.R4)] out float level);

            /// <summary>
            /// Sets the muting state of the audio stream.
            /// </summary>
            /// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int SetMute(
                [In] [MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Gets the muting state of the audio stream.
            /// </summary>
            /// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetMute(
                [Out] [MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

            /// <summary>
            /// Gets information about the current step in the volume range.
            /// </summary>
            /// <param name="step">The current zero-based step index.</param>
            /// <param name="stepCount">The total number of steps in the volume range.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetVolumeStepInfo(
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 step,
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

            /// <summary>
            /// Increases the volume level by one step.
            /// </summary>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int VolumeStepUp(
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Decreases the volume level by one step.
            /// </summary>
            /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int VolumeStepDown(
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

            /// <summary>
            /// Queries the audio endpoint device for its hardware-supported functions.
            /// </summary>
            /// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int QueryHardwareSupport(
                [Out] [MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

            /// <summary>
            /// Gets the volume range of the audio stream, in decibels.
            /// </summary>
            /// <param name="volumeMin">The minimum volume level in decibels.</param>
            /// <param name="volumeMax">The maximum volume level in decibels.</param>
            /// <param name="volumeStep">The volume increment level in decibels.</param>
            /// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
            [PreserveSig]
            int GetVolumeRange(
                [Out] [MarshalAs(UnmanagedType.R4)] out float volumeMin,
                [Out] [MarshalAs(UnmanagedType.R4)] out float volumeMax,
                [Out] [MarshalAs(UnmanagedType.R4)] out float volumeStep);
        }
    }
}
