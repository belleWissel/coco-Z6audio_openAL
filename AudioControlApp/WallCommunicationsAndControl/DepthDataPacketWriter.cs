using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Timers;

//using System.ComponentModel;
using System.Threading;
using System.Diagnostics; // to write to event log

/**************************
 * 
 * Sends data across network using UDP and compressed packets
 * Sends data as short (in16) allowing values from -32,768 to 32,768
 * Potential optimizations include sending data ushort and using BeginSend instead of Send
*/

namespace AudioControlApp.WallCommunicationsAndControl
{
  
    class DepthDataPacketWriter
    {
        private static bool eventLogEnabled = false;
        //private string connectionIP;
        private int connectionPort;

        private static List<Socket> clientSockets;//tw: don't like static type works but not thread safe
        private static object clientSocketsLock;
        private static TcpListener tcpListener;//tw: don't like static type works but not thread safe
        private static bool isRunning = false; //used to determine if shutdown is in progress..

        IPEndPoint serverEndPoint;

        public static int currentWrites = 0;//should probably be waitevent
        private int writeFailedCount = 0;

        public DepthDataPacketWriter(int whichPort)
        {
            System.Diagnostics.Trace.WriteLine("[PACKETWRITER] ctrl");

            //connectionIP = whichIP;
            connectionPort = whichPort;

            clientSockets = new List<Socket>();
            clientSocketsLock = new Object();

            serverEndPoint = new IPEndPoint(IPAddress.Any, connectionPort);

            start();
        }

        public void start()
        {
            if (isRunning) halt();

            tcpListener = new TcpListener(IPAddress.Any, connectionPort);
            tcpListener.Start();

            //begin accepting, some danger when we get a lot of async connects at the same time
            tcpListener.BeginAcceptSocket(new AsyncCallback(AcceptClient), null);

            isRunning = true;

        }
        
        public void halt()
        {
            isRunning = false;

            System.Diagnostics.Trace.WriteLine("[PACKETWRITER] halt");
            tcpListener.Stop();

            //disconnect clients
            try
            {
                foreach (Socket s in clientSockets)
                {
                    try
                    {
                        s.Shutdown(SocketShutdown.Both);
                        s.Close(1);
                    }
                    catch (Exception e)
                    {
                        if (eventLogEnabled)
                        EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] exception haltings socket: " + e.Message, EventLogEntryType.Error);
                        System.Diagnostics.Trace.WriteLine("[PACKETWRITER] exception in halt");
                    }
                }
            }
            catch (Exception e)
            {
                if (eventLogEnabled)
                    EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] exception haltings socket: " + e.Message, EventLogEntryType.Error);
                System.Diagnostics.Trace.WriteLine("[PACKETWRITER] exception in halt");
            }
            clientSockets.Clear();
        }

        private static void AcceptClient(IAsyncResult ar)
        {
            if (!isRunning) return;

            System.Diagnostics.Trace.WriteLine("[PACKETWRITER] accept client");
            try
            {
                Socket tcpSocket = tcpListener.EndAcceptSocket(ar);
                tcpSocket.NoDelay = true;
                tcpSocket.LingerState = new LingerOption(true, 0);

                try
                {
                    if (Monitor.TryEnter(clientSocketsLock, 1000))
                    {
                        //possible problem with thread safety
                        clientSockets.Add(tcpSocket);
                    }
                }
                finally
                {
                    Monitor.Exit(clientSocketsLock);
                }

                //start listening again
                tcpListener.BeginAcceptSocket(new AsyncCallback(AcceptClient), null);
            }
            catch (ObjectDisposedException e)
            {
                if (eventLogEnabled)
                    EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] ObjectDisposedException, this is expected when socket is shutting down", EventLogEntryType.Information);
                System.Diagnostics.Trace.WriteLine("[PACKETWRITER] ObjectDisposedException, this is expected when socket is shutting down");
            }
            catch (Exception e)
            {
                if (eventLogEnabled)
                    EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] unknown exception during socket accept: " + e.Message, EventLogEntryType.Warning);
                System.Diagnostics.Trace.WriteLine("[PACKETWRITER] failed to accpet data");
            }
        }

        public int GetConnectedClientCount()
        {
            int result = 0;
            try
            {
                if (Monitor.TryEnter(clientSocketsLock, 10))
                {
                    result  =  clientSockets.Count;
                }
            }
            finally
            {
                Monitor.Exit(clientSocketsLock);
            }
            return result;
        }

        public bool IsConnected()
        {
            return GetConnectedClientCount() > 0;
        }

        protected long lastSent = DateTime.UtcNow.Ticks;
        public void SendMeshData(byte[] package)
        {
            
            //throttle sending (this is done in usersensorgrid)
            /*if (DateTime.UtcNow.Ticks - lastSent < ( TimeSpan.TicksPerMillisecond * 100))
            {
                return;
            }
            lastSent = DateTime.UtcNow.Ticks;
            */

            //only send if connnected, running and not already sending
            if (isRunning)
            {
                writeFailedCount = 0;
                
                try
                {
                    if (Monitor.TryEnter(clientSocketsLock, 15))
                    {
                        int disconnectcount = 0;
                        currentWrites++;

                        //loop through connected sockets and send data
                        foreach (Socket s in clientSockets)
                        {
                            try
                            {
                                if (s.Connected)
                                {
                                    //System.Diagnostics.Debug.WriteLine("sending package length: " + package.Length);
                                    s.BeginSend(package, 0, package.Length, SocketFlags.None, new AsyncCallback(SendCallBack), s);
                                }
                                else
                                {
                                    if (eventLogEnabled)
                                        EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] socket disconnect detected", EventLogEntryType.Information);
                                    disconnectcount++;
                                }
                            }
                            catch (IOException e)
                            {
                                if (eventLogEnabled)
                                    EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] io exception: socket disconnect detected during send: " + e.Message, EventLogEntryType.Warning);
                                System.Diagnostics.Trace.WriteLine("[PACKETWRITER] io exception");
                            }
                            catch (Exception e)
                            {
                                if (eventLogEnabled)
                                    EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] unknown exception sending data: " + e.Message, EventLogEntryType.Warning);
                                System.Diagnostics.Trace.WriteLine("[PACKETWRITER] exception on sending data");
                            }
                        }

                        //remove disconnected sokcets
                        if(disconnectcount> 0 )
                        {
                            foreach (Socket s in clientSockets)
                            {
                                if (!s.Connected)
                                {
                                    try
                                    {
                                        s.Close();
                                    }
                                    catch (Exception e)
                                    {
                                        if (eventLogEnabled)
                                            EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] exception on closing disconnected socket: " + e.Message, EventLogEntryType.Warning);
                                        System.Diagnostics.Trace.WriteLine("[PACKETWRITER] exception on closing disconnected socket");
                                    }
                                    clientSockets.Remove(s);
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    if (eventLogEnabled)
                        EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] unknown exception during socket send: " + e.Message, EventLogEntryType.Warning);
                    System.Diagnostics.Trace.WriteLine("[PACKETWRITER] failed to send data");
                }
                finally
                {
                    Monitor.Exit(clientSocketsLock);
                }
            }
        }

        public void SendCallBack(IAsyncResult ar)
        {
            //System.Diagnostics.Trace.WriteLine("[PACKETWRITER] SendCallBack: end write " + currentWrites);
            try
            {
                //System.Diagnostics.Trace.WriteLine("[PACKETWRITER] SendCallBack: end write " + currentWrites + " " + ar.IsCompleted);
                if (ar.IsCompleted)
                {
                    //System.Diagnostics.Trace.WriteLine("[PACKETWRITER] SendCallBack: end write " + currentWrites);

                    //not threadsafe, it's possible beginsend is being executed at the same time as we are handling endsend
                    Socket s = (Socket)ar.AsyncState;
                    s.EndSend(ar);
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("[PACKETWRITER] SendCallBack: CompletedSynchronously is false");
                    if (eventLogEnabled)
                        EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] async write did not complete", EventLogEntryType.Warning);
                }
            }
            catch (Exception e)
            {
                if (eventLogEnabled)
                    EventLog.WriteEntry(MainApp.MyWindow.EVENTLOG_SOURCENAME, "[PACKETWRITER] exception in sendcallback: " + e.Message, EventLogEntryType.Warning);
                System.Diagnostics.Trace.WriteLine("[PACKETWRITER] callback failure: " + e.Message);
            }
            currentWrites--;
        }
    }
}
