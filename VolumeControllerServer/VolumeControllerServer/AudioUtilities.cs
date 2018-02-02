using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VolumeControllerServer
{
    public static class AudioUtilities
    {
        private static int currentProcessId = Process.GetCurrentProcess().Id;
        #region Master Volume Manipulation

        /// <summary>
        /// Gets the current master volume in scalar values (percentage)
        /// </summary>
        /// <returns>-1 in case of an error, if successful the value will be between 0 and 100</returns>
        public static float GetMasterVolume()
        {
            Interfaces.IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return -1;

                masterVol.GetMasterVolumeLevelScalar(out float volumeLevel);
                return volumeLevel * 100;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Gets the mute state of the master volume. 
        /// While the volume can be muted the <see cref="GetMasterVolume"/> will still return the pre-muted volume value.
        /// </summary>
        /// <returns>false if not muted, true if volume is muted</returns>
        public static bool GetMasterVolumeMute()
        {
            Interfaces.IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return false;

                bool isMuted;
                masterVol.GetMute(out isMuted);
                return isMuted;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Sets the master volume to a specific level
        /// </summary>
        /// <param name="newLevel">Value between 0 and 100 indicating the desired scalar value of the volume</param>
        public static void SetMasterVolume(float newLevel)
        {
            Interfaces.IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return;

                masterVol.SetMasterVolumeLevelScalar(newLevel / 100, Guid.Empty);
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Increments or decrements the current volume level by the <see cref="stepAmount"/>.
        /// </summary>
        /// <param name="stepAmount">Value between -100 and 100 indicating the desired step amount. Use negative numbers to decrease
        /// the volume and positive numbers to increase it.</param>
        /// <returns>the new volume level assigned</returns>
        public static float StepMasterVolume(float stepAmount)
        {
            Interfaces.IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return -1;

                float stepAmountScaled = stepAmount / 100;

                // Get the level
                float volumeLevel;
                masterVol.GetMasterVolumeLevelScalar(out volumeLevel);

                // Calculate the new level
                float newLevel = volumeLevel + stepAmountScaled;
                newLevel = Math.Min(1, newLevel);
                newLevel = Math.Max(0, newLevel);

                masterVol.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);

                // Return the new volume level that was set
                return newLevel * 100;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Mute or unmute the master volume
        /// </summary>
        /// <param name="isMuted">true to mute the master volume, false to unmute</param>
        public static void SetMasterVolumeMute(bool isMuted)
        {
            Interfaces.IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return;

                masterVol.SetMute(isMuted, Guid.Empty);
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        /// <summary>
        /// Switches between the master volume mute states depending on the current state
        /// </summary>
        /// <returns>the current mute state, true if the volume was muted, false if unmuted</returns>
        public static bool ToggleMasterVolumeMute()
        {
            Interfaces.IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return false;

                bool isMuted;
                masterVol.GetMute(out isMuted);
                masterVol.SetMute(!isMuted, Guid.Empty);

                return !isMuted;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        private static Interfaces.IAudioEndpointVolume GetMasterVolumeObject()
        {
            Interfaces.IMMDeviceEnumerator deviceEnumerator = null;
            Interfaces.IMMDevice speakers = null;
            try
            {
                deviceEnumerator = (Interfaces.IMMDeviceEnumerator)(new Interfaces.MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(Interfaces.EDataFlow.eRender, Interfaces.ERole.eMultimedia, out speakers);

                Guid IID_IAudioEndpointVolume = typeof(Interfaces.IAudioEndpointVolume).GUID;
                object o;
                speakers.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero, out o);
                Interfaces.IAudioEndpointVolume masterVol = (Interfaces.IAudioEndpointVolume)o;

                return masterVol;
            }
            finally
            {
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }



        #endregion

        #region Individual Application Volume Manipulation

        public static float? GetApplicationVolume(int pid)
        {
            Interfaces.ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                throw new Exception("No application found for pid");

            float level;
            volume.GetMasterVolume(out level);
            Marshal.ReleaseComObject(volume);
            return level * 100;
        }

        public static bool? GetApplicationMute(int pid)
        {
            Interfaces.ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                throw new Exception("No application found for pid");

            bool mute;
            volume.GetMute(out mute);
            Marshal.ReleaseComObject(volume);
            return mute;
        }

        public static void SetApplicationVolume(int pid, float level)
        {
            Interfaces.ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                throw new Exception("No application found for pid");

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, guid);
            Marshal.ReleaseComObject(volume);
        }

        public static void SetApplicationMute(int pid, bool mute)
        {
            Interfaces.ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                throw new Exception("No application found for pid");

            Guid guid = Guid.Empty;
            volume.SetMute(mute, guid);
            Marshal.ReleaseComObject(volume);
        }

        private static Interfaces.ISimpleAudioVolume GetVolumeObject(int pid)
        {
            Interfaces.IMMDeviceEnumerator deviceEnumerator = null;
            Interfaces.IAudioSessionEnumerator sessionEnumerator = null;
            Interfaces.IAudioSessionManager2 mgr = null;
            Interfaces.IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (Interfaces.IMMDeviceEnumerator)(new Interfaces.MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(Interfaces.EDataFlow.eRender, Interfaces.ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(Interfaces.IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (Interfaces.IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                Interfaces.ISimpleAudioVolume volumeControl = null;
                for (int i = 0; i < count; ++i)
                {
                    Interfaces.IAudioSessionControl2 ctl = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        int cpid;
                        ctl.GetProcessId(out cpid);
                        ctl.GetDisplayName(out string val);
                        if (cpid == pid)
                        {
                            volumeControl = ctl as Interfaces.ISimpleAudioVolume;
                            break;
                        }
                    }
                    finally
                    {
                    }
                }

                return volumeControl;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        #endregion

        #region System sounds Volume Manipulation
        public static float? GetSystemSoundsVolume()
        {
            Interfaces.ISimpleAudioVolume volume = GetSystemSoundsVolumeObject();
            if (volume == null)
                throw new Exception("No session found for system sounds");

            float level;
            volume.GetMasterVolume(out level);
            Marshal.ReleaseComObject(volume);
            return level * 100;
        }

        public static bool? GetSystemSoundsMute()
        {
            Interfaces.ISimpleAudioVolume volume = GetSystemSoundsVolumeObject();
            if (volume == null)
                throw new Exception("No session found for system sounds");

            bool mute;
            volume.GetMute(out mute);
            Marshal.ReleaseComObject(volume);
            return mute;
        }

        public static void SetSystemSoundsVolume(float level)
        {
            Interfaces.ISimpleAudioVolume volume = GetSystemSoundsVolumeObject();
            if (volume == null)
                throw new Exception("No session found for system sounds");

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, guid);
            Marshal.ReleaseComObject(volume);
        }

        public static void SetSystemSoundsMute(bool mute)
        {
            Interfaces.ISimpleAudioVolume volume = GetSystemSoundsVolumeObject();
            if (volume == null)
                throw new Exception("No session found for system sounds");

            Guid guid = Guid.Empty;
            volume.SetMute(mute, guid);
            Marshal.ReleaseComObject(volume);
        }

        private static Interfaces.ISimpleAudioVolume GetSystemSoundsVolumeObject()
        {
            Interfaces.IMMDeviceEnumerator deviceEnumerator = null;
            Interfaces.IAudioSessionEnumerator sessionEnumerator = null;
            Interfaces.IAudioSessionManager2 mgr = null;
            Interfaces.IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (Interfaces.IMMDeviceEnumerator)(new Interfaces.MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(Interfaces.EDataFlow.eRender, Interfaces.ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(Interfaces.IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (Interfaces.IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                Interfaces.ISimpleAudioVolume volumeControl = null;
                for (int i = 0; i < count; ++i)
                {
                    Interfaces.IAudioSessionControl2 ctl = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);
                        ctl.GetDisplayName(out string val);
                        if (val.ToLower().Contains("@%systemroot%\\system32\\audiosrv.dll"))
                        {
                            volumeControl = ctl as Interfaces.ISimpleAudioVolume;
                            break;
                        }
                    }
                    finally
                    {
                    }
                }

                return volumeControl;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        #endregion

        private static Interfaces.IAudioSessionManager2 GetAudioSessionManager()
        {
            Interfaces.IMMDevice speakers = GetSpeakers();
            if (speakers == null)
                return null;

            // win7+ only
            object o;
            if (speakers.Activate(typeof(Interfaces.IAudioSessionManager2).GUID, Interfaces.CLSCTX.CLSCTX_ALL, IntPtr.Zero, out o) != 0 || o == null)
                return null;

            return o as Interfaces.IAudioSessionManager2;
        }

        public static AudioDevice GetSpeakersDevice()
        {
            return CreateDevice(GetSpeakers());
        }

        private static AudioDevice CreateDevice(Interfaces.IMMDevice dev)
        {
            if (dev == null)
                return null;

            string id;
            dev.GetId(out id);
            Interfaces.DEVICE_STATE state;
            dev.GetState(out state);
            Dictionary<string, object> properties = new Dictionary<string, object>();
            Interfaces.IPropertyStore store;
            dev.OpenPropertyStore(Interfaces.STGM.STGM_READ, out store);
            if (store != null)
            {
                int propCount;
                store.GetCount(out propCount);
                for (int j = 0; j < propCount; j++)
                {
                    if (store.GetAt(j, out Interfaces.PROPERTYKEY pk) == 0)
                    {
                        Interfaces.PROPVARIANT value = new Interfaces.PROPVARIANT();
                        int hr = store.GetValue(ref pk, ref value);
                        object v = value.GetValue();
                        try
                        {
                            if (value.vt != Interfaces.VARTYPE.VT_BLOB) // for some reason, this fails?
                            {
                                Interfaces.PropVariantClear(ref value);
                            }
                        }
                        catch
                        {
                        }
                        string name = pk.ToString();
                        properties[name] = v;
                    }
                }
            }
            return new AudioDevice(id, (AudioDeviceState)state, properties);
        }

        public static IList<AudioDevice> GetAllDevices()
        {
            List<AudioDevice> list = new List<AudioDevice>();
            Interfaces.IMMDeviceEnumerator deviceEnumerator = null;
            try
            {
                deviceEnumerator = (Interfaces.IMMDeviceEnumerator)(new Interfaces.MMDeviceEnumerator());
            }
            catch
            {
            }
            if (deviceEnumerator == null)
                return list;

            Interfaces.IMMDeviceCollection collection;
            deviceEnumerator.EnumAudioEndpoints(Interfaces.EDataFlow.eAll, Interfaces.DEVICE_STATE.MASK_ALL, out collection);
            if (collection == null)
                return list;

            int count;
            collection.GetCount(out count);
            for (int i = 0; i < count; i++)
            {
                Interfaces.IMMDevice dev;
                collection.Item(i, out dev);
                if (dev != null)
                {
                    list.Add(CreateDevice(dev));
                }
            }
            return list;
        }

        private static Interfaces.IMMDevice GetSpeakers()
        {
            // get the speakers (1st render + multimedia) device
            try
            {
                Interfaces.IMMDeviceEnumerator deviceEnumerator = (Interfaces.IMMDeviceEnumerator)(new Interfaces.MMDeviceEnumerator());
                Interfaces.IMMDevice speakers;
                deviceEnumerator.GetDefaultAudioEndpoint(Interfaces.EDataFlow.eRender, Interfaces.ERole.eMultimedia, out speakers);
                return speakers;
            }
            catch
            {
                return null;
            }
        }

        public static IList<AudioSession> GetAllSessions()
        {
            List<AudioSession> list = new List<AudioSession>();
            Interfaces.IAudioSessionManager2 mgr = GetAudioSessionManager();
            if (mgr == null)
                return list;

            Interfaces.IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            for (int i = 0; i < count; i++)
            {
                Interfaces.IAudioSessionControl2 ctl2;
                sessionEnumerator.GetSession(i, out ctl2);
                if (ctl2 != null)
                {
                    list.Add(new AudioSession(ctl2));
                }
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            return list;
        }

        public static AudioSession GetProcessSession()
        {
            foreach (AudioSession session in GetAllSessions())
            {
                if (session.ProcessId == currentProcessId)
                    return session;

                session.Dispose();
            }
            return null;
        }        
    }

    public sealed class AudioSession : IDisposable
    {
        private Interfaces.IAudioSessionControl2 _ctl;
        private Process _process;

        internal AudioSession(Interfaces.IAudioSessionControl2 ctl)
        {
            _ctl = ctl;
        }

        public Process Process
        {
            get
            {
                if (_process == null && ProcessId != 0)
                {
                    try
                    {
                        _process = Process.GetProcessById(ProcessId);
                    }
                    catch
                    {
                        // do nothing
                    }
                }
                return _process;
            }
        }

        public int ProcessId
        {
            get
            {
                CheckDisposed();
                int i;
                _ctl.GetProcessId(out i);
                return i;
            }
        }

        public string Identifier
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetSessionIdentifier(out s);
                return s;
            }
        }

        public string InstanceIdentifier
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetSessionInstanceIdentifier(out s);
                return s;
            }
        }

        public AudioSessionState State
        {
            get
            {
                CheckDisposed();
                AudioSessionState s;
                _ctl.GetState(out s);
                return s;
            }
        }

        public Guid GroupingParam
        {
            get
            {
                CheckDisposed();
                Guid g;
                _ctl.GetGroupingParam(out g);
                return g;
            }
            set
            {
                CheckDisposed();
                _ctl.SetGroupingParam(value, Guid.Empty);
            }
        }

        public string DisplayName
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetDisplayName(out s);
                return s;
            }
            set
            {
                CheckDisposed();
                string s;
                _ctl.GetDisplayName(out s);
                if (s != value)
                {
                    _ctl.SetDisplayName(value, Guid.Empty);
                }
            }
        }

        public string IconPath
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetIconPath(out s);
                return s;
            }
            set
            {
                CheckDisposed();
                string s;
                _ctl.GetIconPath(out s);
                if (s != value)
                {
                    _ctl.SetIconPath(value, Guid.Empty);
                }
            }
        }

        private void CheckDisposed()
        {
            if (_ctl == null)
                throw new ObjectDisposedException("Control");
        }

        public override string ToString()
        {
            string s = DisplayName;
            if (!string.IsNullOrEmpty(s))
                return "DisplayName: " + s;

            if (Process != null)
                return "Process: " + Process.ProcessName;

            return "Pid: " + ProcessId;
        }

        public void Dispose()
        {
            if (_ctl != null)
            {
                Marshal.ReleaseComObject(_ctl);
                _ctl = null;
            }
        }
    }

    public sealed class AudioDevice
    {
        internal AudioDevice(string id, AudioDeviceState state, IDictionary<string, object> properties)
        {
            Id = id;
            State = state;
            Properties = properties;
        }

        public string Id { get; private set; }
        public AudioDeviceState State { get; private set; }
        public IDictionary<string, object> Properties { get; private set; }

        public string Description
        {
            get
            {
                const string PKEY_Device_DeviceDesc = "{a45c254e-df1c-4efd-8020-67d146a850e0} 2";
                object value;
                Properties.TryGetValue(PKEY_Device_DeviceDesc, out value);
                return string.Format("{0}", value);
            }
        }

        public string ContainerId
        {
            get
            {
                const string PKEY_Devices_ContainerId = "{8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c} 2";
                object value;
                Properties.TryGetValue(PKEY_Devices_ContainerId, out value);
                return string.Format("{0}", value);
            }
        }

        public string EnumeratorName
        {
            get
            {
                const string PKEY_Device_EnumeratorName = "{a45c254e-df1c-4efd-8020-67d146a850e0} 24";
                object value;
                Properties.TryGetValue(PKEY_Device_EnumeratorName, out value);
                return string.Format("{0}", value);
            }
        }

        public string InterfaceFriendlyName
        {
            get
            {
                const string DEVPKEY_DeviceInterface_FriendlyName = "{026e516e-b814-414b-83cd-856d6fef4822} 2";
                object value;
                Properties.TryGetValue(DEVPKEY_DeviceInterface_FriendlyName, out value);
                return string.Format("{0}", value);
            }
        }

        public string FriendlyName
        {
            get
            {
                const string DEVPKEY_Device_FriendlyName = "{a45c254e-df1c-4efd-8020-67d146a850e0} 14";
                object value;
                Properties.TryGetValue(DEVPKEY_Device_FriendlyName, out value);
                return string.Format("{0}", value);
            }
        }

        public override string ToString()
        {
            return FriendlyName;
        }
    }

    public enum AudioSessionState
    {
        Inactive = 0,
        Active = 1,
        Expired = 2
    }

    public enum AudioDeviceState
    {
        Active = 0x1,
        Disabled = 0x2,
        NotPresent = 0x4,
        Unplugged = 0x8,
    }

    public enum AudioSessionDisconnectReason
    {
        DisconnectReasonDeviceRemoval = 0,
        DisconnectReasonServerShutdown = 1,
        DisconnectReasonFormatChanged = 2,
        DisconnectReasonSessionLogoff = 3,
        DisconnectReasonSessionDisconnected = 4,
        DisconnectReasonExclusiveModeOverride = 5
    }
}
