using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;

namespace AudioControlApp
{
    /// <summary>
    /// Delegate definition to handle events on the socket
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void MulticastSocketEventHandler(object sender, MulticastSocketEventsArgs e);

    /// <summary>
    /// Arguments that go with the MulticastSocketEventHandler
    /// </summary>
    public class MulticastSocketEventsArgs : EventArgs
    {
        public int id = 0;
        public string name = "";
        public StringBuilder data = new StringBuilder(); // Received data String.
    }

    /// <summary>
    /// Socket state object necessary for asynchronous send and receive
    /// </summary>
    public class MulticastSocketStateObject
    {
        public bool connected = false;// ID received flag
        public Socket workSocket = null;	// Client socket.

        public byte[] buffer = new byte[MulticastSocket.UDP_BUFFERSIZE]; // Receive buffer.

        public StringBuilder sb = new StringBuilder();	// Received data String.
        public string id = String.Empty; // Host or conversation ID
        public DateTime TimeStamp;

        public static int BufferSize
        {
            get { return MulticastSocket.UDP_BUFFERSIZE; }
        }

    }

    /// <summary>
    /// UDP asynchronous multicast socket
    /// </summary>
    public class MulticastSocket
    {
        private Socket UDPSocket;//the socket
        private IPAddress Target_IP;//the ip address the socket works on
        private int Target_Port;//the port the socket with broadcast on

        //private static int buffersize = 512;
        //private static int buffersize = 16384;
        private static int buffersize = 32768;
        public static int UDP_BUFFERSIZE
        {
            get { return buffersize; }
            set { buffersize = value; }
        }


        private static Encoding enctype = Encoding.Unicode;
        public static Encoding UDP_ENCODING
        {
            get { return enctype; }
            set { enctype = value; }
        }

        private string ip;
        private int port;
        private bool serveronly;

        private object syncObject = new Object();//synchronization object for thread-safe access to members

        private bool shuttingdown = false;//is the socket shutting down

        public event MulticastSocketEventHandler OnData;//Data event
        public event MulticastSocketEventHandler OnConnect;//Data event

        public event MulticastSocketEventHandler OnError;//Error event

        public bool connected;//is the socket in connected/bound state
        public bool multicast = true;//hack to allow for not multicast sockets

        System.Timers.Timer reconnectTimer;

        /// <summary>
        /// Constructor, takes ip, port and serveronly
        /// If serveronly is true then the receive "thread" is not started
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="serverOnly"></param>
        public MulticastSocket()
        {

        }

        /// <summary>
        /// Shutdown the socket
        /// </summary>
        public void Destroy()
        {
            shuttingdown = true;
            if (reconnectTimer != null) reconnectTimer.Stop();
            if (connected && UDPSocket != null)
                UDPSocket.Shutdown(SocketShutdown.Both);
        }

        /// <summary>
        /// Connect to IP and Port, will try again every second
        /// </summary>
        /// <param name="ip">ip address to "connect" on</param>
        /// <param name="port">port</param>
        /// <param name="serverOnly">if true will not listen for data, else also listen for data</param>
        public void Connect(string ip, int port, bool serverOnly)
        {
            this.ip = ip;
            this.port = port;
            this.serveronly = serverOnly;

            if (!tryconnect())
            {
                reconnectTimer = new System.Timers.Timer();
                reconnectTimer.Interval = 1000;
                reconnectTimer.AutoReset = true;
                reconnectTimer.Elapsed += new System.Timers.ElapsedEventHandler(reconnectTimer_Elapsed);
                reconnectTimer.Start();
            }
        }

        void reconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (tryconnect())
            {
                reconnectTimer.Stop();
            }
        }

        /// <summary>
        /// try to connect
        /// </summary>
        /// <returns></returns>
        private bool tryconnect()
        {
            //nothing should go wrong in here
            try
            {
                //Socket creation, regular UDP socket
                UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Target_IP = IPAddress.Parse(ip);
                Target_Port = port;

                //recieve data from any source
                IPEndPoint LocalHostIPEnd;

                if (multicast || serveronly)
                {
                    LocalHostIPEnd = new IPEndPoint(IPAddress.Any, Target_Port);
                }
                else
                {
                    LocalHostIPEnd = new IPEndPoint(Target_IP, Target_Port);
                }

                //init Socket properties:
                UDPSocket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);

                //allow for loopback testing
                UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                //extremly important to bind the
                //Socket before joining multicast groups
                UDPSocket.Bind(LocalHostIPEnd);

                if (multicast)
                {
                    //set multicast flags, sending flags - TimeToLive (TTL)
                    // 0 - LAN
                    // 1 - Single Router Hop
                    // 2 - Two Router Hops...

                    UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);

                    //join multicast group
                    UDPSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(Target_IP));
                }

                if (!serveronly)
                {
                    //get in waiting mode for data - always (this doesn't halt code execution)
                    Receive();
                }

                connected = true;

                MulticastSocketEventsArgs args = new MulticastSocketEventsArgs();
                args.id = 0;
                args.name = "OnConnect";
                DoEvent(args);
            }
            catch (Exception e)
            {
                UDPSocket.Shutdown(SocketShutdown.Both);
                Trace.WriteLine("BWCO.MulticastSocket::MulticastSocket exception " + e.Message);
                connected = false;

                MulticastSocketEventsArgs ex = new MulticastSocketEventsArgs();
                ex.id = -1;
                ex.name = "Exception while trying to connect";
                ex.data = new StringBuilder(e.ToString());
                DoEvent(ex);
            }

            return connected;
        }


        /// <summary>
        /// client send function
        /// </summary>
        /// <param name="sendData"></param>
        public void Send(string sendData)
        {
            byte[] bytesToSend = enctype.GetBytes(sendData);

            //set the target IP
            IPEndPoint RemoteIPEndPoint = new IPEndPoint(Target_IP, Target_Port);
            EndPoint RemoteEndPoint = (EndPoint)RemoteIPEndPoint;

            //do asynchronous send
            UDPSocket.BeginSendTo(
                    bytesToSend,
                    0,
                    bytesToSend.Length,
                    SocketFlags.None,
                    RemoteEndPoint,
                    new AsyncCallback(SendCallback),
                    UDPSocket
            );
        }

        /// <summary>
        /// executes the asynchronous send 
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSendTo(ar);
            }
            catch (Exception e)
            {
                Trace.WriteLine("BWCO.MulticastSocket::SendCallback exception " + e.ToString());

                MulticastSocketEventsArgs ex = new MulticastSocketEventsArgs();
                ex.id = -1;
                ex.name = "BWCO.MulticastSocket::SendCallback exception " + e.ToString();
                DoEvent(ex);
            }
        }


        /// <summary>
        /// initial receive function - called only once
        /// </summary>
        private void Receive()
        {
            try
            {
                IPEndPoint LocalHostIPEnd;
                if (multicast) LocalHostIPEnd = new IPEndPoint(IPAddress.Any, Target_Port);
                else LocalHostIPEnd = new IPEndPoint(Target_IP, Target_Port);

                EndPoint LocalEndPoint = (EndPoint)LocalHostIPEnd;

                // Create the state object.
                MulticastSocketStateObject state = new MulticastSocketStateObject();
                state.workSocket = UDPSocket;

                // Begin receiving the data from the remote device.
                UDPSocket.BeginReceiveFrom(
                    state.buffer,
                    0,
                    MulticastSocketStateObject.BufferSize,
                    0,
                    ref LocalEndPoint,
                    new AsyncCallback(ReceiveCallback),
                    state
                );
            }
            catch (Exception e)
            {
                Trace.WriteLine("BWCO.MulticastSocket::Recieve exception " + e.ToString());

                MulticastSocketEventsArgs ex = new MulticastSocketEventsArgs();
                ex.id = -1;
                ex.name = "BWCO.MulticastSocket::Recieve exception " + e.ToString();
                DoEvent(ex);
            }
        }

        /// <summary>
        /// executes the asynchronous receive - executed everytime data is received on the port 
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            lock (syncObject)
            {
                if (shuttingdown)
                    return;
            }

            try
            {
                IPEndPoint LocalHostIPEnd;
                if (multicast) LocalHostIPEnd = new IPEndPoint(IPAddress.Any, Target_Port);
                else LocalHostIPEnd = new IPEndPoint(Target_IP, Target_Port);

                EndPoint LocalEndPoint = (EndPoint)LocalHostIPEnd;

                // Retrieve the state object and the client socket from the async state object.
                MulticastSocketStateObject state = (MulticastSocketStateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceiveFrom(ar, ref LocalEndPoint);

                state.sb.Append(enctype.GetString(state.buffer, 0, bytesRead));

                // Fire Event
                MulticastSocketEventsArgs args = new MulticastSocketEventsArgs();
                args.id = 1;
                args.name = "OnData";
                args.data.Append(state.sb.ToString());
                DoEvent(args);

                state.sb.Remove(0, state.sb.Length);

                //clear buffer
                state.buffer = new byte[MulticastSocket.UDP_BUFFERSIZE];

                //keep listening
                client.BeginReceiveFrom(
                    state.buffer,
                    0,
                    MulticastSocketStateObject.BufferSize,
                    0,
                    ref LocalEndPoint,
                    new AsyncCallback(ReceiveCallback),
                    state
                );
            }
            catch (Exception e)
            {
                Trace.WriteLine("BWCO.MulticastSocket::ReceiveCallback exception " + e.ToString());

                MulticastSocketEventsArgs ex = new MulticastSocketEventsArgs();
                ex.id = -1;
                ex.name = "BWCO.MulticastSocket::ReceiveCallback exception " + e.ToString();
                DoEvent(ex);
            }
        }

        /// <summary>
        /// Fire an event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void DoEvent(MulticastSocketEventsArgs e)
        {
            lock (syncObject)
            {
                switch (e.id)
                {
                    case -1:
                        Trace.WriteLine("BWCO.MulticastSocket::DoEvent error " + e.name);
                        if (OnError != null)
                            OnError.Invoke(this, e);
                        break;

                    case 0:
                        if (OnConnect != null)
                            OnConnect.Invoke(this, e);
                        break;

                    case 1:
                        if (OnData != null)
                            OnData.Invoke(this, e);
                        break;
                }
            }
        }
    }
}