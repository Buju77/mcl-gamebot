using System;
//using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace irc_bot_v2._0
{
    /// <summary>
    /// handles all the communication between the server and this program
    /// contains the socket and provides methods for listener and writer
    /// string receive(void)
    /// void send(string line)
    /// </summary>

    public class Network
    {
        #region class variables:
        protected const int MAX_CHARS = 510;//+2 = maximum number of characters sent to the server
        protected Socket sok;//the socket itself
        protected bool started;//if the connection fails this is false
        protected string msg;//contains sort of a "status message"
        #endregion

        #region properties:
        /// <summary>
        /// true if the connection is established, false if not
        /// </summary>
        public bool Started
        {
            get { return started; }
        }

        /// <summary>
        /// sort of a status message from the socket
        /// </summary>
        public string Message
        {
            get { return msg; }
        }
        #endregion

        #region methods:
        /// <summary>
        /// constructor
        /// </summary>
        /// <param in_name="hostname">the hostname or ip of the server</param>
        /// <param in_name="port">the port the server is listening to</param>
        public Network(string hostname, int port)
        {
            Connect(hostname, port);
        }//public Network(string hostname, int port)

        /// <summary>
        /// creates a socket to a server and establishes connection
        /// </summary>
        /// <param in_name="hostname">the hostname or ip of the server</param>
        /// <param in_name="port">the port the server is listening to</param>
        public void Connect(string hostname, int port)
        {
            msg = "";
            try
            {
                //get the ip-adress(es) of the server.
                IPHostEntry serverIPHE = Dns.GetHostEntry(hostname);
                //create the socket
                sok = ConnectSocket(serverIPHE, port, ref started);
            }//try
            catch (SocketException e)
            {
                started = false;
                msg = "Unable to connect: " + e.Message;
            }//catch(SocketException e)
            if (started)
            {
                msg = "Connected to: " + hostname + "(" + msg + "):" + port;
            }//if (started)
            else
            {
                if (msg.Equals(""))
                {
                    msg = "Unable to connect: No Server found with this IP / Port.";
                }//if (msg.Equals(""))
            }//else::if (started)
        }//public void Connect(string hostname, int port)

        /// <summary>
        /// destructor
        /// </summary>
        ~Network()
        {
            if (sok != null)
            {
                sok.Close();
            }
        }//~Network()

        /// <summary>
        /// method to receive data from the server
        /// </summary>
        /// <returns>a line received from the server</returns>
        public string Receive()
        {
            string line = "";
            string letter;
            while (true)
            {
                Byte[] letterB = new Byte[1];
                sok.Receive(letterB);//, 1, 0);
                letter = Encoding.ASCII.GetString(letterB);//,0,1);
                if (letter == "\n")
                {
                    if (line.EndsWith("\r"))
                    {
                        return line.Substring(0, line.Length - 1);
                    }
                    else
                    {
                        return line;
                    }
                }//if (letter=="\n")
                else
                {
                    line = line + letter;
                }//else::if (letter=="\n")
            }//while (true)
        }//public string read()

        /// <summary>
        /// method to send data to the server
        /// </summary>
        /// <param in_name="line">a line to send to the server</param>
        public void Send(string line)
        {
            if (line.Length > MAX_CHARS)
            {
                line = line.Substring(0, MAX_CHARS);
            }//if (line.Length > MAX_CHARS)
            Byte[] lineB;
            lineB = Encoding.ASCII.GetBytes(line + "\r\n");//converting line to bytes
            sok.Send(lineB);//sending bytes to server
        }//public void Send(string line)

        /// <summary>
        /// function to resolve hostname and create a socket
        /// </summary>
        /// <param in_name="server">hostname or ip of server</param>
        /// <param in_name="port">port the server listens to</param>
        /// <param in_name="connected">referenced boolean, returns true if connected</param>
        /// <returns></returns>
        protected Socket ConnectSocket(IPHostEntry server, int port, ref bool connected)
        {
            Socket s = null;//temporary socket for return value
            connected = false;

            foreach (IPAddress address in server.AddressList)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket temp = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    //this try block catches the SochetException if the server is
                    //not running when the client starts up.
                    temp.Connect(ipe);
                }//try
                catch (SocketException e)
                {
                    Console.WriteLine(e.Message);
                }//catch(SocketException e)

                if (temp.Connected)
                {
                    s = temp;
                    msg = System.Convert.ToString(ipe.Address);
                    connected = true;
                    break;
                }//if(temp.Connected)
                else
                {
                    connected = false;
                    continue;
                }//else::if(temp.Connected)
            }//foreach(IPAddress address in server.AddressList)
            return s;
        }//ConnectSocket;
        #endregion
    }
}
