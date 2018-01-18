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

namespace VolumeControllerServer
{
    class Program
    {
        private static string _firewallRule = "VolumeController";
        private static string _port = "8081";
        private static string _currentIp = "";
        private static Dictionary<string, Func<HttpListenerRequest, string>> _apiMethods = new Dictionary<string, Func<HttpListenerRequest, string>>()
        {
            { "/get/" , SendAllVolumeData },
            { "/set/" , ProcessVolumeRequest },
            { "/mute/" , ProcessMuteRequest }
        };
        private static List<WebServer> _webservers = new List<WebServer>();

        static void Main(string[] args)
        {
            try
            {
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length == 1)
                {
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        _currentIp = GetLocalIPAddress();
                        AddFirewallRule();
                        foreach (var apifunction in _apiMethods)
                        {
                            WebServer ws = new WebServer(apifunction.Value, "http://" + _currentIp + ":" + _port + apifunction.Key);
                            _webservers.Add(ws);
                            ws.Run();
                        }
                        InitializeNotifyIcon();
                        while (true)
                        {
                            Thread.Sleep(100);
                        }
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

        private static void InitializeNotifyIcon()
        {
            Thread notifyThread = new Thread(
            delegate ()
            {
                NotifyIcon tray = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                    Visible = true,
                    Text = "Ip address: " + _currentIp + Environment.NewLine + "Port: " + _port,
                    ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Shutdown Server", ExitApplication) })
                };
                tray.ShowBalloonTip(2000, "Volume Controller Server Running", "The server is running in the background, right click the icon to shut down", ToolTipIcon.Info);
                System.Windows.Forms.Application.Run();
            });
            notifyThread.Start();

        }

        private static void ExitApplication(object sender, EventArgs e)
        {
            foreach(WebServer server in _webservers)
            {
                server.Stop();
            }
            Environment.Exit(0);
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

        private static void AddFirewallRule()
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

            List<INetFwRule> RuleList = new List<INetFwRule>();

            foreach (INetFwRule rule in fwPolicy2.Rules)
            {
                if (rule.Name.IndexOf(_firewallRule) != -1)
                {
                    if (rule.LocalPorts == _port)
                    {
                        rule.Enabled = true;
                        return;
                    }
                }
            }
            
            var currentProfiles = fwPolicy2.CurrentProfileTypes;
            // Let's create a new rule
            INetFwRule2 inboundRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            inboundRule.Enabled = true;
            //Allow through firewall
            inboundRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            //Using protocol TCP
            inboundRule.Protocol = 6; // TCP
                                      //Port 81
            inboundRule.LocalPorts = _port;
            //Name of rule
            inboundRule.Name = _firewallRule;
            // ...//
            inboundRule.Profiles = currentProfiles;

            // Now add the rule
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Add(inboundRule);
        }

        public static string SendAllVolumeData(HttpListenerRequest request)
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
                                Name = session.Process.ProcessName.First().ToString().ToUpper() + session.Process.ProcessName.ToLower().Substring(1),
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
