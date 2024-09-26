using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using SocketIOClient;
using SocketIOClient.Messages;

using System.Collections.Generic;
using System.IO;

/*
using Microsoft.CSharp;
using System.Collections;
using System.Reflection;
*/

namespace SensorControlApp.CommunicateToTableApp
{
    class CommunicateToTableControlViaOSC
    {
        //private static SocketIOClient.Client tableClient;
        private static bool isConnected = false;

        string socketIPAddress = "127.0.0.1";
        int socketPort = 3000;
        private bool localDebugMode = true;

        private System.Timers.Timer checkConnectionTimer = new System.Timers.Timer();

        private static int maxNumberOfAreas = 6;
        private AreaActivationStateControl[] activationState = new AreaActivationStateControl[maxNumberOfAreas];

        public string currentActivationFeedback = "ACTIVATION: left to right: [0] [0] [0] [0] [0] [0] ";

        private static TransmitOSCMessage transmitOSCMessageVar;

        public CommunicateToTableControlViaOSC(string whichIP, int whichPort, bool isDebugMode)
        {
            socketIPAddress = whichIP;
            socketPort = whichPort;
            localDebugMode = isDebugMode;


            checkConnectionTimer.Elapsed += new System.Timers.ElapsedEventHandler(checkConnectionTimerExpired);
            checkConnectionTimer.Interval = 5000;
            checkConnectionTimer.AutoReset = false;
            

            for (int i = 0; i < maxNumberOfAreas; ++i)
            {
                activationState[i] = new AreaActivationStateControl();
            }
        }

        public void updateActivation(int whichArea, int whichLevel, bool doActivate) // level 0 = closest
        {
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] updating Activation: area:["+whichArea+"] level: ["+whichLevel+"] isactive: ["+doActivate+"]");

            if (whichArea > maxNumberOfAreas)
                return;

            if (doActivate)
                activationState[whichArea].activeState(whichLevel);
            else
                activationState[whichArea].deactiveState(whichLevel);

            if (activationState[whichArea].isDirty()) // something relevant changed as a result of the change in activation
            {
                transmitAreaActivationOverSocket(whichArea, activationState[whichArea].currentClosestActivation);
                updateDebugFeedback();
            }
        }

        public void initSocket()
        {

            if (transmitOSCMessageVar == null) // only create this once
            {
                System.Diagnostics.Debug.WriteLine("[TABLECOMM] creating tableClient");

                transmitOSCMessageVar = new TransmitOSCMessage(socketIPAddress, socketPort, localDebugMode);
            }
            /*
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] initSocket");
            if (tableClient == null) // only create this once
            {
                System.Diagnostics.Debug.WriteLine("[TABLECOMM] creating tableClient");
                tableClient = new Client("http://" + socketIPAddress + ":" + socketPort + "/"); // url to the nodejs / socket.io instance

                // configuration of client:
                tableClient.Opened += SocketOpened;
                tableClient.Message += SocketMessage;
                tableClient.SocketConnectionClosed += SocketConnectionClosed;
                tableClient.Error += SocketError;
                tableClient.On("connect", connect);
                tableClient.RetryConnectionAttempts = 0;
                
            }
            */

            bool doResetActivation = false;
            if (doResetActivation)
            {
                int i, j;
                for (i = 0; i < maxNumberOfAreas; ++i)
                {
                    for (j = 0; j < 3; ++j)
                        updateActivation(i, j, false);
                }
            }
            updateDebugFeedback();
            checkConnectionTimer.Start();
            //doReconnect();
        }


        private void updateDebugFeedback()
        {
            currentActivationFeedback = "ACTIVATION: left to right: [" + activationState[0].currentClosestActivation + "] [" + activationState[1].currentClosestActivation + "] [" + activationState[2].currentClosestActivation + "] [" + activationState[3].currentClosestActivation + "] [" + activationState[4].currentClosestActivation + "] [" + activationState[5].currentClosestActivation + "] \n";
            currentActivationFeedback += "Connected to Table Control = " + isConnected + "\n";
        }

        private static void doReconnect()
        {
            try
            {
                if (transmitOSCMessageVar != null)
                {
                    isConnected = transmitOSCMessageVar.getConnectionStatus();
                    if (!isConnected)
                        transmitOSCMessageVar.reconnect();
                }
                /*if (tableClient != null)
                {

                    isConnected = tableClient.IsConnected;

                    if (!isConnected)
                        tableClient.Connect();

                }*/
            }
            catch (Exception ee)
            {
                System.Diagnostics.Debug.WriteLine("[TABLECOMM] crashed while reconnecting " + ee.Message);
            }
        }

        private void checkConnectionTimerExpired(object sender, EventArgs e) 
        {
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] checkConnectionTimerExpired");

            checkConnectionTimer.Stop();
            doReconnect();
            
        }

        private void transmitAreaActivationOverSocket(int whichArea, int whichLevel)
        {
            int whichSide = 0;
            //if (!IamBottomSide)
            //    whichSide = 1;

            int displayIDToTransmit = -1;

            //if (!IamBottomSide)
            //{
                switch (whichArea)
                {
                    case 0:
                        displayIDToTransmit = 1;
                        break;
                    case 1:
                        displayIDToTransmit = 2;
                        break;
                    case 2:
                        displayIDToTransmit = 3;
                        break;
                    /*case 3:
                        displayIDToTransmit = 4;
                        break;
                    case 4:
                        displayIDToTransmit = 5;
                        break;
                    case 5:
                        displayIDToTransmit = 6;
                        break;*/

                }
            /*}
            else
            {
                switch (whichArea)
                {
                    case 0:
                        displayIDToTransmit = 6;
                        break;
                    case 1:
                        displayIDToTransmit = 5;
                        break;
                    case 2:
                        displayIDToTransmit = 4;
                        break;
                    case 3:
                        displayIDToTransmit = 3;
                        break;
                    case 4:
                        displayIDToTransmit = 2;
                        break;
                    case 5:
                        displayIDToTransmit = 1;
                        break;

                }
            }*/

            if (displayIDToTransmit != -1)
                doTransmit(displayIDToTransmit, whichLevel, whichSide);
        }

        private void doTransmit(int displayID, int level, int side)
        {
            if (transmitOSCMessageVar == null)
                return;

            if (transmitOSCMessageVar.getConnectionStatus())
            {
                transmitOSCMessageVar.transmitMessage(displayID, level);
            }

            /*
            if (tableClient == null)
                return;

            if(tableClient.IsConnected)
            {
                dynamic payload = new Dictionary<string, object>();
                payload["targetClientId"] = "display" + displayID; // Where displayIdentifier is 1,2,3,4,5 or 6. 
                payload["type"] = "proximity"; // always this message coming from the sensorControlApp
                payload["level"] = level; // 0, 1, or 2
                payload["side"] = side; // 0 for the "top" or 1 for the "bottom"

                try
                {
                    tableClient.Emit("forward", payload);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("[TABLECOMM] transmission of activation failed");
                }
                
            }*/
        }

        /*
        private void SocketConnectionClosed(object sender, EventArgs e)
        {
            //updateDebugFeedback(); // report to screen new status of connection

            //isConnected = false;
            //updateDebugFeedback();
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] WebSocketConnection was terminated!");
            this.SocketError(sender, new SocketIOClient.ErrorEventArgs("connection Closed", new Exception()));

        }
        
        private void SocketMessage(object sender, MessageEventArgs e)
        {
            // uncomment to show any non-registered messages
            try
            {
                if (string.IsNullOrEmpty(e.Message.Event))
                    System.Diagnostics.Debug.WriteLine("Generic SocketMessage: {0}", e.Message.MessageText);
                else
                    System.Diagnostics.Debug.WriteLine("Generic SocketMessage: {0} : {1}", e.Message.Event, e.Message.JsonEncodedMessage.ToJsonString());
            }
            catch (Exception ee)
            {
                System.Diagnostics.Debug.WriteLine("[TABLECOMM] SocketMessage error");
            }
        }

        private void SocketOpened(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] SocketOpened");
            isConnected = true;
            updateDebugFeedback();
        }

        private void SocketError(object sender, SocketIOClient.ErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] socket tableClient error:");
            System.Diagnostics.Debug.WriteLine("[TABLECOMM] "+e.Message);
            isConnected = false;
            updateDebugFeedback();

            tableClient.Dispose();
            tableClient = null;

            initSocket();
            //checkConnectionTimer.Start(); // restart timer which will reconnect socket
            //tableClient.Close();

        }
        */

        public void onClosing()
        {
            checkConnectionTimer.Stop();
            if (transmitOSCMessageVar != null)
            {
                transmitOSCMessageVar.onClosing();
            }
                

            /*
            if (tableClient != null)
            {
                tableClient.Opened -= SocketOpened;
                tableClient.Message -= SocketMessage;
                tableClient.SocketConnectionClosed -= SocketConnectionClosed;
                tableClient.Error -= SocketError;
                tableClient.Dispose(); // close & dispose of socket client
            }*/
        }


    }
}
