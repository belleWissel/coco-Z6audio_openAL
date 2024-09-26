using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SecondstoryCommon;
using System.Collections;

namespace SensorControlApp.WallCommunicationsAndControl
{
    class VideoSensorGrid
    {
        // for passsing events to MainClass
        public event AppEventHandler OnEvent;
        private string myEventSource = "videoSensorGridControl";

        private static int numberOfVideoAreas = 7;


        VideoResetTimer[] videoResetTimers = new VideoResetTimer[numberOfVideoAreas];

        VideoResetTimer[] reactivateVideoAreaTimer = new VideoResetTimer[numberOfVideoAreas];

        public bool[] videoAreaActivated = new bool[numberOfVideoAreas];
        public bool[] adAreaActivated = new bool[numberOfVideoAreas];
        public bool[] prevVideoAreaActivated = new bool[numberOfVideoAreas]; // keep track of previous state of wall (check for changes)
        public bool[] prevAdAreaActivated = new bool[numberOfVideoAreas]; // keep track of previous state of wall (check for changes)

        // during the last check: what was activated:
        private bool[] immediateVideoAreaActivated = new bool[numberOfVideoAreas];
        private bool[] immediateVideoAreaIsLive = new bool[numberOfVideoAreas];
        private bool[] immediateAdAreaActivated = new bool[numberOfVideoAreas];
        private bool[] immediateAdAreaIsLive = new bool[numberOfVideoAreas];
        private bool[] keepVideoPlayingActivated = new bool[numberOfVideoAreas];
        private bool[] keepVideoPlayingIsLive = new bool[numberOfVideoAreas];


        private int[] spawnVideoCounter = new int[numberOfVideoAreas]; // keep track of how long it has been positive
        //private int[] spawnVideoCounterTall = new int[7]; // keep track of how long it has been positive
        private int[] spawnAdCounter = new int[numberOfVideoAreas]; // keep track of how long it has been positive

        private int[] haltVideoCounter = new int[numberOfVideoAreas];
        private int[] haltAdCounter = new int[numberOfVideoAreas];
        //private int[] waitBeforeSpawnAnotherVideoCounter = new int[numberOfVideoAreas]; // don't spawn two of the same events too quickly
        //private int[] waitBeforeSpawnAnotherAdCounter = new int[numberOfVideoAreas]; // don't spawn two of the same events too quickly
        //private bool[] waitBeforeSpawnActive = new bool[numberOfVideoAreas];
        //private int waitBeforeSpawnLimit = 100;

        // prevent rapid fire of events:
        private int[] waitBeforeSendAnotherEventCounter = new int[numberOfVideoAreas];
        private bool[] sendEventDisabled = new bool[numberOfVideoAreas];
        private int waitBeforeSendEventLimit = 10;

        // slower trigger (someone has to wait for some time in front of ad):
        private int triggersBeforeSpawnVideoEvent = 120; // spawn video event has to be timed with the "load indicator" on the background layer

        // fast trigger (reactive part)
        private int triggersBeforeSpawnAdEvent = 5; // spawn event only after it has been the same for n times

        // kills both video and ad (whichever is active):
        private int triggersBeforeHaltEvent = 30; // spawn event only after it has been the same for n times
        private int triggersBeforeAdHaltEvent = 30; // specific to Ad
        private int triggersBeforeVideoHaltEvent = 30; // specific to Video

        // shortcut to video (touched screen) - another fast one...
        private int[] spawnVideoShortcutCounter = new int[numberOfVideoAreas];
        private int triggersBeforeVideoShortcutEvent = 15;

        private int triggersBeforeShowAnotherAddEvent = 55;



        private int[,] gridXRanges = new int[numberOfVideoAreas, 2]; // 7 video areas, high low grid range (left and right)
        private int[,] gridYRanges = new int[numberOfVideoAreas, 2]; // 7 video areas, bottom and top grid range (bottom and top)


        public bool[] controlStatusVideoIsRunning = new bool[numberOfVideoAreas];

        private int gridCountWidth = 200;
        private int gridCountHeight = 200;
        //private int gridCountHeightTall = 200;

        private float userSensorAreaW = 0.06f; // percent width of entire wall
        private float userSensorAreaBot = 0.06f; // percent height of entire wall
        private float userSensorAreaTop = 0.60f; // percent height of entire wall
        
        // near and far toggle ranges:
        public float toggleRangeFar = 1000.0f; // range at which user data triggers 
        public float toggleRangeNear = 50.0f; // range at which user data triggers 
        public float toggleRangeMid = 1500.0f; // where video is determined to quit
        

        // these values are used only to draw user area on screen (copied from
        private float gridWidth = 7560.0f; // (1080 * 7)
        private float gridHeight = 3840.0f; // (1920 * 2)
        private float gridStartX = -3780.0f;
        private float gridStartY = -1920.0f;
        private float gridResolutionW = 250.0f;
        private float gridResolutionH = 100.0f;

        

        // **************************************
        // GET/SET has state changed?
        // **************************************
        public bool dirty = false;
        public bool isDirty()
        {
            bool valueToReturn = dirty;
            if (dirty)
            {
                dirty = false;
            }
            return valueToReturn;
        }

        public VideoSensorGrid(int whichHCount, int whichVCount)
        {

            gridCountWidth = whichHCount;
            gridCountHeight = whichVCount;
            //gridCountHeightShort = (int)Math.Round((double)whichVCount / 2.0);
            //gridCountHeightTall = whichVCount;
            initializeCountersAndAreaStatus();

            gridResolutionW = gridWidth / (float)whichHCount;
            gridResolutionH = gridHeight / (float)whichVCount;

            assignGridRanges();

            string whichTimerID = "";

            // reset/init all variables:
            for (int i = 0; i < numberOfVideoAreas; ++i)
            {
                controlStatusVideoIsRunning[i] = false;
               
                // prevents rapid transmission of events:
                sendEventDisabled[i] = true;

                int disableCountersForAMoment = -120; // forces counters to start well below limits (wait for system to come up, initialize)

                waitBeforeSendAnotherEventCounter[i] = disableCountersForAMoment;
                spawnVideoCounter[i] = disableCountersForAMoment;
                spawnAdCounter[i] = disableCountersForAMoment;
                spawnVideoShortcutCounter[i] = disableCountersForAMoment;
                haltAdCounter[i] = disableCountersForAMoment;
                haltVideoCounter[i] = disableCountersForAMoment;

                whichTimerID = "resetTimer" + i;
                videoResetTimers[i] = new VideoResetTimer(whichTimerID, 65, false); // waits a full 65 seconds then forces video as disabled... (video end command did not get through)
                videoResetTimers[i].OnEvent += new AppEventHandler(VideoReset_OnEvent);

                reactivateVideoAreaTimer[i] = new VideoResetTimer(whichTimerID, 1.5, false); // waits a full 65 seconds then forces video as disabled... (video end command did not get through)
                reactivateVideoAreaTimer[i].OnEvent += new AppEventHandler(ReactivateVideoAreaTimer_OnEvent);
            }
        }

        public void setUserTiming(double whichStartAdTime, double whichRemoveAdTime, double whichAdToVideoTime, double whichTouchToVideoTime, double whichRemoveVideoTime, double whichWaitForSecondAdTime, int whichTransmissionRate)
        {
            double transmissionRate = (double)whichTransmissionRate; // what is the length of one tick in ms?
            double numberOfTriggersInASecond = 1000.0/transmissionRate; // this is our rough frame rate

            triggersBeforeSpawnAdEvent = (int)Math.Round(whichStartAdTime * numberOfTriggersInASecond);
            triggersBeforeAdHaltEvent = (int)Math.Round(whichRemoveAdTime * numberOfTriggersInASecond);
            triggersBeforeSpawnVideoEvent = (int)Math.Round(whichAdToVideoTime * numberOfTriggersInASecond);
            triggersBeforeVideoShortcutEvent = (int)Math.Round(whichTouchToVideoTime * numberOfTriggersInASecond);
            triggersBeforeVideoHaltEvent = (int)Math.Round(whichRemoveVideoTime * numberOfTriggersInASecond);
            triggersBeforeShowAnotherAddEvent = (int)Math.Round(whichWaitForSecondAdTime * numberOfTriggersInASecond);
        }

        public void updateVideoStatusFromControl(int whichVideoArea, bool didStart)
        {
            int disableCountersForAMoment = 0; // forces counters to start well below limits (wait for system to come up, initialize)
            int disableAdForAMoment = 0;
            controlStatusVideoIsRunning[whichVideoArea] = didStart;
            if (didStart) // video start command sent from control
            {
                if (!videoResetTimers[whichVideoArea].isRunning) // is it already running?
                    videoResetTimers[whichVideoArea].startTimer();
                else
                    videoResetTimers[whichVideoArea].restart();
                videoAreaActivated[whichVideoArea] = true; // redundant but okay (set before we send start signal to control)
            }
            else // video stop command sent from control
            {
                videoResetTimers[whichVideoArea].stopTimer(); // no need to auto-reset since video stop was received
                //videoAreaActivated[whichVideoArea] = false; // allow for another ad or video to start again
                disableCountersForAMoment = 0; // forces counters to start well below limits (wait for system to come up, initialize)
                disableAdForAMoment = 0;
                reactivateVideoAreaTimer[whichVideoArea].startTimer();
            }


            // reset the counters for this video area:
            spawnVideoCounter[whichVideoArea] = disableCountersForAMoment;
            spawnAdCounter[whichVideoArea] = disableAdForAMoment;
            haltAdCounter[whichVideoArea] = disableCountersForAMoment;
            haltVideoCounter[whichVideoArea] = disableCountersForAMoment;
            spawnVideoShortcutCounter[whichVideoArea] = disableCountersForAMoment;

        }

        private void reactivateVideoArea(int whichVideoArea)
        {
            videoAreaActivated[whichVideoArea] = false; // allow for another ad or video to start again

        }
        private void resetVideoArea(int whichVideoArea) // timer expired send reset
        {
            if (false) // for purposes of testing timeout timer in lab
            {
                spawnAdCounter[whichVideoArea] = 0;
                spawnVideoCounter[whichVideoArea] = 0;
                haltAdCounter[whichVideoArea] = 0;
                haltVideoCounter[whichVideoArea] = 0;

                spawnVideoShortcutCounter[whichVideoArea] = 0;
                videoAreaActivated[whichVideoArea] = false; // allow for another ad or video to start again
                videoResetTimers[whichVideoArea].stopTimer();
            }
            else
            {
                if (controlStatusVideoIsRunning[whichVideoArea] == true) // video control status never quit
                {
                    bool didSendHaltVidEvent = checkForSendDeactivateVideoCommand(whichVideoArea); // added this to prevent stuck video areas on wall
                    /*if (didSendHaltVidEvent)
                    {
                        adAreaActivated[n] = false;
                        videoAreaActivated[n] = false;
                        //videoAreaActivated[n] = false;
                        haltAdCounter[n] = 0; // reset if event was sent
                        haltVideoCounter[n] = 0;
                    }*/

                    spawnAdCounter[whichVideoArea] = 0;
                    spawnVideoCounter[whichVideoArea] = 0;
                    haltAdCounter[whichVideoArea] = 0;
                    haltVideoCounter[whichVideoArea] = 0;

                    spawnVideoShortcutCounter[whichVideoArea] = 0;
                    videoAreaActivated[whichVideoArea] = false; // allow for another ad or video to start again
                    videoResetTimers[whichVideoArea].stopTimer();
                }
            }
        }
        
        void VideoReset_OnEvent(object sender, AppEvent e)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] VideoReset_OnEvent for :" + e.EventSource);
            if (e.EventString == "videoTimerExpired")
            {
                switch (e.EventSource)
                {
                    case "resetTimer0":
                        resetVideoArea(0);
                        break;
                    case "resetTimer1":
                        resetVideoArea(1);
                        break;
                    case "resetTimer2":
                        resetVideoArea(2);
                        break;
                    case "resetTimer3":
                        resetVideoArea(3);
                        break;
                    case "resetTimer4":
                        resetVideoArea(4);
                        break;
                    case "resetTimer5":
                        resetVideoArea(5);
                        break;
                    case "resetTimer6":
                        resetVideoArea(6);
                        break;
                }
            }
        }
        void ReactivateVideoAreaTimer_OnEvent(object sender, AppEvent e)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] VideoReset_OnEvent for :" + e.EventSource);
            if (e.EventString == "videoTimerExpired")
            {
                switch (e.EventSource)
                {
                    case "resetTimer0":
                        reactivateVideoArea(0);
                        break;
                    case "resetTimer1":
                        reactivateVideoArea(1);
                        break;
                    case "resetTimer2":
                        reactivateVideoArea(2);
                        break;
                    case "resetTimer3":
                        reactivateVideoArea(3);
                        break;
                    case "resetTimer4":
                        reactivateVideoArea(4);
                        break;
                    case "resetTimer5":
                        reactivateVideoArea(5);
                        break;
                    case "resetTimer6":
                        reactivateVideoArea(6);
                        break;
                }
            }
        }

        public void setDepthRanges(float whichMin, float whichMax)
        {

            //convert the depth toggle range to bytes for testing
            //float percentDepth = (toggleRange - whichMin) / (whichMax - whichMin);
            //toggleRangeConverted = (byte)Math.Round((double)percentDepth * 254);
        }

        public void setSensorAreaSize(double whichWidth, double whichBottom, double whichTop)
        {
            userSensorAreaW = (float)whichWidth;

            userSensorAreaBot = (float)whichBottom;
            userSensorAreaTop = (float)whichTop;

            assignGridRanges();
        }

        private void assignGridRanges()
        {
            int i;

            double areaStartX, areaEndX;
            int gridStartX, gridEndX;

            double areaStartY, areaEndY;
            int gridStartY, gridEndY;

            double numberOfDisplays = 7.0;
            double pctWidthOfDisplay = 1.0/numberOfDisplays;
            double halfOfDisplays = numberOfDisplays / 2.0;
            double centerOfActiveAreaPctTotal = 0.0;

            for (i = 0; i < numberOfVideoAreas; ++i)
            {

                //centerOfActiveAreaPctTotal = ((double)i * pctWidthOfDisplay) - (halfOfDisplays * pctWidthOfDisplay) - (0.5 * pctWidthOfDisplay);
                centerOfActiveAreaPctTotal = (((double)i + 1.0) * pctWidthOfDisplay) - (0.5 * pctWidthOfDisplay);

                // percentage of display area: 
                areaStartX = centerOfActiveAreaPctTotal - (double)userSensorAreaW / 2.0;

                //areaStartX = ((double)i - 3.0) *0.1333

                areaEndX = areaStartX + (double)userSensorAreaW;

                gridStartX = (int)Math.Floor((double)gridCountWidth * areaStartX);
                gridEndX = (int)Math.Ceiling((double)gridCountWidth * areaEndX);

                gridStartY = (int)Math.Floor((double)gridCountHeight * userSensorAreaBot);
                gridEndY = (int)Math.Ceiling((double)gridCountHeight * userSensorAreaTop);

                gridXRanges[i, 0] = gridStartX;
                gridXRanges[i, 1] = gridEndX;

                gridYRanges[i, 0] = gridStartY;
                gridYRanges[i, 1] = gridEndY;


                System.Diagnostics.Debug.WriteLine("[VIDSENSORGRID] #"+i+" start/end = ["+areaStartX+", "+areaEndX+"]");
            }


        }

        private void initializeCountersAndAreaStatus()
        {
            for (int i = 0; i < numberOfVideoAreas; ++i)
            {
                videoAreaActivated[i] = false;
                adAreaActivated[i] = false;

                prevVideoAreaActivated[i] = false;
                prevAdAreaActivated[i] = false;
                immediateVideoAreaActivated[i] = false;
                immediateAdAreaActivated[i] = false;

                immediateVideoAreaIsLive[i] = false;
                immediateAdAreaIsLive[i] = false;
                keepVideoPlayingIsLive[i] = false;

                spawnVideoCounter[i] = 0;
                spawnAdCounter[i] = 0;
                spawnVideoShortcutCounter[i] = 0;
                haltAdCounter[i] = 0;
                haltVideoCounter[i] = 0;
            }
        }

        public void testForUserActivation(float[,] depthData)
        {
           
            int n, i, j;
            int gridStartX, gridEndX;
            int gridStartY, gridEndY;

            /************************************************************************/
            /* first check current status of screen: */
            /************************************************************************/

            for (n = 0; n < numberOfVideoAreas; ++n)
            {
                immediateVideoAreaActivated[n] = false; // false until proven true
                immediateAdAreaActivated[n] = false; // false until proven true
                keepVideoPlayingActivated[n] = false;
                gridStartX = gridXRanges[n, 0];
                gridEndX = gridXRanges[n, 1];
                gridStartY = gridYRanges[n, 0];
                gridEndY = gridYRanges[n, 1];

                for (i = gridStartX; i < gridEndX; ++i) // only check relevant grid areas (no need to check whole wall)
                {
                    for (j = gridStartY; j < gridEndY; ++j)
                    {
                        if ((depthData[i, j] > 0.0f) && (depthData[i, j] < toggleRangeFar))
                        {

                            if (immediateAdAreaIsLive[n])
                                immediateAdAreaActivated[n] = true;
                            immediateAdAreaIsLive[n] = true; // catch it next time...

                            if (depthData[i, j] < toggleRangeNear) // assumes that near range is less than far range!
                            {
                                if (immediateVideoAreaIsLive[n])
                                    immediateVideoAreaActivated[n] = true; // note that both can be active....
                                immediateVideoAreaIsLive[n] = true; // catch it next time...
                            }
                            else
                                immediateVideoAreaIsLive[n] = false; // needs to be "live" for more than one frame to activate

                            if (depthData[i, j] < toggleRangeMid)
                            {
                                if (keepVideoPlayingIsLive[n])
                                    keepVideoPlayingActivated[n] = true;
                                keepVideoPlayingIsLive[n] = true; // catch it next time...
                            }
                            else
                                keepVideoPlayingIsLive[n] = false; // needs to be "live" for more than one frame to activate


                        }
                        else
                            immediateAdAreaIsLive[n] = false; // needs to be "live" for more than one frame to activate
                    }
                }
            }

            // compare and test 

            // copy previous data to prev array:
            Array.Copy(videoAreaActivated, prevVideoAreaActivated, videoAreaActivated.Length);
            Array.Copy(adAreaActivated, prevAdAreaActivated, adAreaActivated.Length);


            for (n = 0; n < numberOfVideoAreas; ++n)
            {
                /************************************************************************/
                /* current status and counters */
                /************************************************************************/

                if (immediateAdAreaActivated[n]) // this one is hot:
                {
                    //if (adAreaActivated[n])
                    //{
                    //    spawnVideoCounter[n] += 1;
                    //}
                    //else
                    //{
                    if (!controlStatusVideoIsRunning[n]) // freeze counter when video area is active
                    {
                        spawnAdCounter[n] += 1;
                        if (spawnVideoShortcutCounter[n] < 0) // this gives us some extra time to show ad, but keeps it reactive when someone does reach out.
                            spawnVideoShortcutCounter[n] += 1;
                        //}
                        haltAdCounter[n] = 0;
                    }
                    //haltVideoCounter[n] = 0;

                }
                else // no one is there right now...
                {
                    // reset counters:
                    spawnAdCounter[n] = 0;
                    //spawnVideoCounter[n] = 0;
                    spawnVideoShortcutCounter[n] = 0;
                    haltAdCounter[n] += 1;
                    //haltVideoCounter[n] += 1;
                }

                if (keepVideoPlayingActivated[n])
                {
                    haltVideoCounter[n] = 0;
                }
                else
                {
                    haltVideoCounter[n] += 1;
                }

                if (immediateVideoAreaActivated[n]) // someone touched the screen!
                {
                    if (adAreaActivated[n]) // make sure ad has already been activated
                    {
                        spawnVideoShortcutCounter[n] += 1;
                    }
                    else
                    {
                        spawnVideoShortcutCounter[n] = 0;
                    }
                }

                if (adAreaActivated[n]) // if ad is active, starting counting down to video start event
                {
                    spawnVideoCounter[n] += 1;
                }
                else
                {
                    spawnVideoCounter[n] = 0;
                }

                /************************************************************************/
                /* activation commands */
                /************************************************************************/

                // somebody touched the screen!
                if (spawnVideoShortcutCounter[n] > triggersBeforeVideoShortcutEvent)
                {
                    if (!videoAreaActivated[n]) // ignore if video has already been sent
                    {
                        if (adAreaActivated[n]) // add area must be triggered before video event can start
                        {
                            bool didSendStartVideoShortcutEvent = checkForSendActivateVideoCommand(n);
                            if (didSendStartVideoShortcutEvent)
                            {
                                adAreaActivated[n] = false; // deactivate ad
                                videoAreaActivated[n] = true; // deactivate video
                                spawnAdCounter[n] = 0; // reset this if event was sent
                                spawnVideoShortcutCounter[n] = 0;
                            }
                        }
                    }
                }

                // somebody is in front of video area
                if (spawnAdCounter[n] > triggersBeforeSpawnAdEvent) // has it been positive for awhile?
                {

                    if (!adAreaActivated[n]) // ignore if ad is aliready active
                    {
                        if (!videoAreaActivated[n]) // ignore if video is now active... 
                        {
                            bool didSendStartAdEvent = checkForSendActivateAdCommand(n);
                            if (didSendStartAdEvent)
                            {
                                adAreaActivated[n] = true; // activate ad
                                spawnAdCounter[n] = 0;
                                spawnVideoShortcutCounter[n] = -30; // force add to be present for a moment instead of flicking on video right away
                                spawnVideoCounter[n] = 0;
                                // ONLY when halt command is received from control to we make video area available again:
                                // redundant: videoAreaActivated[n] = false; // force video to be deactivated
                            }
                        }
                    }

                }

                // add is active and counting down to video start:
                if (spawnVideoCounter[n] > triggersBeforeSpawnVideoEvent) // misnomer! no longer spawning a video when ad times out.
                {
                    if (!videoAreaActivated[n]) // ignore if video has already been sent
                    {
                        if (adAreaActivated[n]) // add area must be triggered before video event can start
                        {
                            /* no longer activating video upon timeout
                            bool didSendStartVideoEvent = checkForSendActivateVideoCommand(n);
                            if (didSendStartVideoEvent)
                            {
                                adAreaActivated[n] = false; // deactivate ad
                                videoAreaActivated[n] = true; // activate video
                                spawnAdCounter[n] = 0; // reset this if event was sent
                                spawnVideoShortcutCounter[n] = 0;
                                spawnVideoCounter[n] = 0;
                            }*/

                            // new approach: just deactivate add - do not start a video.
                            bool didSendHaltAdEvent = checkForSendDeactivateAdCommand(n); 
                            if (didSendHaltAdEvent)
                            {
                                adAreaActivated[n] = false;
                                videoAreaActivated[n] = false;
                                spawnAdCounter[n] = 0 - triggersBeforeShowAnotherAddEvent; // don't show another add for a sec... 
                                spawnVideoShortcutCounter[n] = 0;
                                spawnVideoCounter[n] = 0;
                            }
                        }
                    }
                }


                /************************************************************************/
                /* DEactivation commands */
                /************************************************************************/
                if (haltAdCounter[n] > triggersBeforeAdHaltEvent) // has it been negative/off for awhile?
                {
                    if (adAreaActivated[n]) // is the ad showing?
                    {
                        bool didSendHaltAdEvent = checkForSendDeactivateAdCommand(n);
                        if (didSendHaltAdEvent)
                        {
                            adAreaActivated[n] = false;
                            videoAreaActivated[n] = false;
                            //videoAreaActivated[n] = false;
                            haltAdCounter[n] = 0; // reset if event was sent
                            haltVideoCounter[n] = 0;
                        }
                    }
                }
                if (haltVideoCounter[n] > triggersBeforeVideoHaltEvent)
                {
                    if (videoAreaActivated[n]) // is the video showing?
                    {
                        bool didSendHaltVidEvent = checkForSendDeactivateVideoCommand(n);
                        if (didSendHaltVidEvent)
                        {
                            adAreaActivated[n] = false;
                            videoAreaActivated[n] = false;
                            //videoAreaActivated[n] = false;
                            haltAdCounter[n] = 0; // reset if event was sent
                            haltVideoCounter[n] = 0;
                            spawnAdCounter[n] = -15; // pause before showing ad again after video in interupted or comes to end
                        }
                    }
                }

                /************************************************************************/
                /* prevent rapid transmission of commands: */
                /************************************************************************/
                if (sendEventDisabled[n])
                {
                    waitBeforeSendAnotherEventCounter[n] += 1;
                    if (waitBeforeSendAnotherEventCounter[n] > waitBeforeSendEventLimit)
                    {
                        sendEventDisabled[n] = false; // we have waited for some time, it is okay to send another event...
                    }
                }

                if (videoAreaActivated[n] != prevVideoAreaActivated[n]) // did anything change?
                {
                    dirty = true;
                }
                if (adAreaActivated[n] != prevAdAreaActivated[n]) // did anything change?
                {
                    dirty = true;
                }
                if (keepVideoPlayingActivated[n] != keepVideoPlayingActivated[n]) // did anything change?
                {
                    dirty = true;
                }
            }
        }

        /***************************************/
        #region drawStatusGraphics:

        public void draw()
        {
            float drawPosnXLeft;
            float drawPosnXRight;
            float drawPosnXBottom;
            float drawPosnXTop;

            //float gridPosnY;

            int n;
            int areaOfGridStartX, areaOfGridEndX;
            int areaOfGridStartY, areaOfGridEndY;


            for (n = 0; n < numberOfVideoAreas; ++n)
            {
                areaOfGridStartX = gridXRanges[n, 0];
                areaOfGridEndX = gridXRanges[n, 1];

                areaOfGridStartY = gridYRanges[n, 0];
                areaOfGridEndY = gridYRanges[n, 1];

                drawPosnXLeft = ((float)areaOfGridStartX * gridResolutionW) + gridStartX;
                drawPosnXRight = ((float)areaOfGridEndX * gridResolutionW) + gridStartX;

                drawPosnXBottom = ((float)areaOfGridStartY * gridResolutionH) + gridStartY;
                drawPosnXTop = ((float)areaOfGridEndY * gridResolutionH) + gridStartY;


                // draw near targets

                if (adAreaActivated[n])
                {
                    GL.Color4(0.11f, 0.60f, 0.11f, 0.5f);
                }
                else
                {
                    GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);

                }
                GL.Begin(BeginMode.Quads);


                GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeFar, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeFar, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeFar, drawPosnXTop);
                GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeFar, drawPosnXTop);
                
                if (videoAreaActivated[n])
                {
                    GL.Color4(0.11f, 0.60f, 0.11f, 0.5f);
                }
                else
                {
                    GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);
                }

                GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeNear, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeNear, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeNear, drawPosnXTop);
                GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeNear, drawPosnXTop);

                if (keepVideoPlayingActivated[n])
                {
                    GL.Color4(0.11f, 0.60f, 0.11f, 0.5f);
                }
                else
                {
                    GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);
                }

                GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeMid, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeMid, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeMid, drawPosnXTop);
                GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeMid, drawPosnXTop);

                // draw footprint on/at wall:
                GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);
                GL.Vertex3(drawPosnXLeft, -5.0f, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, -5.0f, drawPosnXBottom);
                GL.Vertex3(drawPosnXRight, -5.0f, drawPosnXTop);
                GL.Vertex3(drawPosnXLeft, -5.0f, drawPosnXTop);
                GL.End();


                GL.Color4(0.9f, 0.90f, 0.11f, 1.0f); // switch to yellow...
                // outline near targets
                if (immediateVideoAreaActivated[n])
                {
                    GL.Begin(BeginMode.LineLoop);
                    GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeNear, drawPosnXBottom);
                    GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeNear, drawPosnXBottom);
                    GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeNear, drawPosnXTop);
                    GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeNear, drawPosnXTop);
                    GL.End();
                }
                // outline far targets
                if (immediateAdAreaActivated[n])
                {
                    GL.Begin(BeginMode.LineLoop);
                    GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeFar, drawPosnXBottom);
                    GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeFar, drawPosnXBottom);
                    GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeFar, drawPosnXTop);
                    GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeFar, drawPosnXTop);
                    GL.End();
                }

                if (keepVideoPlayingActivated[n])
                {
                    GL.Begin(BeginMode.LineLoop);
                    GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeMid, drawPosnXBottom);
                    GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeMid, drawPosnXBottom);
                    GL.Vertex3(drawPosnXRight, 0.0f - toggleRangeMid, drawPosnXTop);
                    GL.Vertex3(drawPosnXLeft, 0.0f - toggleRangeMid, drawPosnXTop);
                    GL.End();
                }

            }
        }
        #endregion drawStatusGraphics
        /***************************************/

        /***************************************/
        #region sendEvents

        /*
        private void sendStopVideoCommand(int whichVideoArea)
        {
            videoAreaActivated[whichVideoArea] = false; // trigger this as off
            sendDeactivateVideoCommand(whichVideoArea);
            haltVideoCounter[whichVideoArea] = 0;
            spawnVideoCounterTall[whichVideoArea] = 0;
            spawnVideoCounterShort[whichVideoArea] = 0;

        }*/

        private bool checkForSendActivateAdCommand(int whichVideoArea)
        {
            if (!sendEventDisabled[whichVideoArea])
            {
                waitBeforeSendAnotherEventCounter[whichVideoArea] = 0;
                sendEventDisabled[whichVideoArea] = true;
                doSendActivateAdCommand(whichVideoArea);
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }

        private bool checkForSendDeactivateAdCommand(int whichVideoArea)
        {
            if (!sendEventDisabled[whichVideoArea])
            {
                waitBeforeSendAnotherEventCounter[whichVideoArea] = 0;
                sendEventDisabled[whichVideoArea] = true;
                doSendDeactivateAdCommand(whichVideoArea);
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }

        private void doSendActivateAdCommand(int whichVideoArea)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Ad Command #" + whichVideoArea);
            ArrayList argList = new ArrayList();
            argList.Add(whichVideoArea);
            //argList.Add(isTall);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "activateAdArea";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendDeactivateAdCommand(int whichVideoArea)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] send DE-Activate Ad Command #" + whichVideoArea);
            ArrayList argList = new ArrayList();
            argList.Add(whichVideoArea);
            //argList.Add(isTall);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "deactivateAdArea";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }
        
        private bool checkForSendActivateVideoCommand(int whichVideoArea)
        {
            if (!sendEventDisabled[whichVideoArea])
            {
                waitBeforeSendAnotherEventCounter[whichVideoArea] = 0;
                sendEventDisabled[whichVideoArea] = true;
                doSendActivateVideoCommand(whichVideoArea);
                doSendDeactivateAdCommand(whichVideoArea); // force removal of ad
                videoResetTimers[whichVideoArea].startTimer(); // force expiration timer (moslty for testing where video app is not available)
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }

        private bool checkForSendDeactivateVideoCommand(int whichVideoArea)
        {
            if (!sendEventDisabled[whichVideoArea])
            {
                waitBeforeSendAnotherEventCounter[whichVideoArea] = 0;
                sendEventDisabled[whichVideoArea] = true;
                doSendDeactivateVideoCommand(whichVideoArea);
                //videoResetTimers[whichVideoArea].stopTimer(); // force expiration timer (moslty for testing where video app is not available)
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }

        private void doSendActivateVideoCommand(int whichVideoArea)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivateVideoCommand #" + whichVideoArea );
            ArrayList argList = new ArrayList();
            argList.Add(whichVideoArea);
            //argList.Add(isTall);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "activateVideoArea";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendDeactivateVideoCommand(int whichVideoArea)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] send DE-Activate VideoCommand #" + whichVideoArea);
            ArrayList argList = new ArrayList();
            argList.Add(whichVideoArea);
            //argList.Add(isTall);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "deactivateVideoArea";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }


        #endregion sendEvents
        /***************************************/

    }
}
