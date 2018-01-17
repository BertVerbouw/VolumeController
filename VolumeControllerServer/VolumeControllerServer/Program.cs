using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VolumeControllerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServer ws = new WebServer(SendAllVolumeData, "http://localhost:8081/get/");
            WebServer ws2 = new WebServer(ProcessVolumeRequest, "http://localhost:8081/set/");
            WebServer ws3 = new WebServer(ProcessMuteRequest, "http://localhost:8081/mute/");
            ws.Run();
            ws2.Run();
            ws3.Run();
            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
            ws2.Stop();
            ws3.Stop();
        }

        public static string SendAllVolumeData(HttpListenerRequest request)
        {
            List<AudioInfo> info = new List<AudioInfo>();
            try
            {
                //Master Volume
                info.Add(new AudioInfo()
                {
                    Name = "MasterVolume",
                    IsMuted = AudioUtilities.GetMasterVolumeMute(),
                    Volume = AudioUtilities.GetMasterVolume(),
                    Pid = -1
                });
                info.Add(new AudioInfo()
                {
                    Name = "SystemSoundsVolume",
                    IsMuted = AudioUtilities.GetSystemSoundsMute(),
                    Volume = AudioUtilities.GetSystemSoundsVolume(),
                    Pid = -2
                });
                foreach (AudioSession session in AudioUtilities.GetAllSessions().GroupBy(x => x.ProcessId).Select(g => g.First()))
                {
                    try
                    {
                        info.Add(new AudioInfo()
                        {
                            Name = session.Process.ProcessName,
                            Pid = session.ProcessId,
                            Volume = AudioUtilities.GetApplicationVolume(session.ProcessId),
                            IsMuted = AudioUtilities.GetApplicationMute(session.ProcessId)
                        });
                    }
                    catch
                    {
                    }
                }
            }
            catch { }
            return JsonConvert.SerializeObject(info.Distinct());
        }

        public static string ProcessVolumeRequest(HttpListenerRequest request)
        {
            string pid = request.QueryString["pid"];
            string volume = request.QueryString["vol"];
            if (pid != null && volume != null)
            {
                try
                {
                    if (pid == "-1")
                    {
                        AudioUtilities.SetMasterVolume(float.Parse(volume));
                    }
                    else if(pid == "-2")
                    {
                        AudioUtilities.SetSystemSoundsVolume(float.Parse(volume));
                    }
                    else
                    {
                        AudioUtilities.SetApplicationVolume(Int32.Parse(pid), float.Parse(volume));
                    }
                    return string.Format("<HTML><BODY>OK</BODY></HTML>");
                } catch
                {
                    return string.Format("<HTML><BODY>Failed to set volume to "+volume+" for pid "+pid+"</BODY></HTML>");
                }
            }
            else
            {
                return string.Format("<HTML><BODY>Parameters incorrect</BODY></HTML>");
            }
        }

        public static string ProcessMuteRequest(HttpListenerRequest request)
        {
            string pid = request.QueryString["pid"];
            string mute = request.QueryString["mute"];
            if (pid != null && mute != null)
            {
                try
                {
                    if (pid == "-1")
                    {
                        AudioUtilities.SetMasterVolumeMute(bool.Parse(mute));
                    }
                    else if (pid == "-2")
                    {
                        AudioUtilities.SetSystemSoundsMute(bool.Parse(mute));
                    }
                    else
                    {
                        AudioUtilities.SetApplicationMute(Int32.Parse(pid), bool.Parse(mute));
                    }
                    return string.Format("<HTML><BODY>OK</BODY></HTML>");
                }
                catch
                {
                    return string.Format("<HTML><BODY>Failed to mute pid " + pid + "</BODY></HTML>");
                }
            }
            else
            {
                return string.Format("<HTML><BODY>Parameters incorrect</BODY></HTML>");
            }
        }
    }
}
