using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.ComponentModel;

namespace VolumeControllerServer
{
    class Program
    {
        private static string _firewallRule = "VolumeController";
        private static int _port = 11000;
        private static string _currentIp = "";
        private static BackgroundWorker _volumeChecker = new BackgroundWorker();
        private static string _lastvolumedata = "";

        static void Main(string[] args)
        {
            try
            {
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length == 1)
                {
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        _currentIp = GetLocalIPAddress().ToString();
                        //AddFirewallRule();
                        InitializeNotifyIcon();
                        _volumeChecker.DoWork += _volumeChecker_DoWork;
                        _volumeChecker.RunWorkerAsync();
                        AsynchronousSocketListener.StartListening(_currentIp, _port);
                    }
                    else
                    {
                        MessageBox.Show("No network connection.", "Volume Controller Server");
                    }
                }
                else
                {
                    MessageBox.Show("Process already running.", "Volume Controller Server");
                }
            }
            catch(Exception e) {
                if (e.Message.Contains("E_ACCESSDENIED"))
                {
                    MessageBox.Show("Start the program as Administrator", "Not enough rights to start server");
                }
                else
                {
                    MessageBox.Show(e.ToString(), "Volume Controller Server");
                }
            }
        }        

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void _volumeChecker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                string newdata = GetAllVolumeData();
                if (newdata != _lastvolumedata)
                {
                    AsynchronousSocketListener.Send(newdata);
                }
                Thread.Sleep(50);
            }
        }

        private static void InitializeNotifyIcon()
        {
            Thread notifyThread = new Thread(
            delegate ()
            {
                string text = "Ip address: " + _currentIp + Environment.NewLine + "Port: " + _port;
                NotifyIcon tray = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                    Visible = true,
                    Text = text
                };
                tray.ShowBalloonTip(2000, "Volume Controller Server Running", "The server is running: "+Environment.NewLine+text, ToolTipIcon.Info);
                System.Windows.Forms.Application.Run();
            });
            notifyThread.Start();

        }

        private static void AddFirewallRule()
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

            List<INetFwRule> RuleList = new List<INetFwRule>();

            var currentProfiles = fwPolicy2.CurrentProfileTypes;

            foreach (INetFwRule rule in fwPolicy2.Rules)
            {
                if (rule.Name.IndexOf(_firewallRule) != -1)
                {
                    if (rule.LocalPorts == _port.ToString() && rule.Profiles == currentProfiles)
                    {
                        rule.Enabled = true;
                        return;
                    }
                }
            }
            
            // Let's create a new rule
            INetFwRule2 inboundRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            inboundRule.Enabled = true;
            //Allow through firewall
            inboundRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            //Using protocol TCP
            inboundRule.Protocol = 6; // TCP
            inboundRule.LocalPorts = _port.ToString();
            //Name of rule
            inboundRule.Name = _firewallRule;
            // ...//
            inboundRule.Profiles = currentProfiles;

            // Now add the rule
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Add(inboundRule);
        }

        internal static void ProcessRequest(string content)
        {
            try
            {
                string[] contentarr = content.Split('*');
                if(contentarr[0] == "mute")
                {
                    ProcessMuteRequest(Int32.Parse(contentarr[1]), Boolean.Parse(contentarr[2]));
                }
                else if(contentarr[1] == "vol")
                {
                    ProcessVolumeRequest(Int32.Parse(contentarr[1]), float.Parse(contentarr[2]));
                }
            }
            catch { }
        }

        public static string GetAllVolumeData()
        {
            List<AudioInfo> info = new List<AudioInfo>();
            try
            {
                //Master Volume
                info.Add(new AudioInfo()
                {
                    Name = "Master Volume",
                    IsMuted = AudioUtilities.GetMasterVolumeMute(),
                    Volume = AudioUtilities.GetMasterVolume(),
                    Pid = -1
                });
                info.Add(new AudioInfo()
                {
                    Name = "System Sounds",
                    IsMuted = AudioUtilities.GetSystemSoundsMute(),
                    Volume = AudioUtilities.GetSystemSoundsVolume(),
                    Pid = -2
                });
                foreach (AudioSession session in AudioUtilities.GetAllSessions().GroupBy(x => x.ProcessId).Select(g => g.First()))
                {
                    try
                    {
                        if (session.Process != null && session.Process.ProcessName.Length > 1)
                        {
                            info.Add(new AudioInfo()
                            {
                                Name = session.Process.ProcessName.First().ToString().ToUpper() + session.Process.ProcessName.Substring(1),
                                Pid = session.ProcessId,
                                Volume = AudioUtilities.GetApplicationVolume(session.ProcessId),
                                IsMuted = AudioUtilities.GetApplicationMute(session.ProcessId)
                            });
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch { }
            return JsonConvert.SerializeObject(info.Distinct());
        }

        public static void ProcessVolumeRequest(int pid, float volume)
        {
                try
                {
                    if (pid == -1)
                    {
                        AudioUtilities.SetMasterVolume(volume);
                    }
                    else if(pid ==-2)
                    {
                        AudioUtilities.SetSystemSoundsVolume(volume);
                    }
                    else
                    {
                        AudioUtilities.SetApplicationVolume(pid, volume);
                    }
                } catch
                {
                }
        }

        public static void ProcessMuteRequest(int pid, bool mute)
        {
                try
                {
                    if (pid ==-1)
                    {
                        AudioUtilities.SetMasterVolumeMute(mute);
                    }
                    else if (pid == -2)
                    {
                        AudioUtilities.SetSystemSoundsMute(mute);
                    }
                    else
                    {
                        AudioUtilities.SetApplicationMute(pid, mute);
                    }
                }
                catch
                {
                }
        }
    }
}
