using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    static class Program
    {
        static NotifyIcon warning;
        static IPAddress ip;
        static int port;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        
        static void Main()
        {
            string conf = System.IO.File.ReadAllText("ipconf.ini");
            char[] delimiters = new char[]{'\r', '\n'};
            string[] vals = conf.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            ip = IPAddress.Parse(vals[0]);
            port = int.Parse(vals[1]);
            warning = new NotifyIcon();
            warning.Icon = Properties.Resources.icon;
            warning.Visible = true;
            warning.Text = "StipeStatus";
            Timer tmr = new Timer();
            MenuItem m1 = new MenuItem("Open Stipe Control Panel in browser");
            MenuItem m2 = new MenuItem("Manually check Stipe server again");
            MenuItem m3 = new MenuItem("Close StipeStatus");
            m1.Click += m1_Click;
            m2.Click += m2_Click;
            m3.Click += m3_Click;
            MenuItem[] marray = new MenuItem[3];
            marray[0] = m1;
            marray[1] = m2;
            marray[2] = m3;
            ContextMenu menu = new ContextMenu(marray);
            warning.ContextMenu = menu;
            tmr.Interval = 1800000;
            tmr.Tick += tmr_Tick;
            tmr.Start();
            tmr_Tick(null, null);
            Application.Run();
        }

        static void m3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        static void m2_Click(object sender, EventArgs e)
        {
            tmr_Tick(null, null);
        }

        static void m1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://stipe.com.au/?page=servers.php");
        }

        static void tmr_Tick(object sender, EventArgs e)
        {
            bool status = CheckServer(ip, port);
            if (!status)
            {
                warning.BalloonTipIcon = ToolTipIcon.Warning;
                warning.BalloonTipText = "Warning: Stipe server is down!";
                warning.BalloonTipTitle = "StipeStatus";
                warning.ShowBalloonTip(10000);
            }
            
        }

        static bool CheckServer(IPAddress ip, int port)
        {
            try
            {
                System.Net.Sockets.TcpClient tcp;
                tcp = new System.Net.Sockets.TcpClient();
                tcp.Connect(ip, port);
                tcp.Close();
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
