using System;
using System.Collections;
using System.Collections.Generic;
using OSC.NET;
using System.Runtime.InteropServices;


namespace SensorControlApp.CommunicateToTableApp
{
    class TransmitOSCMessage
    {
        OSCTransmitter oscTransmitterVar;
        string OSCIp; //ip tipically 127.0.0.1
        int OSCPort;//3333 
        //int TUIOfseq = 0;//for secuence index 
        [DllImport("kernel32.dll")]
        private static extern bool Beep(int freq, int dur);
        bool debugMode = false;
        bool beepOverride = true;

        public TransmitOSCMessage(string whichIP, int whichPort, bool whichDebug)
        {
            Console.WriteLine("[TUIOXMIT] ctrl");
            debugMode = whichDebug;

            OSCIp = whichIP;
            OSCPort = whichPort;

            oscTransmitterVar = new OSCTransmitter(OSCIp, OSCPort);
            oscTransmitterVar.Connect();

            if (debugMode)
                Beep(500, 500);

        }

        public void reconnect()
        {
            oscTransmitterVar.Connect();
        }

        public bool getConnectionStatus()
        {
            return oscTransmitterVar.isConnected();
        }

        public void transmitMessage(int displayID, int level)
        {
            if (oscTransmitterVar == null)
                return;

            if (!oscTransmitterVar.isConnected())
                return;

            OSCBundle OscB = new OSCBundle();


            string tag="touchUpdate";
            OSCMessage s = new OSCMessage(tag);

            s.Append(displayID);
            s.Append(level);

            OscB.Append(s);

            oscTransmitterVar.Send(OscB);

            int whichMultiplier = (level + 1) * 2;

            int whichFreq = 100 * whichMultiplier * displayID;
            
            int whichDur = 50 * (level + 1);
            if (!beepOverride)
            {
                if (debugMode)

                    Beep(whichFreq, whichDur);
            }
            //Beep(300, 50);
        }

        public void onClosing()
        {
            oscTransmitterVar.Close();
            
        }

    }
}
