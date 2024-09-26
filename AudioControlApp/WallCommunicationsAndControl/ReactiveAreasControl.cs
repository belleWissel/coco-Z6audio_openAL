using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SecondstoryCommon; // used for AppEventHandler
using System.Collections;

using AudioControlApp.OpenGLProgrammablePipeline;
using AudioControlApp.Shaders;

namespace AudioControlApp.WallCommunicationsAndControl
{
    public class ReactiveAreasControl
    {
        private ReactiveAreasDataFromXML depthAreaDataVar;

        // for passsing events to MainClass
        public event AppEventHandler OnEvent;
        private string myEventSource = "reactiveAreaControl";

        private static int maxNumberOfReactiveRegions = 16;
        public int actualNumberOfReactiveRegions = 4;
        public int regionStartIndex = 0;

        public bool[] regionActivatedNear = new bool[maxNumberOfReactiveRegions];
        public bool[] regionActivatedMid = new bool[maxNumberOfReactiveRegions];
        public bool[] regionActivatedFar = new bool[maxNumberOfReactiveRegions];


        public bool[] prevRegionActivatedNear = new bool[maxNumberOfReactiveRegions];
        public bool[] prevRegionActivatedMid = new bool[maxNumberOfReactiveRegions];
        public bool[] prevRegionActivatedFar = new bool[maxNumberOfReactiveRegions];


        private int[] regionNearDataCount = new int[maxNumberOfReactiveRegions];
        private int[] regionMidDataCount = new int[maxNumberOfReactiveRegions];
        private int[] regionFarDataCount = new int[maxNumberOfReactiveRegions];

        // secondary layer of activation reduces noisy activation:
        private bool[] sensingRegionNear = new bool[maxNumberOfReactiveRegions];
        private bool[] sensingRegionMid = new bool[maxNumberOfReactiveRegions];
        private bool[] sensingRegionFar = new bool[maxNumberOfReactiveRegions];

        private int[] doActivateRegionCounterNear = new int[maxNumberOfReactiveRegions]; // keep track of how long it has been positive
        private int[] doActivateRegionCounterMid = new int[maxNumberOfReactiveRegions]; // keep track of how long it has been positive
        private int[] doActivateRegionCounterFar = new int[maxNumberOfReactiveRegions]; // keep track of how long it has been positive

        private int[] deactivateRegionCounterNear = new int[maxNumberOfReactiveRegions];
        private int[] deactivateRegionCounterMid = new int[maxNumberOfReactiveRegions];
        private int[] deactivateRegionCounterFar = new int[maxNumberOfReactiveRegions];

        // prevent rapid fire of events:
        private int[] waitBeforeSendAnotherEventCounter = new int[maxNumberOfReactiveRegions];
        private bool[] sendEventDisabled = new bool[maxNumberOfReactiveRegions];

        // ************************************************
        // the following times are based upon rate in MAINAPP: depthDataTransmitRate (33ms)
        private int waitBeforeSendEventLimit = 1; // changed from 10 to 6 then to 4 (found that it was missing deactivate commands) (6 = 0.2s)

        // slower trigger (someone has to wait for some time in front before showing CALL TO ACTION):
        private int triggersBeforeActivateFarEvent = 1; // spawn CTA event after many cycles (80 = 2.6 s)
        // fast trigger (reactive part)
        private int triggersBeforeActivateMidEvent = 1; // 
        // fast trigger (reactive part)
        private int triggersBeforeActivateNearEvent = 1; // spawn event only after it has been the same for n times (5 = 0.165s)


        // kills both near and far and button (whichever is active):
        // set automatically below...
        private int triggersBeforeHaltNearEvent = 2; // when to disable active area (set automatically below)
        private int triggersBeforeHaltMidEvent = 2; // when to disable active area
        private int triggersBeforeHaltFarEvent = 2; // when to disable active area


        // what ranges of user grid are local to activation areas:
        private int[,] activationGridXRanges = new int[maxNumberOfReactiveRegions, 2]; // 7 activation areas, high low grid range (left and right)
        private int[,] activationGridYRanges = new int[maxNumberOfReactiveRegions, 2]; // 7 activation areas, bottom and top grid range (bottom and top)

        // local store of user grid:
        private int gridCountWidth = 120;
        private int gridCountHeight = 80;

        // size of activation areas (as percentage of entire wall)
        private float activationAreaW = 0.06f; // percent width of entire wall
        private float activationAreaBot = 0.06f; // percent height of entire wall
        private float activationAreaTop = 0.60f; // percent height of entire wall

        // near and far toggle ranges:
        public float toggleRangeFar = 1500.0f;
        public float toggleRangeMid = 1250.0f;
        public float toggleRangeNear = 1000.0f;


        // these values are used only to draw user area on screen (copied from
        private float gridWidth = 7560.0f; // (1080 * 7)
        private float gridHeight = 3840.0f; // (1920 * 2)
        private float gridStartX = -3780.0f;
        private float gridStartY = -1920.0f;
        private float gridResolutionW = 250.0f;
        private float gridResolutionH = 100.0f;


        //PPDrawTexturedQuad[] drawRegions = new PPDrawTexturedQuad[maxNumberOfReactiveRegions];

        // ****************************************************************************
        // shader variables
        shaderFileLoader simpleTextureShaderSource;

        int handleShader;

        private int shaderlocPosition,
            shaderlocColor, shaderlocOffset,
            shaderlocModelMatrix, shaderlocProjMatrix;

        PPDrawSensingTargetPlane[] drawNearRegions = new PPDrawSensingTargetPlane[maxNumberOfReactiveRegions];
        PPDrawSensingTargetPlane[] drawMidRegions = new PPDrawSensingTargetPlane[maxNumberOfReactiveRegions];
        PPDrawSensingTargetPlane[] drawFarRegions = new PPDrawSensingTargetPlane[maxNumberOfReactiveRegions];

        private bool readyToDraw = false;

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
        public ReactiveAreasControl(int whichHCount, int whichVCount, int whichTransmissionRate)
        {
            depthAreaDataVar = new ReactiveAreasDataFromXML();

            actualNumberOfReactiveRegions = depthAreaDataVar.actualNumberOfActivationAreas;
            regionStartIndex = depthAreaDataVar.areaStartIndex;
            activationAreaW = depthAreaDataVar.sensorActivationAreaWidthPercentOfTotal;
            activationAreaBot = depthAreaDataVar.sensorActivationAreaBottomPercentOfTotalHeight;
            activationAreaTop = depthAreaDataVar.sensorActivationAreaTopPercentOfTotalHeight;

            toggleRangeFar = depthAreaDataVar.sensorDepthFar * 1000f;
            toggleRangeNear = depthAreaDataVar.sensorDepthNear * 1000f;
            toggleRangeMid = depthAreaDataVar.sensorDepthMid * 1000f;

            double transmissionRate = (double)whichTransmissionRate; // what is the length of one tick in ms?
            double numberOfTriggersInASecond = 1000.0 / transmissionRate; // this is our rough frame rate
            triggersBeforeActivateNearEvent = (int)Math.Round(depthAreaDataVar.triggerOnNear * numberOfTriggersInASecond);
            triggersBeforeActivateMidEvent = (int)Math.Round(depthAreaDataVar.triggerOnMid * numberOfTriggersInASecond);
            triggersBeforeActivateFarEvent = (int)Math.Round(depthAreaDataVar.triggerOnFar * numberOfTriggersInASecond);

            triggersBeforeHaltNearEvent = (int)Math.Round(depthAreaDataVar.triggerOffNear * numberOfTriggersInASecond);
            triggersBeforeHaltMidEvent = (int)Math.Round(depthAreaDataVar.triggerOffMid * numberOfTriggersInASecond);
            triggersBeforeHaltFarEvent = (int)Math.Round(depthAreaDataVar.triggerOffFar * numberOfTriggersInASecond);

            System.Diagnostics.Debug.WriteLine("[ReactiveArea] region counters: activate: [" + triggersBeforeActivateFarEvent + ", " + triggersBeforeActivateMidEvent + ", " + triggersBeforeActivateNearEvent + " ]");
            System.Diagnostics.Debug.WriteLine("[ReactiveArea] region counters: deactivate: [" + triggersBeforeHaltFarEvent + ", " + triggersBeforeHaltMidEvent + ", " + triggersBeforeHaltNearEvent + " ]");

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
            simpleTextureShaderSource = new shaderFileLoader();
            simpleTextureShaderSource.loadShaders("shaders\\FlatShader2.vp", "shaders\\FlatShader2.fp");

            // ****************************************************
            // create shader app:
            handleShader = ShaderLoader.CreateProgram(simpleTextureShaderSource.vertexShaderSource,
                                            simpleTextureShaderSource.fragmentShaderSource);

            // ****************************************************
            GL.UseProgram(handleShader);


            // uniforms:
            shaderlocPosition = GL.GetAttribLocation(handleShader, "vPosition");


            //attributes
            shaderlocModelMatrix = GL.GetUniformLocation(handleShader, "mModelMatrix");
            shaderlocProjMatrix = GL.GetUniformLocation(handleShader, "mProjectionMatrix");
            shaderlocColor = GL.GetUniformLocation(handleShader, "vColorValue");
            shaderlocOffset = GL.GetUniformLocation(handleShader, "vPositionOffset");

            createVAO(handleShader, "vPosition", "", "");

            GL.UseProgram(0);
            // ****************************************************

            readyToDraw = true;
        }

        public void createVAO(int whichShaderHandle, string whichVertPosVarName, string whichNormVarName, string whichUVVarName)
        {
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                drawNearRegions[i] = new PPDrawSensingTargetPlane();
                drawMidRegions[i] = new PPDrawSensingTargetPlane();
                drawFarRegions[i] = new PPDrawSensingTargetPlane();
            }

            Vector3 ulPoint, lrPoint;
            float drawPosnXLeft;
            float drawPosnXRight;
            float drawPosnYBottom;
            float drawPosnYTop;
            float pctGapBetweenAreas = 1.0f;


            int areaOfGridStartX, areaOfGridEndX;
            int areaOfGridStartY, areaOfGridEndY;



            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                areaOfGridStartX = activationGridXRanges[i, 0];
                areaOfGridEndX = activationGridXRanges[i, 1];

                areaOfGridStartY = activationGridYRanges[i, 0];
                areaOfGridEndY = activationGridYRanges[i, 1];

                drawPosnXLeft = ((float)areaOfGridStartX * gridResolutionW) + gridStartX;
                drawPosnXRight = ((float)areaOfGridEndX * gridResolutionW) + gridStartX;

                drawPosnYBottom = ((float)areaOfGridStartY * gridResolutionH) + gridStartY;
                drawPosnYTop = ((float)areaOfGridEndY * gridResolutionH) + gridStartY;



                ulPoint.X = drawPosnXLeft;// + 0.005f * pctGapBetweenAreas*(drawPosnXRight - drawPosnXLeft);
                lrPoint.X = drawPosnXRight;// - 0.005f * pctGapBetweenAreas * (drawPosnXRight - drawPosnXLeft);
                ulPoint.Y = drawPosnYTop;
                lrPoint.Y = drawPosnYBottom;
                ulPoint.Z = toggleRangeNear;
                lrPoint.Z = toggleRangeNear;
                drawNearRegions[i].initOpenGL(ulPoint, lrPoint);
                ulPoint.Z = toggleRangeMid;
                lrPoint.Z = toggleRangeMid;
                drawMidRegions[i].initOpenGL(ulPoint, lrPoint);
                ulPoint.Z = toggleRangeFar;
                lrPoint.Z = toggleRangeFar;
                drawFarRegions[i].initOpenGL(ulPoint, lrPoint);
            }

            // ****************************************************
            // 6. create VAO:
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                drawNearRegions[i].createVAO(whichShaderHandle, whichVertPosVarName, whichNormVarName, whichUVVarName);
                drawMidRegions[i].createVAO(whichShaderHandle, whichVertPosVarName, whichNormVarName, whichUVVarName);
                drawFarRegions[i].createVAO(whichShaderHandle, whichVertPosVarName, whichNormVarName, whichUVVarName);
            }


            // ****************************************************

            readyToDraw = true;

        }

        public void setUserGridMeasurementRanges(double whichWidth, double whichStartY, double whichEndY)
        {

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
                doActivateRegionCounterMid[i] = disableCountersForAMoment;
                doActivateRegionCounterFar[i] = disableCountersForAMoment;


                deactivateRegionCounterNear[i] = disableCountersForAMoment;
                deactivateRegionCounterMid[i] = disableCountersForAMoment;
                deactivateRegionCounterFar[i] = disableCountersForAMoment;
            }

        }

        public void resetActiveForRegion(int whichRegion)
        {
            int disableCountersForAMoment = -30;

            waitBeforeSendAnotherEventCounter[whichRegion] = disableCountersForAMoment;
            doActivateRegionCounterNear[whichRegion] = disableCountersForAMoment;
            doActivateRegionCounterMid[whichRegion] = disableCountersForAMoment;
            doActivateRegionCounterFar[whichRegion] = disableCountersForAMoment;

            deactivateRegionCounterNear[whichRegion] = disableCountersForAMoment;
            deactivateRegionCounterMid[whichRegion] = disableCountersForAMoment;
            deactivateRegionCounterFar[whichRegion] = disableCountersForAMoment;

        }


        private void assignGridRanges()
        {
            int i;

            double areaStartX, areaEndX;
            int igridStartX, igridEndX;

            //double areaStartY, areaEndY;
            int igridStartY, igridEndY;

            double numberOfDisplays = actualNumberOfReactiveRegions;
            double pctWidthOfDisplay = 1.0 / numberOfDisplays;
            //double halfOfDisplays = numberOfDisplays / 2.0;
            double centerOfActiveAreaPctTotal = 0.0;

            // reactive regions are the large "buttons" located well in front of the display area:
            for (i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                // where is the middle of the display
                centerOfActiveAreaPctTotal = (((double)i + 1.0) * pctWidthOfDisplay) - (0.5 * pctWidthOfDisplay);

                // still using percentage of wall width here 
                areaStartX = centerOfActiveAreaPctTotal - (double)activationAreaW / 2.0;
                areaEndX = areaStartX + (double)activationAreaW;

                // convert to user grid positions
                igridStartX = (int)Math.Floor((double)gridCountWidth * areaStartX);
                igridEndX = (int)Math.Ceiling((double)gridCountWidth * areaEndX);

                // vertical areas are the same for all activation areas:
                igridStartY = (int)Math.Floor((double)gridCountHeight * activationAreaBot);
                igridEndY = (int)Math.Ceiling((double)gridCountHeight * activationAreaTop);

                activationGridXRanges[i, 0] = igridStartX;
                activationGridXRanges[i, 1] = igridEndX;

                activationGridYRanges[i, 0] = igridStartY;
                activationGridYRanges[i, 1] = igridEndY;
            }
        }

        private void initializeCountersAndAreaStatus()
        {
            for (int i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                regionActivatedNear[i] = false;
                regionActivatedFar[i] = false;
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
                sensingRegionFar[n] = false; // false until proven true
                sensingRegionNear[n] = false; // false until proven true
                gridStartX = activationGridXRanges[n, 0];
                gridEndX = activationGridXRanges[n, 1];
                gridStartY = activationGridYRanges[n, 0];
                gridEndY = activationGridYRanges[n, 1];

                regionNearDataCount[n] = 0;
                regionMidDataCount[n] = 0;
                regionFarDataCount[n] = 0;

                for (i = gridStartX; i < gridEndX; ++i) // only check relevant grid areas (no need to check whole wall)
                {
                    for (j = gridStartY; j < gridEndY; ++j)
                    {
                        //if ((depthData[i, j] > 0.0f) && (depthData[i, j] < toggleRangeFar))
                        //if (depthData[i, j] > 0.0f) // remove all null values
                        //{
                            /*
                            if (depthData[i, j] < toggleRangeFar)
                            {
                                regionFarDataCount[n] += 1;
                            }
                            // check for near activation:
                            if (depthData[i, j] < toggleRangeMid) // assumes that mid range is less than far range!
                            {
                                regionMidDataCount[n] += 1;
                            }

                            // check for near activation:
                            if (depthData[i, j] < toggleRangeNear) // assumes that near range is less than mid and far range!
                            {
                                regionNearDataCount[n] += 1;
                            }
                            */

                            // alt method, more efficient
                            if (depthData[i, j] < toggleRangeFar)
                            {
                                regionFarDataCount[n] += 1;
                                if (depthData[i, j] < toggleRangeMid)
                                {
                                    regionMidDataCount[n] += 1;
                                    if (depthData[i, j] < toggleRangeNear)
                                        regionNearDataCount[n] += 1;
                                }
                            }
                        //}
                    }
                }

                if (regionFarDataCount[n] > 0)
                    sensingRegionFar[n] = true;
                else
                    sensingRegionFar[n] = false;

                if (regionMidDataCount[n] > 0)
                    sensingRegionMid[n] = true;
                else
                    sensingRegionMid[n] = false;

                if (regionNearDataCount[n] > 0)
                    sensingRegionNear[n] = true;
                else
                    sensingRegionNear[n] = false;

                // test code here:
                /*
                if (n==1)
                {
                    if (regionFarDataCount[n] > 0)
                        System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] activated far:" + regionFarDataCount[n]);
                }*/
            } // finished running through all of the activation areas




            // compare and test 

            // copy previous data to prev array:
            Array.Copy(regionActivatedNear, prevRegionActivatedNear, regionActivatedNear.Length);
            Array.Copy(regionActivatedMid, prevRegionActivatedMid, regionActivatedMid.Length);
            Array.Copy(regionActivatedFar, prevRegionActivatedFar, regionActivatedFar.Length);

            bool didSendActivateFarEvent = false;
            bool didSendActivateMidEvent = false;
            bool didSendActivateNearEvent = false;

            bool didSendDeactivateFarEvent = false;
            bool didSendDeactivateMidEvent = false;
            bool didSendDeactivateNearEvent = false;


            for (n = 0; n < actualNumberOfReactiveRegions; ++n)
            {
                /************************************************************************/
                /* current status and counters */
                /************************************************************************/
                /*
                if (sensingRegionFar[n]) // this one is hot:
                {
                    doActivateRegionCounterFar[n] += 1;
                    deactivateRegionCounterFar[n] = 0;
                }
                else // no one is there right now...
                {
                    doActivateRegionCounterFar[n] = 0;
                    deactivateRegionCounterFar[n] += 1;
                }

                if (sensingRegionMid[n]) // someone is closer to screen!
                {
                    doActivateRegionCounterMid[n] += 1;
                    deactivateRegionCounterMid[n] = 0;
                }
                else // no one is there right now...
                {
                    doActivateRegionCounterMid[n] = 0;
                    deactivateRegionCounterMid[n] += 1;
                }

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
                */

                // 3/31/20 alt method of culling hits
                if (sensingRegionNear[n])
                {
                    doActivateRegionCounterNear[n] += 1;
                    deactivateRegionCounterNear[n] = 0; //reset deactivate counter
                    doActivateRegionCounterMid[n] += 1;
                    deactivateRegionCounterMid[n] = 0;
                    doActivateRegionCounterFar[n] += 1;
                    deactivateRegionCounterFar[n] = 0;
                }
                else
                {
                    doActivateRegionCounterNear[n] = 0;  //reset activate counter
                    deactivateRegionCounterNear[n] += 1;

                    if (sensingRegionMid[n])
                    {
                        doActivateRegionCounterMid[n] += 1;
                        deactivateRegionCounterMid[n] = 0;
                        doActivateRegionCounterFar[n] += 1;
                        deactivateRegionCounterFar[n] = 0;

                    }
                    else
                    {
                        doActivateRegionCounterMid[n] = 0; 
                        deactivateRegionCounterMid[n] += 1;

                        if (sensingRegionFar[n])
                        {
                            doActivateRegionCounterFar[n] += 1;
                            deactivateRegionCounterFar[n] = 0;
                        }
                        else
                        {
                            doActivateRegionCounterFar[n] = 0;
                            deactivateRegionCounterFar[n] += 1;
                        }
                    }
                }

                /************************************************************************/
                /* activation commands */
                /************************************************************************/
                if (doActivateRegionCounterFar[n] > triggersBeforeActivateFarEvent)
                {
                    if (!regionActivatedFar[n])
                    {
                        didSendActivateFarEvent = checkForSendActivateFarCommand(n);
                        if (didSendActivateFarEvent)
                        {
                            regionActivatedFar[n] = true;
                            doActivateRegionCounterFar[n] = 0;
                        }
                    }
                }

                if (doActivateRegionCounterMid[n] > triggersBeforeActivateMidEvent)
                {
                    if (regionActivatedFar[n]) // only proceed if far region IS activated
                    {
                        if (!regionActivatedMid[n])
                        {
                            didSendActivateMidEvent = checkForSendActivateMidCommand(n);
                            if (didSendActivateMidEvent)
                            {
                                regionActivatedMid[n] = true;
                                doActivateRegionCounterMid[n] = 0;
                            }
                        }
                    }
                }

                if (doActivateRegionCounterNear[n] > triggersBeforeActivateNearEvent)
                {
                    if (regionActivatedFar[n]) // only proceed if far region IS activated
                    {
                        if (regionActivatedMid[n]) // only proceed if mid region IS activated
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

                if (deactivateRegionCounterMid[n] > triggersBeforeHaltMidEvent)
                {
                    if (!regionActivatedNear[n]) // only proceed if near region is NOT activated
                    {
                        if (regionActivatedMid[n])
                        {
                            didSendDeactivateMidEvent = checkForSendDeactivateMidCommand(n);
                            if (didSendDeactivateMidEvent)
                            {
                                regionActivatedMid[n] = false;
                                deactivateRegionCounterMid[n] = 0;
                            }
                        }
                        else
                        {
                            deactivateRegionCounterMid[n] = 0;
                        }
                    }
                }

                if (deactivateRegionCounterFar[n] > triggersBeforeHaltFarEvent)
                {
                    if (!regionActivatedNear[n]) // only proceed if near region is NOT activated
                    {
                        if (!regionActivatedMid[n]) // only proceed if mid region is NOT activated
                        {
                            if (regionActivatedFar[n])
                            {
                                didSendDeactivateFarEvent = checkForSendDeactivateFarCommand(n);
                                if (didSendDeactivateFarEvent)
                                {
                                    regionActivatedFar[n] = false;
                                    deactivateRegionCounterFar[n] = 0;
                                }
                            }
                            else
                            {
                                deactivateRegionCounterFar[n] = 0;
                            }
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

                /************************************************************************/
                /* mark as dirty if state changed */
                /************************************************************************/
                if (regionActivatedNear[n] != prevRegionActivatedNear[n]) // did anything change?
                {
                    dirty = true;
                }
                if (regionActivatedMid[n] != prevRegionActivatedMid[n]) // did anything change?
                {
                    dirty = true;
                }
                if (regionActivatedFar[n] != prevRegionActivatedFar[n]) // did anything change?
                {
                    dirty = true;
                }

            }
        }

        /***************************************/
        #region drawStatusGraphics:

        public void draw(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            GL.UseProgram(handleShader);

            // udpate uniforms in shaders:
            GL.UniformMatrix4(shaderlocModelMatrix, false, ref whichviewMat);
            GL.UniformMatrix4(shaderlocProjMatrix, false, ref whichProjMat);

            Vector4 textureColorOffset = new Vector4(1.0f, 1.0f, 1.0f, 0.5f); // transparent white

            int i;
            bool success = false;

            //for (i = 0; i < actualNumberOfReactiveRegions; ++i)
            for (i = actualNumberOfReactiveRegions-1; i >= 0; --i) // draw from back to front
            {
                
                if (regionActivatedNear[i])
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
                // update color offset:
                GL.Uniform4(shaderlocColor, ref textureColorOffset);
                drawNearRegions[i].drawFill();
                
                if (regionActivatedMid[i])
                {
                    // this is the color of the outline:
                    GL.Color4(0.11f, 0.60f, 0.11f, 0.5f);
                    
                    // this is the color of the fill:
                    //textureColorOffset.X = 0.11f;
                    //textureColorOffset.Y = 0.60f;
                    //textureColorOffset.Z = 0.11f;
                    //textureColorOffset.W = 0.5f;
                    // activated = yellow transp
                    //textureColorOffset.X = 247f / 255f;
                    //textureColorOffset.Y = 1.0f;
                    //textureColorOffset.Z = 25f/255f;
                    //textureColorOffset.W = 0.5f;
                    // try just white:
                    textureColorOffset.X = 1.0f;
                    textureColorOffset.Y = 1.0f;
                    textureColorOffset.Z = 1.0f;
                    textureColorOffset.W = 0.15f;
                }
                else
                {
                    GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);
                    //textureColorOffset.X = 0.6f;
                    //textureColorOffset.Y = 0.11f;
                    //textureColorOffset.Z = 0.11f;
                    //textureColorOffset.W = 0.5f;
                    // deactivated = dark grey, mostly transparent
                    textureColorOffset.X = 0.05f;
                    textureColorOffset.Y = 0.05f;
                    textureColorOffset.Z = 0.05f;
                    textureColorOffset.W = 0.25f;
                }
                // update color offset:
                GL.Uniform4(shaderlocColor, ref textureColorOffset);
                drawMidRegions[i].drawFill();
                
                if (regionActivatedFar[i])
                {
                    GL.Color4(0.11f, 0.60f, 0.11f, 0.5f);
                    //textureColorOffset.X = 0.11f;
                    //textureColorOffset.Y = 0.60f;
                    //textureColorOffset.Z = 0.11f;
                    //textureColorOffset.W = 0.5f;
                    // make it more subtle: (deactivated = dark grey)
                    textureColorOffset.X = 0.11f;
                    textureColorOffset.Y = 0.11f;
                    textureColorOffset.Z = 0.11f;
                    textureColorOffset.W = 0.5f;
                }
                else
                {
                    GL.Color4(0.6f, 0.11f, 0.11f, 0.5f);
                    //textureColorOffset.X = 0.6f;
                    //textureColorOffset.Y = 0.11f;
                    //textureColorOffset.Z = 0.11f;
                    //textureColorOffset.W = 0.5f;
                    // make it more subtle: (deactivated = almost transparent)
                    textureColorOffset.X = 0.05f;
                    textureColorOffset.Y = 0.05f;
                    textureColorOffset.Z = 0.05f;
                    textureColorOffset.W = 0.25f;
                }

                // update color offset:
                GL.Uniform4(shaderlocColor, ref textureColorOffset);
                success = drawFarRegions[i].drawFill();

                // switching to yellow:
                textureColorOffset.X = 0.9f;
                textureColorOffset.Y = 0.9f;
                textureColorOffset.Z = 0.11f;
                textureColorOffset.W = 1.0f;
                GL.Uniform4(shaderlocColor, ref textureColorOffset);

                // outline near targets
                if (sensingRegionFar[i])
                {
                    drawFarRegions[i].drawOutline();
                }
                if (sensingRegionMid[i])
                {
                    drawMidRegions[i].drawOutline();
                }
                if (sensingRegionNear[i])
                {
                    drawNearRegions[i].drawOutline();
                }
            }
            GL.UseProgram(0);
        }

        public void Olddraw(int whichColorAdjustPointer)
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

                if (regionActivatedFar[n])
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
                // update color offset:
                GL.Uniform4(whichColorAdjustPointer, ref textureColorOffset);


                GL.Begin(BeginMode.Quads);


                GL.Vertex3(drawPosnXLeft, drawPosnYBottom, toggleRangeFar);
                GL.Vertex3(drawPosnXRight, drawPosnYBottom, toggleRangeFar);
                GL.Vertex3(drawPosnXRight, drawPosnYTop, toggleRangeFar);
                GL.Vertex3(drawPosnXLeft, drawPosnYTop, toggleRangeFar);

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
                // outline far targets
                if (sensingRegionFar[n])
                {
                    GL.Begin(BeginMode.LineLoop);
                    GL.Vertex3(drawPosnXLeft, drawPosnYBottom, toggleRangeFar);
                    GL.Vertex3(drawPosnXRight, drawPosnYBottom, toggleRangeFar);
                    GL.Vertex3(drawPosnXRight, drawPosnYTop, toggleRangeFar);
                    GL.Vertex3(drawPosnXLeft, drawPosnYTop, toggleRangeFar);
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

        private bool checkForSendActivateMidCommand(int whichRegion)
        {
            if (!sendEventDisabled[whichRegion])
            {
                waitBeforeSendAnotherEventCounter[whichRegion] = 0;
                sendEventDisabled[whichRegion] = true;
                doSendActivateMidCommand(whichRegion);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool checkForSendActivateFarCommand(int whichRegion)
        {
            if (!sendEventDisabled[whichRegion])
            {
                waitBeforeSendAnotherEventCounter[whichRegion] = 0;
                sendEventDisabled[whichRegion] = true;
                doSendActivateFarCommand(whichRegion);
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

        private bool checkForSendDeactivateMidCommand(int whichRegion)
        {
            if (!sendEventDisabled[whichRegion])
            {
                waitBeforeSendAnotherEventCounter[whichRegion] = 0;
                sendEventDisabled[whichRegion] = true;
                doSendDeactivateMidCommand(whichRegion);
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }

        private bool checkForSendDeactivateFarCommand(int whichRegion)
        {
            if (!sendEventDisabled[whichRegion])
            {
                waitBeforeSendAnotherEventCounter[whichRegion] = 0;
                sendEventDisabled[whichRegion] = true;
                doSendDeactivateFarCommand(whichRegion);
                return true;
            }
            else
            {
                return false; // didn't send a command yet...
            }
        }


        private void doSendActivateNearCommand(int whichRegion)
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Near Command #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "activateRegionNear";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendActivateMidCommand(int whichRegion)
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Mid Command #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "activateRegionMid";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendActivateFarCommand(int whichRegion)
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Far Command #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "activateRegionFar";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }


        private void doSendDeactivateNearCommand(int whichRegion)
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] send DE-Activate Near #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "deactivateRegionNear";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendDeactivateMidCommand(int whichRegion)
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] send DE-Activate Mid #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "deactivateRegionMid";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendDeactivateFarCommand(int whichRegion)
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] send DE-Activate Far #" + whichRegion);
            ArrayList argList = new ArrayList();
            argList.Add(whichRegion);

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "deactivateRegionFar";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        #endregion sendEvents
        /***************************************/

        public void exitApp()
        {
            int i;
            GL.DeleteProgram(handleShader);

            for (i = 0; i < actualNumberOfReactiveRegions; ++i)
            {
                if (drawNearRegions[i]!=null)
                    drawNearRegions[i].exitApp();
                if (drawMidRegions[i] != null)
                    drawMidRegions[i].exitApp();
                if (drawFarRegions[i] != null)
                    drawFarRegions[i].exitApp();
            }
        }
    }
}
