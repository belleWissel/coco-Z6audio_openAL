using System;
using System.Collections.Generic;
using System.Collections;

using System.Text;

namespace AudioControlApp.WallCommunicationsAndControl
{

    public delegate void ClientCommandEventHandler(object sender, ClientCommandEventsArgs e);

    /// <summary>
    /// Arguments that go with the ClientCommandEventHandler
    /// </summary>
    public class ClientCommandEventsArgs : EventArgs
    {
        public string source = "";
        public string method = "";
        public string args = "";

    }

    class InterComputerClient
    {
        // subscribe to this event handler
        public event ClientCommandEventHandler OnCommand;//Data event

        private MulticastSocket socket;



        public InterComputerClient(string ip, int port)
        {
            socket = new MulticastSocket();
            socket.OnError += new MulticastSocketEventHandler(socket_OnError);
            socket.OnConnect += new MulticastSocketEventHandler(socket_OnConnect);
            socket.OnData += new MulticastSocketEventHandler(socket_OnData);

            socket.Connect(ip, port, false);
        }


        private void socket_OnData(object sender, MulticastSocketEventsArgs e)
        {
            string[] split = e.data.ToString().Split(new Char[] { ',' });

            ClientCommandEventsArgs args = new ClientCommandEventsArgs();
            try
            {
                args.source = split[0];
                args.method = split[1];
                args.args = split[2];
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("[INTRCOMPCLIENT] invalid argument received: " + e.data.ToString());
            }

            if (this.OnCommand != null)
            {
                OnCommand.BeginInvoke(this, args, null, null);
            }

        }

        private void socket_OnConnect(object sender, MulticastSocketEventsArgs e)
        {
            if (MainApp.MyWindow.debugMode)
            {
                //MainClass.textBoxLogVar.logText("[interComputer] Socket Connected.");
            }
            sendConnectedStatus();
            System.Diagnostics.Debug.WriteLine("INTERCOMPCLIENT socket connected");
        }

        private void socket_OnError(object sender, MulticastSocketEventsArgs e)
        {
            if (MainApp.MyWindow.debugMode)
            {
                //MainClass.textBoxLogVar.logText("[interComputer] Socket Connect Error.");
            }
            throw new Exception("Inter Computer socket error");
        }

        public void sendConnectedStatus()
        {
            //socket.Send(MainClass.kioskName + ",connected," + MainClass.kioskIPaddress);
            socket.Send(MainApp.MyWindow.kioskName + ",connected," + MainApp.MyWindow.kioskIPaddress);
        }

        public void requestConnections()
        {
            socket.Send(MainApp.MyWindow.kioskName + ",confirmConnection," + MainApp.MyWindow.kioskIPaddress);
        }
        public void sendUpdatedVideoData(int whichVideo, bool whichStatus, bool isTall)
        {
            if (whichStatus)
            {
                if (isTall)
                {
                    socket.Send(MainApp.MyWindow.kioskName + ",videoRequestedForStartTall," + whichVideo);
                }
                else
                {
                    socket.Send(MainApp.MyWindow.kioskName + ",videoRequestedForStartShort," + whichVideo);

                }
            }
            else
            {
                socket.Send(MainApp.MyWindow.kioskName + ",videoRequestedForHalt," + whichVideo);

            }
        }

        public void sendUpdatedAdData(int whichVideo, bool whichStatus)
        {
            if (whichStatus)
            {
                socket.Send(MainApp.MyWindow.kioskName + ",adRequestedForStart," + whichVideo);

            }
            else
            {
                socket.Send(MainApp.MyWindow.kioskName + ",adRequestedForHalt," + whichVideo);
            }

        }

        public void sendDisconnectedStatus()
        {
            socket.Send(MainApp.MyWindow.kioskName + ",disconnected,true");
        }

        public void sendRunStatus(bool isRunning)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",runStatus," + isRunning);
        }
        /*
        // video starting...
        public void sendVideoInStatus(string whichSide)
        {
            //socket.Send(MainClass.kioskName + ",runStatus," + isRunning);
        }

        // video stopping...
        public void sendVideoOutStatus(string whichSide)
        {
            //socket.Send(MainClass.kioskName + ",runStatus," + isRunning);
        }
        
        public void sendTouchEvent(int whichRegion, int whichButton)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",touchButtonRequest," + whichButton);

        }
        public void sendReleaseEvent(int whichRegion, int whichButton)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",releaseButtonRequest," + whichButton);

        }
        
        public void sendDepthData(string whichData)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",depthData," + whichData);
        }
        */

        public void sendStartAmbientMode()
        {
            socket.Send(MainApp.MyWindow.kioskName +",launchAmbientLoops,true");
        }
        public void sendStopAmbientMode()
        {
            socket.Send(MainApp.MyWindow.kioskName +",haltAmbientLoops,true");
        }

        
        public void sendActivationStatus(string whichNewStatus)
        {
            socket.Send(MainApp.MyWindow.kioskName +",activationStatusUpdate,"+whichNewStatus);
        }
        
        
        public void sendActivateNearEvent(int whichRegion)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",activateRegionNear," + whichRegion);

        }
        public void sendActivateMidEvent(int whichRegion)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",activateRegionMid," + whichRegion);

        }
        public void sendActivateFarEvent(int whichRegion)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",activateRegionFar," + whichRegion);

        }
        public void sendDeactivateNearEvent(int whichRegion)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",deactivateRegionNear," + whichRegion);

        }
        public void sendDeactivateMidEvent(int whichRegion)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",deactivateRegionMid," + whichRegion);

        }
        public void sendDeactivateFarEvent(int whichRegion)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",deactivateRegionFar," + whichRegion);

        }
        public void sendAudioSampleRequest(string whichAudioSampleCode)
        {
            socket.Send(MainApp.MyWindow.kioskName + ",playAudio," + whichAudioSampleCode);
        }

    }
}
