using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MineCraftServerChecker
{
    public class MinecraftClient
    {

        /// <summary>
        /// The multicast port that minecraft uses
        /// </summary>
        const int ANNOUNCE_MULTICAST_PORT = 4445;
        /// <summary>
        /// Announces a world to the multicast group, so that it shows up in lan worlds
        /// </summary>
        /// <param name="motd">The server MOTD, or description</param>
        /// <param name="port">The port to use. The default is stored in ANNOUNCE_MULTICAST_PORT</param>
        public static void AnnounceWorld(string motd, int port)
        {
            byte[] ANNOUNCE_MULTICAST_IP = { 0xE0, 0x0, 0x2, 0x3C };
            byte[] motd_bytes = ASCIIEncoding.ASCII.GetBytes("[MOTD]" + motd + "[/MOTD]");
            byte[] ip_bytes = ASCIIEncoding.ASCII.GetBytes("[AD]" + port + "[/AD]");

            Socket sk = new Socket(SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[motd_bytes.Length + ip_bytes.Length];
            motd_bytes.CopyTo(buffer, 0);
            ip_bytes.CopyTo(buffer, motd_bytes.Length);

            sk.SendTo(buffer, new IPEndPoint(new IPAddress(ANNOUNCE_MULTICAST_IP), ANNOUNCE_MULTICAST_PORT));
        }


        /// <summary>
        /// A function to read in a byte array up until a certain point
        /// </summary>
        /// <param name="buffer">The buffer to read out of</param>
        /// <param name="start">Start index</param>
        /// <param name="max">Max number to read overall</param>
        /// <param name="terminator">The function stops reading data once this value is found</param>
        /// <param name="TerminatorIndex">A ref int that will contain the value of the index of the found termination byte</param>
        /// <returns>Returns the data specified. If none found, returns the original array and a ref int TerminatorIndex of -1</returns>
        public static byte[] ReadUntilTerminatorChar(byte[] buffer, int start, int max, byte terminator, ref int TerminatorIndex)
        {
            byte[] buf = new byte[max];

            if (max < start) return null;

            for (int i = start; i < max; i++)
            {
                byte cur = buffer[i];
                if (cur == terminator)
                {
                    TerminatorIndex = i;
                    Array.Resize(ref buf, i - start);
                    Array.Copy(buffer, start, buf, 0, i - start);
                    return buf;
                }
            }
            Array.Copy(buffer, start, buf, 0, max - start);
            TerminatorIndex = -1;
            return buf;
        }


        /// <summary>
        /// The class that holds all the information about a Minecraft server
        /// </summary>
        public class ServerInfo
        {
            private IPAddress _addr;
            private string _ident;

            public string Identifier
            {
                get { return _ident; }
                set { _ident = value; }
            }

            int _max;

            bool _isup;
            int _latency;

            public int Latency
            {
                get { return _latency; }
                set { _latency = value; }
            }

            public bool IsUp
            {
                get { return _isup; }
                set { _isup = value; }
            }

            public int Max
            {
                get { return _max; }
                set { _max = value; }
            }
            int _online;

            public int CurrentlyOnline
            {
                get { return _online; }
                set { _online = value; }
            }
            private int _port;

            public int Port
            {
                get { return _port; }
                set { _port = value; }
            }
            private string _motd;

            public string Motd
            {
                get { return _motd; }
                set { _motd = value; }
            }


            public IPAddress Host
            {
                get { return _addr; }
            }

            public override string ToString()
            {
                if (_isup) _getPing();
                return "Server: " + Identifier + ":" + Port.ToString() + "\n" +
                    "Players: " + CurrentlyOnline.ToString() + "/" + Max.ToString() + "\n" +
                    "Ping: " + Latency.ToString();
            }


            private void _getPing()
            {
                Ping ping = new Ping();
                PingReply pr;
                pr = ping.Send(this._addr);
                if (pr.Status == IPStatus.Success)
                {
                    _latency = (int)pr.RoundtripTime;
                }
            }

            /// <summary>
            /// Populates the class fields by connecting to a minecraft server and reading data. Sets IsUp to false if failed
            /// </summary>
            /// <param name="host"></param>
            /// <param name="port"></param>
            public ServerInfo(IPAddress host, int port,string ident="")
            {
                _setinfo(host, port, ident);
            }

            private void _setinfo(IPAddress host,int port, string ident="")
            {

                TcpClient tc = new TcpClient();
                try
                {
                    tc.Connect(host, port);
                }
                catch (Exception)
                {
                    _isup = false;
                    return;

                }
                _isup = true;

                tc.GetStream().WriteByte(0xFE);

                byte response = (byte)tc.GetStream().ReadByte();
                if (response != 0xFF) { return; } //Not a mc server

                tc.GetStream().ReadByte();
                tc.GetStream().ReadByte(); //seek past empty bytes

                byte[] buffer = new byte[48];
                int responseLen = tc.GetStream().Read(buffer, 0, 48);


                Array.Resize<byte>(ref buffer, responseLen);



                string[] splits = ByteSplit(buffer, 167);

                _motd = splits[0];
                _max = int.Parse(splits[2]);
                _online = int.Parse(splits[1]);
                _addr = host;
                _port = port;
                _getPing();
                _ident = (ident == "" ?  SelectIPv4Address(Dns.GetHostEntry(host).AddressList).ToString() : ident);
                
                
            }


            /// <summary>
            /// Returns a section of the string separated by a sep character. Accepts unicode byte input
            /// </summary>
            /// <param name="input"></param>
            /// <param name="split"></param>
            /// <returns></returns>
            public static string[] ByteSplit(byte[] input, byte split)
            {
                return Encoding.BigEndianUnicode.GetString(input).Split(new char[] { (char)split });
            }

            public static IPAddress SelectIPv4Address(IPAddress[] list)
            {
                foreach (IPAddress ip in list)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
                }
                return null;
            }





        }
    }
}
