using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SecondstoryCommon;
using System.Collections;
using SensorControlApp.OpenGLProgrammablePipeline;

namespace SensorControlApp.WallCommunicationsAndControl
{
    class ReactiveAreasControlTwoDimensional
    {
        private ReactiveAreasDataTwoDimensional depthAreaDataVar;

        // for passsing events to MainClass
        public event AppEventHandler OnEvent;
        private string myEventSource = "reactiveArea2DControl";

        private static int maxNumberOfReactiveRegions = 400;
        private int actualNumberOfReactiveRegionsW = 4;
        private int actualNumberOfReactiveRegionsH = 4;
        private int actualNumberOfReactiveRegions = 16;

        public bool[] regionActivatedNear = new bool[maxNumberOfReactiveRegions];

        public bool[] prevRegionActivatedNear = new bool[maxNumberOfReactiveRegions];

        private int[] regionNearDataCount = new int[maxNumberOfReactiveRegions];
        
        // secondary layer of activation reduces noisy activation:
        private bool[] sensingRegionNear = new bool[maxNumberOfReactiveRegions];

        private int[] doActivateRegionCounterNear = new int[maxNumberOfReactiveRegions]; // keep track of how long it has been positive

        private int[] deactivateRegionCounterNear = new int[maxNumberOfReactiveRegions];

        // prevent rapid fire of events:
        private int[] waitBeforeSendAnotherEventCounter = new int[maxNumberOfReactiveRegions];
        private bool[] sendEventDisabled = new bool[maxNumberOfReactiveRegions];

        // ************************************************
        // the following times are based upon rate in MAINAPP: depthDataTransmitRate (33ms)
        private int waitBeforeSendEventLimit = 6; // 6.24 was 6 // changed from 10 to 6 (found that it was missing deactivate commands) (6 = 0.2s)

        // fast trigger (reactive part)
        private int triggersBeforeActivateNearEvent = 5; // 6/24 was 5 // spawn event only after it has been the same for n times (5 = 0.165s)

 
        // kills both near and far and button (whichever is active):
        private int triggersBeforeHaltNearEvent = 30; // when to disable active area


        // what ranges of user grid are local to activation areas:
        private int[,] activationGridXRanges = new int[maxNumberOfReactiveRegions, 2]; // 400 activation areas, high low grid range (left and right)
        private int[,] activationGridYRanges = new int[maxNumberOfReactiveRegions, 2]; // 400 activation areas, bottom and top grid range (bottom and top)

         // local store of user grid:
        private int gridCountWidth = 200;
        private int gridCountHeight = 200;

        // size of activation areas (as percentage of entire wall)
        private float activationAreaW = 0.06f; // percent width of entire wall
        private float activationAreaH = 0.06f; // percent width of entire wall
        //private float activationAreaBot = 0.06f; // percent height of entire wall
        //private float activationAreaTop = 0.60f; // percent height of entire wall

        // near and far toggle ranges:
        public float toggleRangeNear = 1000.0f; 
 
        // these values are used only to draw user area on screen (copied from
        private float gridWidth = 7560.0f; // (1080 * 7)
        private float gridHeight = 3840.0f; // (1920 * 2)
        private float gridStartX = -3780.0f;
        private float gridStartY = -1920.0f;
        private float gridResolutionW = 250.0f;
        private float gridResolutionH = 100.0f;


        PPDrawTexturedQuad[] drawRegions = new PPDrawTexturedQuad[maxNumberOfReactiveRegions];


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
        /// <summary>
        /// lays out activation areas and touch areas for wall interaction
        /// </summary>
        /// <param name="whichHCount"> user sensor grid width resolution </param>
        /// <param name="whichVCount"> user sensor grid height resolution </param>
        /// 
        public ReactiveAreasControlTwoDimensional(int whichHCount, int whichVCount, int whichTransmissionRate)
        {
            depthAreaDataVar = new ReactiveAreasDataTwoDimensional();

            actualNumberOfReactiveRegionsW = depthAreaDataVar.actualNumberOfActivationAreasW;
            actualNumberOfReactiveRegionsH = depthAreaDataVar.actualNumberOfActivationAreasH;
            activationAreaW = depthAreaDataVar.sensorActivationAreaWidthPercentOfTotal;
            activationAreaH = depthAreaDataVar.sensorActivationAreaHeightPercentOfTotal;
            actualNumberOfReactiveRegions = actualNumberOfReactiveRegionsW * actualNumberOfReactiveRegionsH;

            toggleRangeNear = depthAreaDataVar.sensorTriggerDepth* 1000f;

            double transmissionRate = (double)whichTransmissionRate; // what is the length of one tick in ms?
            double numberOfTriggersInASecond = 1000.0 / transmissionRate; // this is our rough frame rate
            triggersBeforeActivateNearEvent = (int)Math.Round(depthAreaDataVar.triggerOn * numberOfTriggersInASecond);

            triggersBeforeHaltNearEvent = (int)Math.Round(depthAreaDataVar.triggerOff * numberOfTriggersInASecond);

            gridCountWidth = whichHCount;
            gridCountHeight = whichVCount;

            gridResolutionW = gridWidth / (float)whichHCount;
            gridResolutionH = gridHeight / (float)whichVCount;

            assignGridRanges();

            //string whichTimerID = "";

            resetActive(false);

        }

        public void initOpenGL()
        {
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                drawRegions[i] = new PPDrawTexturedQuad();
                drawRegions[i].initOpenGL();
            }
        }

        public void createVAO()
        {
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                drawRegions[i].createVAO(0, "vertPosn", "", "vertUV");
            }
        }

        public void setUserGridMeasurementRanges(double whichWidth, double whichStartY, double whichEndY)
        {

            whichWidth *= 1000;
            whichStartY *= 1000;
            whichEndY *= 1000;



            gridWidth = (float)whichWidth;
            gridHeight = (float)(whichEndY - whichStartY);
            gridStartX = 0f - gridWidth / 2.0f;
            gridStartY = (float)whichStartY;

            gridResolutionW = gridWidth / (float)gridCountWidth;
            gridResolutionH = gridHeight / (float)gridCountHeight;

            assignGridRanges();

        }

        public void resetActive(bool disableBriefly)
        {
            initializeCountersAndAreaStatus();
            int disableCountersForAMoment = -120; // forces counters to start well below limits (wait for system to come up, initialize)
            if (disableBriefly)
                disableCountersForAMoment = -60;

            // reset/init all variables:
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                sendEventDisabled[i] = true;


                waitBeforeSendAnotherEventCounter[i] = disableCountersForAMoment;
                doActivateRegionCounterNear[i] = disableCountersForAMoment;               

                deactivateRegionCounterNear[i] = disableCountersForAMoment;
            }

        }

        public void resetActiveForRegion(int whichRegion)
        {
            int disableCountersForAMoment = -30;

            waitBeforeSendAnotherEventCounter[whichRegion] = disableCountersForAMoment;
            doActivateRegionCounterNear[whichRegion] = disableCountersForAMoment;

            deactivateRegionCounterNear[whichRegion] = disableCountersForAMoment;
        }

        private void assignGridRanges()
        {
            int i,j;
            int whichRegionIndex;

            double areaStartX, areaEndX;
            int igridStartX, igridEndX;

            double areaStartY, areaEndY;
            int igridStartY, igridEndY;

            double numberOfDisplaysW = actualNumberOfReactiveRegionsW;
            double pctWidthOfDisplay = 1.0 / numberOfDisplaysW;
            double numberOfDisplaysH = actualNumberOfReactiveRegionsH;
            double pctHeightOfDisplay = 1.0 / numberOfDisplaysH;

            double centerOfActiveAreaPctTotalW = 0.0;
            double centerOfActiveAreaPctTotalH = 0.0;
            
            // reactive regions are the large "buttons" located well in front of the display area:
            /*
            for (i = 0; i < actualNumberOfReactiveRegionsW; ++i)
            {
                // where is the middle of the display
                centerOfActiveAreaPctTotalW = (((double)i + 1.0) * pctWidthOfDisplay) - (0.5 * pctWidthOfDisplay);
                // still using percentage of wall width here 
                areaStartX = centerOfActiveAreaPctTotalW - (double)activationAreaW / 2.0;
                areaEndX = areaStartX + (double)activationAreaW;

                // convert to user grid positions
                igridStartX = (int)Math.Floor((double)gridCountWidth * areaStartX);
                igridEndX = (int)Math.Ceiling((double)gridCountWidth * areaEndX);

                for (j = 0; j < actualNumberOfReactiveRegionsH; ++j)
                {
                    whichRegionIndex = (i * actualNumberOfReactiveRegionsH) + j;

                    // where is the middle of the display
                    centerOfActiveAreaPctTotalH = (((double)j + 1.0) * pctHeightOfDisplay) - (0.5 * pctHeightOfDisplay);
                    // still using percentage of wall width here 
                    areaStartY = centerOfActiveAreaPctTotalH - (double)activationAreaH / 2.0;
                    areaEndY = areaStartY + (double)activationAreaH;

                    // vertical areas are the same for all activation areas:
                    igridStartY = (int)Math.Floor((double)gridCountHeight * areaStartY);
                    igridEndY = (int)Math.Ceiling((double)gridCountHeight * areaEndY);

                    activationGridXRanges[whichRegionIndex, 0] = igridStartX;
                    activationGridXRanges[whichRegionIndex, 1] = igridEndX;

                    activationGridYRanges[whichRegionIndex, 0] = igridStartY;
                    activationGridYRanges[whichRegionIndex, 1] = igridEndY;
                }
            }*/
            
            
            // reactive regions are the large "buttons" located well in front of the display area:
            for (i = 0; i < actualNumberOfReactiveRegionsH; ++i)
            {
                // where is the middle of the display
                centerOfActiveAreaPctTotalH = 1.0 - ((((double)i + 1.0) * pctHeightOfDisplay) - (0.5 * pctHeightOfDisplay)); // 6/25 make it start at bottom

                // still using percentage of wall width here 
                areaStartY = centerOfActiveAreaPctTotalH - (double)activationAreaH / 2.0;
                areaEndY = areaStartY + (double)activationAreaH;

                // vertical areas are the same for all activation areas:
                igridStartY = (int)Math.Floor((double)gridCountHeight * areaStartY);
                igridEndY = (int)Math.Ceiling((double)gridCountHeight * areaEndY);

                for (j = 0; j < actualNumberOfReactiveRegionsW; ++j)
                {
                    whichRegionIndex = (i * actualNumberOfReactiveRegionsW) + j;


                    // where is the middle of the display
                    centerOfActiveAreaPctTotalW = (((double)j + 1.0) * pctWidthOfDisplay) - (0.5 * pctWidthOfDisplay);
                    // still using percentage of wall width here 
                    areaStartX = centerOfActiveAreaPctTotalW - (double)activationAreaW / 2.0;
                    areaEndX = areaStartX + (double)activationAreaW;

                    // convert to user grid positions
                    igridStartX = (int)Math.Floor((double)gridCountWidth * areaStartX);
                    igridEndX = (int)Math.Ceiling((double)gridCountWidth * areaEndX);


                    activationGridXRanges[whichRegionIndex, 0] = igridStartX;
                    activationGridXRanges[whichRegionIndex, 1] = igridEndX;

                    activationGridYRanges[whichRegionIndex, 0] = igridStartY;
                    activationGridYRanges[whichRegionIndex, 1] = igridEndY;
                }
            }       
        }

        private void initializeCountersAndAreaStatus()
        {
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                regionActivatedNear[i] = false;
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

            for (n = 0; n < actualNumberOfReactiveRegions; ++n)
            {
                sensingRegionNear[n] = false; // false until proven true
                //keepVideoPlayingActivated[n] = false;
                gridStartX = activationGridXRanges[n, 0];
                gridEndX = activationGridXRanges[n, 1];
                gridStartY = activationGridYRanges[n, 0];
                gridEndY = activationGridYRanges[n, 1];

                regionNearDataCount[n] = 0;

                for (i = gridStartX; i < gridEndX; ++i) // only check relevant grid areas (no need to check whole wall)
                {
                    for (j = gridStartY; j < gridEndY; ++j)
                    {
                        if ((depthData[i, j] > 0.0f) && (depthData[i, j] < toggleRangeNear))
                        {
                            regionNearDataCount[n] += 1;
                        }
                    }
                }

                if (regionNearDataCount[n] >= 1) // 6/24 waqs set to > 2
                    sensingRegionNear[n] = true;
                else
                    sensingRegionNear[n] = false;
            } // finished running through all of the activation areas




            // compare and test 

            // copy previous data to prev array:
            Array.Copy(regionActivatedNear, prevRegionActivatedNear, regionActivatedNear.Length);

            bool didSendActivateNearEvent = false;

            bool didSendDeactivateNearEvent = false;


            for (n = 0; n < actualNumberOfReactiveRegions; ++n)
            {
                /************************************************************************/
                /* current status and counters */
                /************************************************************************/

                if (sensingRegionNear[n]) // someone is closer to screen!
                {
                    doActivateRegionCounterNear[n] += 1;
                    deactivateRegionCounterNear[n] = 0;
                }
                else // no one is there right now...
                {
                    doActivateRegionCounterNear[n] = 0;
                    deactivateRegionCounterNear[n] += 1;
                }

                /************************************************************************/
                /* activation commands */
                /************************************************************************/


                if (doActivateRegionCounterNear[n] > triggersBeforeActivateNearEvent)
                {
                    if (!regionActivatedNear[n])
                    {
                        didSendActivateNearEvent = checkForSendActivateNearCommand(n);
                        if (didSendActivateNearEvent)
                        {
                            regionActivatedNear[n] = true;
                            doActivateRegionCounterNear[n] = 0;
                        }
                    }
                }

                /************************************************************************/
                /* DEactivation commands */
                /************************************************************************/

                if (deactivateRegionCounterNear[n] > triggersBeforeHaltNearEvent)
                {
                    if (regionActivatedNear[n])
                    {
                        didSendDeactivateNearEvent = checkForSendDeactivateNearCommand(n);
                        if (didSendDeactivateNearEvent)
                        {
                            regionActivatedNear[n] = false;
                            deactivateRegionCounterNear[n] = 0;
                        }
                    }
                    else
                    {
                        deactivateRegionCounterNear[n] = 0;
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

                /************************************************************************/
                /* mark as dirty if state changed */
                /************************************************************************/
                if (regionActivatedNear[n] != prevRegionActivatedNear[n]) // did anything change?
                {
                    dirty = true;
                }
            }
        }

        /***************************************/
        #region drawStatusGraphics:

        public void draw(int whichColorAdjustPointer)
        {
            float drawPosnXLeft;
            float drawPosnXRight;
            float drawPosnYBottom;
            float drawPosnYTop;

            
            int n;
            int areaOfGridStartX, areaOfGridEndX;
            int areaOfGridStartY, areaOfGridEndY;
            Vector4 textureColorOffset = new Vector4(1.0f, 1.0f, 1.0f, 0.5f); // transparent white


            for (n = 0; n < actualNumberOfReactiveRegions; ++n)
            {
                areaOfGridStartX = activationGridXRanges[n, 0];
                areaOfGridEndX = activationGridXRanges[n, 1];

                areaOfGridStartY = activationGridYRanges[n, 0];
                areaOfGridEndY = activationGridYRanges[n, 1];

                drawPosnXLeft = ((float)areaOfGridStartX * gridResolutionW) + gridStartX;
                drawPosnXRight = ((float)areaOfGridEndX * gridResolutionW) + gridStartX;

                drawPosnYBottom = ((float)areaOfGridStartY * gridResolutionH) + gridStartY;
                drawPosnYTop = ((float)areaOfGridEndY * gridResolutionH) + gridStartY;


                // draw near targets

                if (regionActivatedNear[n])
                {
                    GL.Color4(0.11f, 0.60f, 0.11f, 0.5f);
                    textureColorOffset.X = 0.11f;
                    textureColorOffset.Y = 0.60f;
                    textureColorOffset.Z = 0.11f;
                    textureColorOffset.W = 0.5f;

                }
                else
                {
                    GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);
                    textureColorOffset.X = 0.6f;
                    textureColorOffset.Y = 0.11f;
                    textureColorOffset.Z = 0.11f;
                    textureColorOffset.W = 0.5f;
                }
                GL.Uniform4(whichColorAdjustPointer, ref textureColorOffset);

                GL.Begin(BeginMode.LineLoop);

                GL.Vertex3(drawPosnXLeft, drawPosnYBottom, toggleRangeNear);
                GL.Vertex3(drawPosnXRight, drawPosnYBottom, toggleRangeNear);
                GL.Vertex3(drawPosnXRight, drawPosnYTop, toggleRangeNear);
                GL.Vertex3(drawPosnXLeft, drawPosnYTop, toggleRangeNear);

                GL.End();

                GL.Color4(0.9f, 0.90f, 0.11f, 1.0f); // switch to yellow...
                textureColorOffset.X = 0.9f;
                textureColorOffset.Y = 0.9f;
                textureColorOffset.Z = 0.11f;
                textureColorOffset.W = 1.0f;
                GL.Uniform4(whichColorAdjustPointer, ref textureColorOffset);

                // outline near targets
                if (sensingRegionNear[n])
                {
                    GL.Begin(BeginMode.LineLoop);
                    GL.Vertex3(drawPosnXLeft, drawPosnYBottom, toggleRangeNear);
                    GL.Vertex3(drawPosnXRight, drawPosnYBottom, toggleRangeNear);
                    GL.Vertex3(drawPosnXRight, drawPosnYTop, toggleRangeNear);
                    GL.Vertex3(drawPosnXLeft, drawPosnYTop, toggleRangeNear);
                    GL.End();
                }

            }
       
        }
        #endregion drawStatusGraphics
        /***************************************/


        // ***********************************
        #region create and update VBOs
        // ***********************************



        // ***********************************
        #endregion create and update VBOs
        // ***********************************
        /***************************************/
        #region sendEvents

        private bool checkForSendActivateNearCommand(int whichRegion)
        {
            if (!sendEventDisabled[whichRegion])
            {
                waitBeforeSendAnotherEventCounter[whichRegion] = 0;
                sendEventDisabled[whichRegion] = true;
                doSendActivateNearCommand(whichRegion);
                return true;
            }
            else
            {
                return false;
            }
        }


        private bool checkForSendDeactivateNearCommand(int whichRegion)
        {
            if (!sendEventDisabled[whichRegion])
            {
                waitBeforeSendAnotherEventCounter[whichRegion] = 0;
                sendEventDisabled[whichRegion] = true;
                doSendDeactivateNearCommand(whichRegion);
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }


        private void doSendActivateNearCommand(int whichRegion)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Near Command #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "activateRegion2DNear";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendDeactivateNearCommand(int whichRegion)
        {
            System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] send DE-Activate Near #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "deactivateRegion2DNear";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }
        
        #endregion sendEvents
        /***************************************/

    }
}
