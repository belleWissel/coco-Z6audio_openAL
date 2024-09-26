using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using AudioControlApp.Shaders;

namespace AudioControlApp.WallCommunicationsAndControl
{
    class UserSensorGrid
    {
        //private static bool isPrologue = true;
        static int maxNumberOfCameras = 4;

        private static int maxResolution = 255;//max amount of values recorded, 1024 is a bit arbitrary
        private int actualResolutionX = 500;
        private int actualResolutionY = 500;
        private int actualResolutionZ = 255;//amount of depth levels to down sample to
        private int actualElementCount = 50;

        //private float maxDepth = 9999.9f;

        private float[, ,] totalDepthMeasured = new float[maxResolution, maxResolution, maxNumberOfCameras]; // running total of depth for each of 3 cameras
        private float[, ,] numberOfMeasuredSamples = new float[maxResolution, maxResolution, maxNumberOfCameras]; // running total of sample count for each of 3 cameras
        private float[,] avgDepthMeasured = new float[maxResolution, maxResolution]; // avg of all values measured

        private float[,] minDepthMeasured = new float[maxResolution, maxResolution];
        private float[,] prevDepthMeasured = new float[maxResolution, maxResolution];
        //public float[, ,] gridPositions = new float[maxResolution, maxResolution, 2]; // precalculate position of each part of grid 200x200 x and y
        private float[,] smoothedData = new float[maxResolution, maxResolution];
        public float[] smoothedDataLinear = new float[maxResolution * maxResolution]; // linear array for easy sharing
        private float[,] prevSmoothedData = new float[maxResolution, maxResolution];

        private float[,] testActivationData = new float[maxResolution, maxResolution];

        private int[,] convertedDepthData = new int[maxResolution, maxResolution];
        private int maxDepthDataXCount = maxResolution;
        private int maxDepthDataYCount = maxResolution;
        private int[,] prevConvertedDepthData01 = new int[maxResolution, maxResolution];
        private int[,] prevConvertedDepthData02 = new int[maxResolution, maxResolution];
        private float[,] prevAvgDepthMeasured01 = new float[maxResolution, maxResolution];
        private float[,] prevAvgDepthMeasured02 = new float[maxResolution, maxResolution];




        private float[,] deltaData = new float[maxResolution, maxResolution]; // for tracking and filtering noise
        private float[,] prevDeltaData = new float[maxResolution, maxResolution]; // for tracking and filtering noise
        private float[,] smoothedConvertedDepthData = new float[maxResolution, maxResolution]; //for smoothing out the data 

        // *******************************
        // data that will be transmitted:
        // *******************************
        // linear array of smoothed depth values
        private float[] userMeshDepthForTransmit = new float[maxResolution * maxResolution];
        // linear array of which indexes are being sent
        private int[] userMeshIndexes = new int[maxResolution * maxResolution * 2]; // maximum number of possible mesh depth points * 2 values (x and y point)
        // keep track of how many we are transmitting:
        public int validUserMeshPointCounter = 0;



        // *******************************
        // user data mesh dimensions and filters:
        // *******************************
        private float gridWidth = 5000.0f; // 194 inches wide
        private float gridHeight = 2133.0f; // 7 feet high
        private float gridStartX = -2500.0f;
        private float gridStartY = 100.0f; // rigth near floor

        private float gridResolutionW = 100.0f;
        private float gridResolutionH = 100.0f;
        private float gridResolutionDepth = 10.0f;

        private float minTransmitRange = 0.0f;
        private float maxTransmitRange = 5000.0f;

        // *******************************
        // data transmission variables:
        // *******************************
        private bool activateDataSream = false;
        //private bool compressDataPackets = true;
        //private static int maxNumberOfPacketWriters = 4;
        private DepthDataPacketWriter depthDataWriterVar;
        //private DepthDataPacketWriter depthDataWriterVar1;
        public bool connectionStatus = false;
        public int connectionCount = 0;

        public ReactiveAreasControl reactiveAreasVar;
        //public ReactiveAreasControlTwoDimensional reactiveAreas2DVar;
        private int depthTransmissionRate = 33;

        private long lastTransmitTick = 0;//last time something was sent
        // note: rate of transmission is controlled from cameraControl updateDepthData_Tick (Main class: private static int depthDataTransmitRate)
        private int minTransmitInterval = 30;//how often to transmit in ms (60=15fps)

        // *******************************
        // file writer variables:
        // *******************************

        public int[] dataForFileWrite = new int[maxResolution * maxResolution * 3]; //65025 =  255* 255 * 3
        public bool writingDataToFile = false;
        private WriteByteDataToFile fileWriter;
        //private int fileWriteFrameNumber = 0;

        // *******************************
        // convert pixel size vars:
        // *******************************
        private double HFOV = 50.1; // estimate from right angle
        private double VFOV = 40.5; // determined onsite
        private float depthPlaneWidthConstant; // notice float means we are working with transformed points.
        private float depthPlaneHeightConstant; // notice float means we are working with transformed points.


        public bool isDirty = false;

        // *******************************
        // shader variables:
        // *******************************
        shaderFileLoader simpleFlatShaderSource;
        // buffer arrays:
        int positionVboHandle,
            normalVboHandle,
            uvVboHandle,
            indicesVboHandle,
            colorVboHandle;

        int handleShader;

        int vaoID;

        // local shader variables:
        int shaderlocPosition, shaderlocVertColor,
            shaderlocColor, shaderlocOffset,
            shaderlocModelMatrix, shaderlocProjMatrix;

        private bool readyToDraw = false;
        //Vector4 darkGreenColor, brightYellowColor;
        // *******************************


        private Vector3[] positionVboData = new Vector3[maxResolution * maxResolution * 6]; // 3d points of entire grid
        uint[] indicesVboData = new uint[(maxResolution * maxResolution) * 6]; // each point in grid has two triangles
        private Vector4[] planeVertColors = new Vector4[(maxResolution * maxResolution) * 6];

        private int displayMode = 1; // 0 = off, 1 = wire, 2 = solid quads

 


        public UserSensorGrid(int whichHCount, int whichVCount, int whichZCount, bool doActivateDataTransmisssion, int whichTransmissionRate)
        {
            //isPrologue = whichIsPrologue;

            activateDataSream = doActivateDataTransmisssion;
            depthTransmissionRate = whichTransmissionRate;

            actualResolutionX = whichHCount;
            actualResolutionY = whichVCount;
            actualResolutionZ = whichZCount;


            if (actualResolutionX > maxDepthDataXCount)
            {
                actualResolutionX = maxDepthDataXCount;
            }
            if (actualResolutionY > maxDepthDataYCount)
            {
                actualResolutionY = maxDepthDataYCount;
            }

            gridResolutionW = gridWidth / (float)whichHCount;
            gridResolutionH = gridHeight / (float)whichVCount;
            gridResolutionDepth = (maxTransmitRange - minTransmitRange) / (float)actualResolutionZ;

            initDepthData();


            fileWriter = new WriteByteDataToFile();

            depthPlaneWidthConstant = (float)(Math.Tan(HFOV * Math.PI / 180));
            depthPlaneHeightConstant = (float)(Math.Tan(VFOV * Math.PI / 180));

            //videoSensorGridVar = new VideoSensorGrid(whichHCount, whichVCount);
            //if (isPrologue)
            //    reactiveAreas2DVar = new ReactiveAreasControlTwoDimensional(whichHCount, whichVCount, depthTransmissionRate);
            //else
            reactiveAreasVar = new ReactiveAreasControl(whichHCount, whichVCount, depthTransmissionRate);

        }

        public void initDepthTransmit(int whichPort)
        {
            if (activateDataSream)
            {
                // setup to use packet writer only:
                if (depthDataWriterVar == null) // only start if we haven't already
                    depthDataWriterVar = new DepthDataPacketWriter(whichPort);
            }
        }

        public void terminateDepthTransmit()
        {
            if (depthDataWriterVar != null) // only stop if its not started!
                depthDataWriterVar.halt();
        }
        /*
        public void setUserTiming(double whichNearOnTime, double whichFarOnTime, double whichTouchOnTime, double whichNearOffTime, double whichFarOffTime, double whichTouchOffTime, int whichTransmissionRate)
        {

            //videoSensorGridVar.setUserTiming(whichStartAdTime, whichRemoveAdTime, whichAdToVideoTime, whichTouchToVideoTime, whichRemoveVideoTime, whichWaitForSecondAdTime, whichTransmissionRate);
            if (reactiveAreasVar != null)
                reactiveAreasVar.setUserTiming(whichNearOnTime, whichFarOnTime, whichTouchOnTime, whichNearOffTime, whichFarOffTime, whichTouchOffTime, whichTransmissionRate);
        }
        */

        public void resetRegions() // for when state control app starts
        {
            /*if (isPrologue)
            {
                if (reactiveAreas2DVar != null)
                    reactiveAreas2DVar.resetActive(false);
            }
            else
            {*/
                if (reactiveAreasVar != null)
                    reactiveAreasVar.resetActive(false);
            //}
        }

        public void resetSpecificRegion(int whichRegion)
        {
            /*if (isPrologue)
            {
                if (reactiveAreas2DVar != null)
                    reactiveAreas2DVar.resetActiveForRegion(whichRegion);
            }
            else
            {*/
                if (reactiveAreasVar != null)
                    reactiveAreasVar.resetActiveForRegion(whichRegion);
            //}

        }

        public void resetRegionsShort() // for when topic changes
        {
            /*if (isPrologue)
            {
                if (reactiveAreas2DVar != null)
                    reactiveAreas2DVar.resetActive(true);
            }
            else
            {*/
                if (reactiveAreasVar != null)
                    reactiveAreasVar.resetActive(true);
            //}
        }
        
        public void haltingProgram()
        {
            if (activateDataSream)
            {

                if (depthDataWriterVar != null)
                    depthDataWriterVar.halt();
            }
        }


        // ************************************************************
        #region checkData

        public void setDepthRanges(double whichNear, double whichFar) // set this to millimeters
        {
            minTransmitRange = (float)whichNear * 1000.0f;
            maxTransmitRange = (float)whichFar * 1000.0f;

            gridResolutionDepth = (maxTransmitRange - minTransmitRange) / (float)actualResolutionZ;

        }
        /*
        public void setTriggerDistances(double whichNearTrigger, double whichFarTrigger, double whichTouchTrigger) // this is already in millimeters
        {
            if (reactiveAreasVar != null)
            {
                reactiveAreasVar.toggleRangeNear = (float)whichNearTrigger;
                reactiveAreasVar.toggleRangeFar = (float)whichFarTrigger;
                reactiveAreasVar.toggleRangeTouch = (float)whichTouchTrigger;
            }
        }
        */
        /*
        public void setActivationAreaSizes(double whichW, double whichBottom, double whichTop) // this is in percentage of entire wall
        {
            if (reactiveAreasVar != null)
            {
                reactiveAreasVar.setSensorAreaSize(whichW, whichBottom, whichTop);

            }
        }

        public void setButtonAreaSizes(double whichW, double whichBottom, double whichTop) // this is in percentage of entire wall
        {
            if (reactiveAreasVar != null)
            {
                reactiveAreasVar.setButtonAreaSize(whichW, whichBottom, whichTop);

            }
        }
        */


        public void setUserGridMeasurementRanges(double whichWidth, double whichStartY, double whichEndY)
        {
            gridWidth = (float)whichWidth * 1000.0f;
            gridStartX = 0.0f - gridWidth / 2.0f;

            gridStartY = (float)whichStartY * 1000.0f;
            gridHeight = ((float)whichEndY * 1000.0f) - gridStartY;

            // re-evaluate the grid based upon these new measurements
            gridResolutionW = gridWidth / (float)actualResolutionX;
            gridResolutionH = gridHeight / (float)actualResolutionY;

            // re-evaluate the reactive regions:
            //if (isPrologue)
            //    reactiveAreas2DVar.setUserGridMeasurementRanges(whichWidth, whichStartY, whichEndY);
            //else
                reactiveAreasVar.setUserGridMeasurementRanges(whichWidth, whichStartY, whichEndY);
        }

        private void initDepthData()
        {
            int i, j;
            for (i = 0; i < maxResolution; ++i)
            {
                for (j = 0; j < maxResolution; ++j)
                {
                    minDepthMeasured[i, j] = maxTransmitRange;
                    prevDepthMeasured[i, j] = maxTransmitRange;
                    avgDepthMeasured[i, j] = maxTransmitRange;
                    

                }
            }
        }


        public void updatePreviousValues()
        {
            unsafe
            {
                // copy current array into "previous" arrays
                // copy old to older
                Array.Copy(prevAvgDepthMeasured01, prevAvgDepthMeasured02, avgDepthMeasured.Length);

                // copy current to old
                Array.Copy(avgDepthMeasured, prevAvgDepthMeasured01, avgDepthMeasured.Length);

                // store previous delta values to test for noisy/jumpy data
                Array.Copy(deltaData, prevDeltaData, deltaData.Length);

                // store (final) smoothed values to ease new values in...
                Array.Copy(smoothedData, prevSmoothedData, smoothedData.Length);
            } // end of unsafe
        }

        private int returnGridIndexPositionX(float whichX)
        {
            int valueToReturn = 0;

            valueToReturn = (int)Math.Floor((whichX - gridStartX) / gridResolutionW);

            return valueToReturn;
        }

        private int returnGridIndexPositionY(float whichY)
        {
            int valueToReturn = 0;

            valueToReturn = (int)Math.Floor((whichY - gridStartY) / gridResolutionH);

            return valueToReturn;
        }

        public void resetAvgValues()
        {
            int i, j, k;
            unsafe
            {
                for (i = 0; i < actualResolutionX; ++i)
                {
                    for (j = 0; j < actualResolutionY; ++j)
                    {
                        for (k = 0; k < maxNumberOfCameras; ++k)
                        {
                            totalDepthMeasured[i, j, k] = 0.0f;
                            numberOfMeasuredSamples[i, j, k] = 0.0f;
                        }
                    }
                }
            } // end of unsafe
        }

        public void resetMinMeasured()
        {
            int i, j;
            unsafe
            {
                for (i = 0; i < maxResolution; ++i)
                {
                    for (j = 0; j < maxResolution; ++j)
                    {
                        minDepthMeasured[i, j] = maxTransmitRange;
                    }
                }
            } // end unsafe
        }

        public void calculateAverageData()
        {
            int i, j, k;
            float[] camAvg = new float[4];
            float dataAverageOverTime;
            float minAvgValue01 = 0;
            float minAvgValue23 = 0;
            float minAvgValue = 0;

            float noiseLimit = 500.0f; // if data is waffling (frame - to -frame) more than this ammount then strip it out

            int linearArrayIndex = 0;

            validUserMeshPointCounter = 0;

            unsafe
            {
                // now calculate values based upon average at that sample point:
                for (i = 0; i < actualResolutionX; ++i)
                {
                    for (j = 0; j < actualResolutionY; ++j)
                    {
                        /*
                        for (k = 0; k < maxNumberOfCameras; ++k) // determine averages from each of the cameras:
                        {
                            if (numberOfMeasuredSamples[i, j, k] > 0)
                            {
                                camAvg[k] = totalDepthMeasured[i, j, k] / numberOfMeasuredSamples[i, j, k];
                            }
                            else
                            {
                                camAvg[k] = maxTransmitRange; // no data available here (from this camera)
                            }
                        }

                        // TODO: combine data from overlapping cameras?

                        // sorting method 1:
                        if (camAvg[0] < camAvg[1])
                            minAvgValue01 = camAvg[0];
                        else
                            minAvgValue01 = camAvg[1];

                        if (camAvg[2] < camAvg[3])
                            minAvgValue23 = camAvg[2];
                        else
                            minAvgValue23 = camAvg[3];


                        if (minAvgValue01 < minAvgValue23)
                            minAvgValue = minAvgValue01;
                        else
                            minAvgValue = minAvgValue23;
                        

                        avgDepthMeasured[i, j] = minAvgValue; // use this as the average value
                        */

                        // only using camera 0:
                        avgDepthMeasured[i, j] = totalDepthMeasured[i, j, 0] / numberOfMeasuredSamples[i, j, 0];


                        // steps for smoothing and removing noise from data

                        // take average of last 3 samples:
                        dataAverageOverTime = (avgDepthMeasured[i, j] + prevAvgDepthMeasured01[i, j] + prevAvgDepthMeasured02[i, j]) / 3.0f;

                        // determine the delta between the previous smoothed value and the new value:
                        deltaData[i, j] = avgDepthMeasured[i, j] - prevSmoothedData[i, j];

                        if (Math.Abs(avgDepthMeasured[i, j] - dataAverageOverTime) > noiseLimit) // this data is not settling on a value
                        {
                            smoothedData[i, j] = maxTransmitRange;
                        }
                        else // data is stable:
                        {
                            //new value is easing into this new value from the previous value:
                            //smoothedData[i, j] = prevSmoothedData[i, j] + (deltaData[i, j] / 2.0f);
                            // new value does not consider easing:
                            //smoothedData[i, j] = avgDepthMeasured[i, j];
                            // new value does not consider easing:
                            //smoothedData[i, j] = minDepthMeasured[i, j];

                            smoothedData[i, j] = getNearestDepth(minDepthMeasured[i, j]); // minDepthMeasured = this is data which is drawn

                            // now store values which will be tested against activation areas:
                            if ((smoothedData[i, j] > minTransmitRange * 1.05) && (smoothedData[i, j] < maxTransmitRange * 0.95))
                                testActivationData[i, j] = smoothedData[i, j];
                            else
                                testActivationData[i, j] = maxTransmitRange;

                            linearArrayIndex = i * actualResolutionX + j;
                            smoothedDataLinear[linearArrayIndex] = smoothedData[i, j];
                            //smoothedDataLinear[linearArrayIndex] = avgDepthMeasured[i, j];

                            // now store values which will be passed over network:
                            //if ((smoothedDataLinear[linearArrayIndex] > minTransmitRange * 1.05) && (smoothedDataLinear[linearArrayIndex] < maxTransmitRange * 0.95))
                            if ((smoothedDataLinear[linearArrayIndex] > minTransmitRange * 1.01) && (smoothedDataLinear[linearArrayIndex] < maxTransmitRange * 0.99))
                            {
                                userMeshDepthForTransmit[validUserMeshPointCounter] = smoothedDataLinear[linearArrayIndex];
                                userMeshIndexes[(validUserMeshPointCounter * 2)] = i;
                                userMeshIndexes[(validUserMeshPointCounter * 2) + 1] = j;
                                validUserMeshPointCounter += 1;
                            }
                        }


                    }
                }
            } // end unsafe
        }


        // optimized performance: instead of calling with each point, send it entire array of data to test at once:
        public void testPointsOnGrid(Vector3[] arrayOfDepthData, int numberOfPointsToTest, int whichCamera)
        {
            int i;
            float whichX, whichY, whichDepth;
            int gridPosnX, gridPosnY;
            int gridPosnStartX, gridPosnStartY;
            int gridPosnEndX, gridPosnEndY;
            int m, n;

            unsafe
            {
                for (i = 0; i < numberOfPointsToTest; ++i)
                {
                    whichX = arrayOfDepthData[i].X;
                    whichY = arrayOfDepthData[i].Y;
                    whichDepth = arrayOfDepthData[i].Z;


                    gridPosnStartX = returnGridIndexStartPositionX(whichX, whichDepth);
                    gridPosnEndX = returnGridIndexEndPositionX(whichX, whichDepth);
                    gridPosnStartY = returnGridIndexStartPositionY(whichY, whichDepth);
                    gridPosnEndY = returnGridIndexEndPositionY(whichY, whichDepth);
                    //gridPosnX = returnGridIndexPositionX(whichX);
                    //gridPosnY = returnGridIndexPositionY(whichY);
                    //System.Diagnostics.Debug.WriteLine("[ " + gridPosnStartX + ", " + gridPosnEndX + " ] - [ " + gridPosnStartY + ", " + gridPosnEndY + " ] - [ " + gridPosnX + ", " + gridPosnY + " ]");
                    for (m = gridPosnStartX; m < gridPosnEndX; ++m)
                    {
                        if ((m > 0) && (m < actualResolutionX))
                        {
                            for (n = gridPosnStartY; n < gridPosnEndY; ++n)
                            {
                                if ((n > 0) && (n < actualResolutionY))
                                {
                                    totalDepthMeasured[m, n, whichCamera] += whichDepth;
                                    numberOfMeasuredSamples[m, n, whichCamera] += 1.0f;
                                    // also store minDepthMeasured
                                    if (whichDepth < minDepthMeasured[m, n])
                                        minDepthMeasured[m, n] = whichDepth;
                                }
                            }
                        }
                    }
                }
            } // end of unsafe code
        }

        private int returnGridIndexStartPositionX(float whichX, float whichZ)
        {
            int valueToReturn = 0;

            float dataPixelSize = whichZ * depthPlaneWidthConstant / (float)actualResolutionX;

            whichX -= dataPixelSize / 1.1f;
            valueToReturn = (int)Math.Floor((whichX - gridStartX) / gridResolutionW);


            return valueToReturn;
        }

        private int returnGridIndexEndPositionX(float whichX, float whichZ)
        {
            int valueToReturn = 0;
            float dataPixelSize = whichZ * depthPlaneWidthConstant / (float)actualResolutionX;

            whichX += dataPixelSize / 1.1f;
            valueToReturn = (int)Math.Ceiling((whichX - gridStartX) / gridResolutionW);

            return valueToReturn;
        }

        private int returnGridIndexStartPositionY(float whichY, float whichZ)
        {
            int valueToReturn = 0;
            float dataPixelSize = whichZ * depthPlaneHeightConstant / (float)actualResolutionX;

            whichY -= dataPixelSize / 1.1f;
            valueToReturn = (int)Math.Floor((whichY - gridStartY) / gridResolutionH);

            return valueToReturn;
        }

        private int returnGridIndexEndPositionY(float whichY, float whichZ)
        {
            int valueToReturn = 0;
            float dataPixelSize = whichZ * depthPlaneHeightConstant / (float)actualResolutionX;

            whichY += dataPixelSize / 1.1f;
            valueToReturn = (int)Math.Ceiling((whichY - gridStartY) / gridResolutionH);

            return valueToReturn;
        }

        private float getNearestDepth(float whichZ)
        {
            float valueToReturn = 0;

            //float depthRes = (maxTransmitRange - minTransmitRange) / (float)actualResolutionZ;

            float remainder = whichZ % gridResolutionDepth;

            valueToReturn = whichZ - remainder;


            return valueToReturn;
        }

        public bool checkDataAgainstActivationAreas() // uses min depth measured at each point in the user sensor grid
        {
            //videoSensorGridVar.testForUserActivation(minDepthMeasured);
            //videoSensorGridVar.testForUserActivation(avgDepthMeasured);

            //return videoSensorGridVar.isDirty();

            //reactiveAreasVar.testForUserActivation(minDepthMeasured);
            /*if (isPrologue)
            {
                reactiveAreas2DVar.testForUserActivation(testActivationData);
                return reactiveAreas2DVar.isDirty();
            }
            else
            {*/
                reactiveAreasVar.testForUserActivation(testActivationData);
                return reactiveAreasVar.isDirty();
            //}
        }
        #endregion checkData
        // ************************************************************

        // ************************************************************
        #region sendData

        public int getConnectionStatus()
        {
            if (depthDataWriterVar != null)
            {
                connectionStatus = depthDataWriterVar.IsConnected();
                connectionCount = depthDataWriterVar.GetConnectedClientCount();
                return connectionCount;
            }
            else
                return 0;
            /*

            if (whichConnection == 0)
            {
                if (depthDataWriterVar0 != null)
                {
                    // update local status indicator:
                    connectionStatus0 = depthDataWriterVar0.IsConnected();
                    connectionCount0 = depthDataWriterVar0.GetConnectedClientCount();
                    return connectionCount0;
                }
                else return 0;
            }
            else if (whichConnection == 1)
            {
                //if (depthDataWriterVar1 != null)
                //{
                    // update local status indicator:
                //    connectionStatus1 = depthDataWriterVar1.isConnected;
                //    return connectionStatus1;
                //}
                //else return false;
                return 0;
            }
            else return 0;
            */
        }

        public void transmitMeshData()
        {
            
            //connectionStatus = depthDataWriterVar.isConnected;
            //  check against min transmit rate (don't overwhelm connection) 
            // note: rate of transmission is controlled from cameraControl updateDepthData_Tick (Main class: private static int depthDataTransmitRate)
            if (DateTime.UtcNow.Ticks - lastTransmitTick < (TimeSpan.TicksPerMillisecond * minTransmitInterval))
            {
                return;
            }

            // create local arrays which are the exact length needed:
            int localValidUserMeshPointCounter = validUserMeshPointCounter;
            float[] localUserMeshRange = new float[localValidUserMeshPointCounter];
            int[] localUserMeshPosition = new int[localValidUserMeshPointCounter * 2];

            Array.Copy(userMeshDepthForTransmit, localUserMeshRange, localValidUserMeshPointCounter); // raw depth data which is not 0 or max range
            Array.Copy(userMeshIndexes, localUserMeshPosition, localValidUserMeshPointCounter * 2); // address for each  of the depth points sent

            //MainApp.MyWindow.update_fp.Start("array_header");
            //array header
            int tagsize = Encoding.ASCII.GetByteCount("KinectData");
            byte[] header = new byte[tagsize + 4];

            //MainApp.MyWindow.update_fp.Stop();

            //MainApp.MyWindow.update_fp.Start("array_data");
            //array data
            int buffer_size = (header.Length * 4);
            int offset = 0;

            byte[] userMeshRange_data = Utilities.FloatArrayToByteArray(localUserMeshRange, localValidUserMeshPointCounter);
            buffer_size += userMeshRange_data.Length;

            byte[] userMeshPosition_data = Utilities.IntArrayToByteArray(localUserMeshPosition, localValidUserMeshPointCounter * 2);
            buffer_size += userMeshPosition_data.Length;

            //MainApp.MyWindow.update_fp.Stop();

            //MainApp.MyWindow.update_fp.Start("create_byte_package");
            //package that will be sent and compressed
            byte[] byte_data = new byte[buffer_size];

            //MainApp.MyWindow.update_fp.Stop();
            //pack points

            offset = packArray(header, userMeshRange_data, byte_data, offset);
            offset = packArray(header, userMeshPosition_data, byte_data, offset);

            //MainApp.MyWindow.update_fp.Start("compress");

            byte[] buffer = byte_data;
            // data compression used to be here.. (determined to be source of instability)

            //MainApp.MyWindow.update_fp.Stop();

            //MainApp.MyWindow.update_fp.Start("create_compressed_package");
            tagsize = Encoding.ASCII.GetByteCount("KinectData");
            header = new byte[tagsize + (sizeof(int) * 2)];

            byte[] package = new byte[buffer.Length + header.Length];
            //OpenTK.Graphics.OpenGL.Buffer vs System.Buffer...
            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("KinectData"), 0, header, 0, tagsize);
            System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, header, tagsize, sizeof(int));
            System.Buffer.BlockCopy(BitConverter.GetBytes(byte_data.Length), 0, header, tagsize + 4, sizeof(int));

            System.Buffer.BlockCopy(header, 0, package, 0, header.Length);
            System.Buffer.BlockCopy(buffer, 0, package, header.Length, buffer.Length);

            //MainApp.MyWindow.update_fp.Stop();

            //System.Diagnostics.Debug.WriteLine("packages compressed:" + package.Length + " uncompressed:" + byte_data.Length + " approx ratio:" + (package.Length / (byte_data.Length * 1.0f)) + " pointPositions:" + (meshPointIndexCounter * 3) + " pointUVs:" + (meshPointIndexCounter * 2) + " whichMeshIndexList:" + meshIndexListCounter);

            //MainApp.MyWindow.update_fp.Start("send_mesh_data");

 
                if (depthDataWriterVar != null)
                {
                    depthDataWriterVar.SendMeshData(package);
                    //connectionStatus = ((DepthDataPacketWriter)depthDataWriterVar).isConnected;
                    //connectionStatus = depthDataWriterVar.isConnected;
                }
            
            /*
            if (depthDataWriterVar1 != null)
            {
                depthDataWriterVar1.SendMeshData(package);
                //connectionStatus = ((DepthDataPacketWriter)depthDataWriterVar).isConnected;
                //connectionStatus = depthDataWriterVar.isConnected;
            }
            */
            //MainApp.MyWindow.update_fp.Stop();


            //MainApp.MyWindow.update_fp.Start("write_data_to_file");
            if (writingDataToFile) fileWriter.writeDataToFile(package);
            //MainApp.MyWindow.update_fp.Stop();

            lastTransmitTick = DateTime.UtcNow.Ticks;


        }

        private int packArray(byte[] header, byte[] source, byte[] buffer, int offset)
        {
            //do it again with low whichMeshIndexList
            int tagsize = Encoding.ASCII.GetByteCount("KinectData");
            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("KinectData"), 0, header, 0, tagsize);
            System.Buffer.BlockCopy(BitConverter.GetBytes(source.Length), 0, header, tagsize, sizeof(int));
            //copy header into package
            System.Buffer.BlockCopy(header, 0, buffer, offset, header.Length);
            offset += header.Length;
            //copy data into package
            System.Buffer.BlockCopy(source, 0, buffer, offset, source.Length);
            offset += source.Length;

            return offset;

        }

        #endregion sendData
        // ************************************************************

        // ************************************************************
        #region drawData

        public void draw(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (displayMode == 0)
                return;
            if (!readyToDraw)
                return;

            bool drawWireframe = false;

            if (displayMode == 2)
                drawWireframe = false;

            Vector4 textureColorOffset = new Vector4(1.0f, 1.0f, 1.0f, 0.5f); // transparent white
            Vector3 PositionOffset = new Vector3(0.0f, 0.0f, 0.0f); // no offset

            //GL.Enable(EnableCap.DepthTest); // force to top
            GL.Enable(EnableCap.Blend);
            // old openTK: GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            // new openTK:
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.UseProgram(handleShader);

            // udpate uniforms in shaders:
            GL.UniformMatrix4(shaderlocModelMatrix, false, ref whichviewMat);
            GL.UniformMatrix4(shaderlocProjMatrix, false, ref whichProjMat);

            // update attributes in shaders:

            GL.Uniform4(shaderlocColor, ref textureColorOffset);
            GL.Uniform3(shaderlocOffset, ref PositionOffset);

            // ******************************************
            // draw VAO as triangles:
            if (vaoID == 0)
                return;
            GL.BindVertexArray(vaoID);
            
            /*
            if (drawWireframe)
            {
                GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
            }
            else
            { */
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //} 
            

            GL.DrawElements(BeginMode.Triangles, actualElementCount,
                DrawElementsType.UnsignedInt, IntPtr.Zero);
            
            
            GL.BindVertexArray(0);            
            // ******************************************


            GL.UseProgram(0);

            // have their own vao's, and their own shader 
            reactiveAreasVar.draw(whichviewMat, whichProjMat);

        }

        #endregion drawData
        // ************************************************************

        public void initOpenGL()
        {
            bool doUsePerVertColor = false;
            // ****************************************************************************
            // create object buffers
            createPositionBuffer();
            if (doUsePerVertColor)
                createColorBuffer();
            createIndexBuffer();

            // ****************************************************************************
            // load shader source
            simpleFlatShaderSource = new shaderFileLoader();
            if (doUsePerVertColor)
                simpleFlatShaderSource.loadShaders("shaders\\PerVertexColor.vp", "shaders\\PerVertexColor.fp");
            else
                simpleFlatShaderSource.loadShaders("shaders\\FlatShader2.vp", "shaders\\FlatShader2.fp");

            // ****************************************************************************
            // create shader app
            handleShader = ShaderLoader.CreateProgram(simpleFlatShaderSource.vertexShaderSource,
                                                        simpleFlatShaderSource.fragmentShaderSource);

            GL.UseProgram(handleShader);

            // ****************************************************************************
            // retreive shader locations
            
            // uniforms:
            shaderlocPosition = GL.GetAttribLocation(handleShader, "vPosition");
            if (doUsePerVertColor) 
                shaderlocVertColor = GL.GetAttribLocation(handleShader, "vColor");


            //attributes
            shaderlocModelMatrix = GL.GetUniformLocation(handleShader, "mModelMatrix");
            shaderlocProjMatrix = GL.GetUniformLocation(handleShader, "mProjectionMatrix");
            shaderlocColor = GL.GetUniformLocation(handleShader, "vColorValue");
            shaderlocOffset = GL.GetUniformLocation(handleShader, "vPositionOffset");

            // ****************************************************************************
            // create VAO
            if (doUsePerVertColor) 
                createVAO2(handleShader, "vPosition", "", "", "vColor");
            else
                createVAO2(handleShader, "vPosition", "", "", "");

            GL.UseProgram(0);

            reactiveAreasVar.initOpenGL(); // this has its own shader, VAO
        }


        // ***********************************
        #region create and update VBOs
        // ***********************************

        private void createPositionBuffer()
        {
            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StreamDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        private void updatePositionBuffer()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        
        private void createColorBuffer()
        {
            GL.GenBuffers(1, out colorVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorVboHandle);
            GL.BufferData<OpenTK.Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(planeVertColors.Length * OpenTK.Vector4.SizeInBytes),
                planeVertColors, BufferUsageHint.StreamDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void updateColorBuffer()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorVboHandle);
            GL.BufferData<OpenTK.Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(planeVertColors.Length * OpenTK.Vector4.SizeInBytes),
                planeVertColors, BufferUsageHint.StreamDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        

        private void createIndexBuffer()
        {
            GL.GenBuffers(1, out indicesVboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                  new IntPtr(indicesVboData.Length * sizeof(uint)),
                  indicesVboData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        private void updateIndexBuffer()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                  new IntPtr(indicesVboData.Length * sizeof(uint)),
                  indicesVboData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }
        /*
        public void createVAO(int whichShaderHandle, string whichVertPosVarName, string whichNormVarName, string whichUVVarName)
        {

            GL.GenVertexArrays(1, out vaoID);
            GL.BindVertexArray(vaoID);

            int arrayIndexCounter = 0;

            if (whichVertPosVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichVertPosVarName);
                arrayIndexCounter += 1;
            }

            if (whichNormVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichNormVarName);
                arrayIndexCounter += 1;
            }

            if (whichUVVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 2, VertexAttribPointerType.Float, false, OpenTK.Vector2.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichUVVarName);
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);


            // clear for new VAO (outline of shape)
            GL.BindVertexArray(0);



            readyToDraw = true;
        }
        */

        public void createVAO2(int whichShaderHandle, string whichVertPosVarName, string whichNormVarName, string whichUVVarName, string whichVertColorName)
        {

            GL.GenVertexArrays(1, out vaoID);
            GL.BindVertexArray(vaoID);

            int arrayIndexCounter = 0;

            if (whichVertPosVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichVertPosVarName);
                arrayIndexCounter += 1;
            }

            if (whichNormVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichNormVarName);
                arrayIndexCounter += 1;
            }

            if (whichUVVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 2, VertexAttribPointerType.Float, false, OpenTK.Vector2.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichUVVarName);
                arrayIndexCounter += 1;
            }

            if (whichVertColorName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, colorVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 4, VertexAttribPointerType.Float, false, OpenTK.Vector4.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichVertColorName);
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);


            // clear for new VAO (outline of shape)
            GL.BindVertexArray(0);



            readyToDraw = true;
        }
        /*
        private void createVAO()
        {
            GL.GenVertexArrays(1, out handleVAO);
            GL.BindVertexArray(handleVAO);

            int arrayIndexCounter = 0;

            // set positions to vPosition:
            GL.BindBuffer(BufferTarget.ArrayBuffer, handlePosnVBO);
            GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(arrayIndexCounter);
            GL.BindAttribLocation(handleShader, arrayIndexCounter, "vPosition");

            arrayIndexCounter += 1;
            /*
            // set colors to vColorValue:
            GL.BindBuffer(BufferTarget.ArrayBuffer, handleColorVBO);
            GL.VertexAttribPointer(arrayIndexCounter, 4, VertexAttribPointerType.Float, false, OpenTK.Vector4.SizeInBytes, 0);
            GL.EnableVertexAttribArray(arrayIndexCounter);
            GL.BindAttribLocation(handleShader, arrayIndexCounter, "vColor");

            arrayIndexCounter += 1;
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handleIndicesVBO);

            // clear for new VAO:
            GL.BindVertexArray(0);
        }*/

        public bool doUpdateGraphicsOnNextPass = false;

        public void updateDrawGeometry()
        {
            updateGridVao();
        }

        private void updateGridVao()
        {

            float depthTestValue;
            float gridPosnX;
            float gridPosnY;
            int indexCounter = 0;

            float greyValue = 0.0f;
            float alphaValue = 1.0f;

            int i, j;

            unsafe
            {
                for (i = 0; i < actualResolutionX; ++i)
                {
                    for (j = 0; j < actualResolutionY; ++j)
                    {
                        depthTestValue = minDepthMeasured[i, j];
                        if ((minDepthMeasured[i, j] < maxTransmitRange) && (minDepthMeasured[i, j] > minTransmitRange))
                        {

                            greyValue = 1f - (depthTestValue - minTransmitRange) / (maxTransmitRange - minTransmitRange);
                            alphaValue = greyValue * 3f;

                            if (greyValue < 0.45f) // don't let it get too dark (on black background)
                            {
                                greyValue = 0.45f;
                            }



                            gridPosnX = ((float)i * gridResolutionW) + gridStartX;
                            gridPosnY = ((float)j * gridResolutionH) + gridStartY;


                            // triangle number 1:
                            indicesVboData[indexCounter] = (uint)indexCounter;
                            positionVboData[indexCounter].X = gridPosnX;
                            positionVboData[indexCounter].Y = gridPosnY;
                            positionVboData[indexCounter].Z = depthTestValue;

                            planeVertColors[indexCounter].X = greyValue;
                            planeVertColors[indexCounter].Y = greyValue;
                            planeVertColors[indexCounter].Z = greyValue;
                            planeVertColors[indexCounter].W = alphaValue;

                            indexCounter += 1;

                            indicesVboData[indexCounter] = (uint)indexCounter;
                            positionVboData[indexCounter].X = gridPosnX + gridResolutionW;
                            positionVboData[indexCounter].Y = gridPosnY;
                            positionVboData[indexCounter].Z = depthTestValue;
                            planeVertColors[indexCounter].X = greyValue;
                            planeVertColors[indexCounter].Y = greyValue;
                            planeVertColors[indexCounter].Z = greyValue;
                            planeVertColors[indexCounter].W = alphaValue;
                            indexCounter += 1;

                            indicesVboData[indexCounter] = (uint)indexCounter;
                            positionVboData[indexCounter].X = gridPosnX;
                            positionVboData[indexCounter].Y = gridPosnY + gridResolutionH;
                            positionVboData[indexCounter].Z = depthTestValue;
                            planeVertColors[indexCounter].X = greyValue;
                            planeVertColors[indexCounter].Y = greyValue;
                            planeVertColors[indexCounter].Z = greyValue;
                            planeVertColors[indexCounter].W = alphaValue;
                            indexCounter += 1;

                            //tiangle number 2:
                            indicesVboData[indexCounter] = (uint)indexCounter;
                            positionVboData[indexCounter].X = gridPosnX + gridResolutionW;
                            positionVboData[indexCounter].Y = gridPosnY;
                            positionVboData[indexCounter].Z = depthTestValue;
                            planeVertColors[indexCounter].X = greyValue;
                            planeVertColors[indexCounter].Y = greyValue;
                            planeVertColors[indexCounter].Z = greyValue;
                            planeVertColors[indexCounter].W = alphaValue;
                            indexCounter += 1;

                            indicesVboData[indexCounter] = (uint)indexCounter;
                            positionVboData[indexCounter].X = gridPosnX + gridResolutionW;
                            positionVboData[indexCounter].Y = gridPosnY + gridResolutionH;
                            positionVboData[indexCounter].Z = depthTestValue;
                            planeVertColors[indexCounter].X = greyValue;
                            planeVertColors[indexCounter].Y = greyValue;
                            planeVertColors[indexCounter].Z = greyValue;
                            planeVertColors[indexCounter].W = alphaValue;
                            indexCounter += 1;

                            indicesVboData[indexCounter] = (uint)indexCounter;
                            positionVboData[indexCounter].X = gridPosnX;
                            positionVboData[indexCounter].Y = gridPosnY + gridResolutionH;
                            positionVboData[indexCounter].Z = depthTestValue;
                            planeVertColors[indexCounter].X = greyValue;
                            planeVertColors[indexCounter].Y = greyValue;
                            planeVertColors[indexCounter].Z = greyValue;
                            planeVertColors[indexCounter].W = alphaValue;
                            indexCounter += 1;
                        }

                    }
                }
            } // end unsafe code

            actualElementCount = indexCounter;
            updatePositionBuffer();
            updateColorBuffer();
            updateIndexBuffer();
        }

        // ***********************************
        #endregion create and update VBOs
        // ***********************************

        public void onClosing()
        {
            GL.DeleteProgram(handleShader);
            GL.DeleteVertexArrays(1, ref vaoID);
            reactiveAreasVar.exitApp();
        }

    }
}
