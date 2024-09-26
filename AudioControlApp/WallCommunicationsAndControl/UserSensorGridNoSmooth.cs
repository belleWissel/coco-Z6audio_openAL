using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using AudioControlApp.Shaders;

namespace AudioControlApp.WallCommunicationsAndControl
{
    public class UserSensorGridNoSmooth
    {
        public bool doEnableLocalFileInterrop = true;
        private UserSensorGridRecordAndPlayback playAndRecordDataFromFileVar;

        static int maxNumberOfCameras = 4;

        private static int maxResolution = 255;//max amount of values recorded, 1024 is a bit arbitrary
        private int actualResolutionX = 500;
        private int actualResolutionY = 500;
        private int actualResolutionZ = 255;//amount of depth levels to down sample to
        private int actualElementCount = 50;


        private float[,] minDepthMeasured = new float[maxResolution, maxResolution];
        private float[,] minDepthMeasuredDefaultValues = new float[maxResolution, maxResolution]; // always points to max values
        private float[,] testActivationData = new float[maxResolution, maxResolution];

        private int[,] convertedDepthData = new int[maxResolution, maxResolution];
        private int maxDepthDataXCount = maxResolution;
        private int maxDepthDataYCount = maxResolution;

        // *******************************
        // data that will be transmitted:
        // *******************************
        // linear array of smoothed edge values
        private float[] userEdgeDepthForTransmit = new float[maxResolution * maxResolution];
        // linear array of which indexes are being sent
        private int[] userEdgeIndexes = new int[maxResolution * maxResolution * 2]; // maximum number of possible mesh depth points * 2 values (x and y point)
        // keep track of how many we are transmitting:
        public int validUserEdgePointCounter = 0;
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
        int shaderlocPosition, shaderlocVertVaryColor,
            shaderlocColor, shaderlocOffset,
            shaderlocModelMatrix, shaderlocProjMatrix;

        private bool readyToDraw = false;
        //Vector4 darkGreenColor, brightYellowColor;
        // *******************************


        private Vector3[] positionVboData = new Vector3[maxResolution * maxResolution * 6]; // 3d points of entire grid
        uint[] indicesVboData = new uint[(maxResolution * maxResolution) * 6]; // each point in grid has two triangles
        private Vector4[] planeVertColors = new Vector4[(maxResolution * maxResolution) * 6];

        private int displayMode = 1; // 0 = off, 1 = wire, 2 = solid quads

        // *******************************
        // EDGE FINDING
        // *******************************

        private bool[,] activatedArea = new bool[maxResolution, maxResolution];
        private bool[,] activatedAreaDefaultValues = new bool[maxResolution, maxResolution];
        private int activatedAreaSubSample = 4; //MUST BE EVEN! this value gives the checkered pattern of fill 
        private bool[,] isEdge = new bool[maxResolution, maxResolution];
        private bool[,] wasEdge = new bool[maxResolution, maxResolution];
        private bool[,] wasEdgeBeforeThat = new bool[maxResolution, maxResolution];

        private bool doDrawEdges = true;

        public UserSensorGridNoSmooth(int whichHCount, int whichVCount, int whichZCount, bool doActivateDataTransmisssion, int whichTransmissionRate, bool doUseLocalFile, bool doRecordToFile, bool doPlayFromFile, string whichDataFileName)
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

            reactiveAreasVar = new ReactiveAreasControl(whichHCount, whichVCount, depthTransmissionRate);
            doEnableLocalFileInterrop = doUseLocalFile;

            if (doEnableLocalFileInterrop)
            {
                playAndRecordDataFromFileVar = new UserSensorGridRecordAndPlayback(actualResolutionX, actualResolutionY, actualResolutionZ, whichTransmissionRate, doRecordToFile, doPlayFromFile, whichDataFileName);
            }
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

        public void resetRegions() // for when state control app starts
        {
            if (reactiveAreasVar != null)
                reactiveAreasVar.resetActive(false);
        }

        public void resetSpecificRegion(int whichRegion)
        {
            if (reactiveAreasVar != null)
                reactiveAreasVar.resetActiveForRegion(whichRegion);
        }

        public void resetRegionsShort() // for when topic changes
        {
            if (reactiveAreasVar != null)
                reactiveAreasVar.resetActive(true);
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

        public void setDepthRanges(double whichNear, double whichFar) //dance floor depth in mm
        {
            minTransmitRange = (float)whichNear;
            maxTransmitRange = (float)whichFar;

            gridResolutionDepth = (maxTransmitRange - minTransmitRange) / (float)actualResolutionZ;
            if (doEnableLocalFileInterrop)
                playAndRecordDataFromFileVar.updateMeasurementAreaDepth(minTransmitRange, maxTransmitRange);

        }

        public void setUserGridMeasurementRanges(double whichWidth, double whichStartY, double whichEndY) // dance floor width and height in mm
        {
            gridWidth = (float)whichWidth;
            gridStartX = 0.0f - gridWidth / 2.0f;

            gridStartY = (float)whichStartY;
            gridHeight = (float)whichEndY - gridStartY;

            // re-evaluate the grid based upon these new measurements
            gridResolutionW = gridWidth / (float)actualResolutionX;
            gridResolutionH = gridHeight / (float)actualResolutionY;

            reactiveAreasVar.setUserGridMeasurementRanges(whichWidth, whichStartY, whichEndY);
            if (doEnableLocalFileInterrop)
                playAndRecordDataFromFileVar.updateMeasurementAreaSize(gridWidth, gridStartY, gridHeight);

        }

        private void initDepthData()
        {
            int i, j;
            for (i = 0; i < maxResolution; ++i)
            {
                for (j = 0; j < maxResolution; ++j)
                {
                    minDepthMeasured[i, j] = maxTransmitRange;
                    minDepthMeasuredDefaultValues[i, j] = maxTransmitRange;
                    activatedAreaDefaultValues[i,j]= false;
                    activatedArea[i,j]= false;
                    //prevDepthMeasured[i, j] = maxTransmitRange;
                    //avgDepthMeasured[i, j] = maxTransmitRange;
                }
            }
        }

        public void resetMinMeasured()
        {
            Array.Copy(wasEdge, wasEdgeBeforeThat, activatedAreaDefaultValues.Length);
            Array.Copy(isEdge, wasEdge, activatedAreaDefaultValues.Length);

            Array.Copy(minDepthMeasuredDefaultValues, minDepthMeasured, minDepthMeasuredDefaultValues.Length);
            Array.Copy(activatedAreaDefaultValues, activatedArea, activatedAreaDefaultValues.Length);
            Array.Copy(activatedAreaDefaultValues, isEdge, activatedAreaDefaultValues.Length);
        }

        private void resetTestActivationArea()
        {
            Array.Copy(minDepthMeasuredDefaultValues, testActivationData, activatedAreaDefaultValues.Length);
        }

        public void getDataFromRecordedFile()
        {
            int i, j;
            if (playAndRecordDataFromFileVar.dataReadFromFileSuccessfully)
            {
                resetTestActivationArea();
                validUserMeshPointCounter = 0;
                validUserEdgePointCounter = 0;

                float[] edgeDepthData = playAndRecordDataFromFileVar.getNextEdgeData();
                int[] edgeIndeces = playAndRecordDataFromFileVar.getNextEdgeIndexData();
                float[] fillDepthData = playAndRecordDataFromFileVar.getNextMeshData();
                int[] meshIndeces = playAndRecordDataFromFileVar.getNextMeshIndexData();

                int index1, index2;

                unsafe
                {
                    for (i = 0; i < edgeDepthData.Length; ++i)
                    {
                        index1 = edgeIndeces[i * 2];
                        index2 = edgeIndeces[(i * 2) + 1];
                        activatedArea[index1, index2] = true;
                        isEdge[index1, index2] = true;
                        testActivationData[index1, index2] = edgeDepthData[i];
                        minDepthMeasured[index1, index2] = edgeDepthData[i];
                    }
                    // copy into data for transmitting over network
                    Array.Copy(edgeDepthData, userEdgeDepthForTransmit, edgeDepthData.Length);
                    Array.Copy(edgeIndeces, userEdgeIndexes, edgeDepthData.Length * 2);
                    validUserEdgePointCounter = edgeDepthData.Length;

                    for (i = 0; i < fillDepthData.Length; ++i)
                    {
                        index1 = meshIndeces[i * 2];
                        index2 = meshIndeces[(i * 2) + 1];
                        activatedArea[index1, index2] = true;
                        isEdge[index1, index2] = false;
                        testActivationData[index1, index2] = fillDepthData[i];
                        minDepthMeasured[index1, index2] = fillDepthData[i];
                    }
                    // copy into data for transmitting over network
                    Array.Copy(fillDepthData, userMeshDepthForTransmit, fillDepthData.Length);
                    Array.Copy(meshIndeces, userMeshIndexes, fillDepthData.Length * 2);
                    validUserMeshPointCounter = fillDepthData.Length;
                }
                playAndRecordDataFromFileVar.postDataRetreivedStep(); // advances to Next frame
            }
        }
        public void prepUserGridDataForTransmission() // bad name: this is a filter which narrows down the data which is sent over the network 
        {
            int i, j;

            validUserMeshPointCounter = 0;
            validUserEdgePointCounter = 0;

            float depthValueToTest = 0.0f;
            float zDepthDeltaForEdge = (maxTransmitRange - minTransmitRange) / 10f;

            unsafe
            {
                resetTestActivationArea();
                // now calculate values based upon average at that sample point:
                for (i = 0; i < actualResolutionX; ++i)
                {
                    for (j = 0; j < actualResolutionY; ++j)
                    {
                        if (activatedArea[i, j])
                        {
                            isEdge[i, j] = false;
                            if (i == 0)
                                isEdge[i, j] = false;
                            else if (i == actualResolutionX - 1)
                                isEdge[i, j] = false;
                            else if (j == 0)
                                isEdge[i, j] = false;
                            else if (j == actualResolutionY - 1)
                                isEdge[i, j] = false;
                            else
                            {

                                if (Math.Abs(minDepthMeasured[i, j] - minDepthMeasured[i - 1, j]) > zDepthDeltaForEdge)
                                {
                                    isEdge[i, j] = true;
                                }
                                else if (Math.Abs(minDepthMeasured[i, j] - minDepthMeasured[i + 1, j]) > zDepthDeltaForEdge)
                                {
                                    isEdge[i, j] = true;
                                }
                                else if (Math.Abs(minDepthMeasured[i, j] - minDepthMeasured[i, j - 1]) > zDepthDeltaForEdge)
                                {
                                    isEdge[i, j] = true;
                                }
                                else if (Math.Abs(minDepthMeasured[i, j] - minDepthMeasured[i, j + 1]) > zDepthDeltaForEdge)
                                {
                                    isEdge[i, j] = true;
                                }
                                else if (!activatedArea[i - 1, j])
                                    isEdge[i, j] = true;
                                else if (!activatedArea[i, j + 1])
                                    isEdge[i, j] = true;
                                else if (!activatedArea[i + 1, j])
                                    isEdge[i, j] = true;
                                else if (!activatedArea[i, j - 1])
                                    isEdge[i, j] = true;
                            }
                        }

                        if (isEdge[i, j])
                        {
                            //if (wasEdge[i, j])
                            //{
                                //if (wasEdgeBeforeThat[i, j])
                                //{
                                depthValueToTest = minDepthMeasured[i, j];
                                // now store values which will be tested against activation areas:
                                testActivationData[i, j] = depthValueToTest;
                                // now store values which will be passed over network:

                                userEdgeDepthForTransmit[validUserEdgePointCounter] = depthValueToTest;
                                userEdgeIndexes[(validUserEdgePointCounter * 2)] = i;
                                userEdgeIndexes[(validUserEdgePointCounter * 2) + 1] = j;
                                validUserEdgePointCounter += 1;
                                //}
                            //}
                        }
                        //else
                        //{
                            if (activatedArea[i, j])
                            {
                                if ((((i % activatedAreaSubSample) == activatedAreaSubSample / 2) && ((j % activatedAreaSubSample) == 0)) || (((i % activatedAreaSubSample) == 0) && ((j % activatedAreaSubSample) == activatedAreaSubSample/2)))
                                {
                                    depthValueToTest = minDepthMeasured[i, j];
                                    userMeshDepthForTransmit[validUserMeshPointCounter] = depthValueToTest;
                                    userMeshIndexes[(validUserMeshPointCounter * 2)] = i;
                                    userMeshIndexes[(validUserMeshPointCounter * 2) + 1] = j;
                                    validUserMeshPointCounter += 1;
                                }
                            }
                        //}
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
                                    // for averaging
                                    //totalDepthMeasured[m, n, whichCamera] += whichDepth;
                                    //numberOfMeasuredSamples[m, n, whichCamera] += 1.0f;
                                    // also store minDepthMeasured
                                    if (whichDepth > minTransmitRange)
                                    {
                                        if (whichDepth < minDepthMeasured[m, n])
                                        {
                                            minDepthMeasured[m, n] = whichDepth;
                                            if (whichDepth < maxTransmitRange)
                                                activatedArea[m, n] = true;
                                        }
                                    }
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
            float dataPixelSize = whichZ * depthPlaneHeightConstant / (float)actualResolutionY;

            whichY -= dataPixelSize / 1.1f;
            valueToReturn = (int)Math.Floor((whichY - gridStartY) / gridResolutionH);

            return valueToReturn;
        }

        private int returnGridIndexEndPositionY(float whichY, float whichZ)
        {
            int valueToReturn = 0;
            float dataPixelSize = whichZ * depthPlaneHeightConstant / (float)actualResolutionY;

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

            reactiveAreasVar.testForUserActivation(testActivationData);
            return reactiveAreasVar.isDirty();

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
        }

        public int transmitMeshData()
        {

            //connectionStatus = depthDataWriterVar.isConnected;
            //  check against min transmit rate (don't overwhelm connection) 
            // note: rate of transmission is controlled from cameraControl updateDepthData_Tick (Main class: private static int depthDataTransmitRate)
            /*if (DateTime.UtcNow.Ticks - lastTransmitTick < (TimeSpan.TicksPerMillisecond * minTransmitInterval))
            {
                return;
            }*/

            // create local arrays which are the exact length needed:
            int localValidUserMeshPointCounter = validUserMeshPointCounter;
            float[] localUserMeshRange = new float[localValidUserMeshPointCounter];
            int[] localUserMeshPosition = new int[localValidUserMeshPointCounter * 2];

            Array.Copy(userMeshDepthForTransmit, localUserMeshRange, localValidUserMeshPointCounter); // raw depth data which is not 0 or max range
            Array.Copy(userMeshIndexes, localUserMeshPosition, localValidUserMeshPointCounter * 2); // address for each  of the depth points sent
 
            int localValidUserEdgePointCounter = validUserEdgePointCounter;
            float[] localUserEdgeRange = new float[localValidUserEdgePointCounter];
            int[] localUserEdgePosition = new int[localValidUserEdgePointCounter * 2];

            Array.Copy(userEdgeDepthForTransmit, localUserEdgeRange, localValidUserEdgePointCounter); // raw depth data which is not 0 or max range
            Array.Copy(userEdgeIndexes, localUserEdgePosition, localValidUserEdgePointCounter * 2); // address for each  of the depth points sent

            //MainApp.MyWindow.update_fp.Start("array_header");
            if (doEnableLocalFileInterrop)
            {
                if (playAndRecordDataFromFileVar.isRecording)
                {
                    playAndRecordDataFromFileVar.recordFrame(userEdgeDepthForTransmit, userEdgeIndexes, localValidUserEdgePointCounter, userMeshDepthForTransmit, userMeshIndexes, localValidUserMeshPointCounter);
                }
            }
            //array header interspersed with data:
            int tagInbetweenData = Encoding.ASCII.GetByteCount("kd");
            byte[] headerInbetweenData = new byte[tagInbetweenData + 4];

            //MainApp.MyWindow.update_fp.Stop();
            //MainApp.MyWindow.update_fp.Start("array_data");

            //array data
            int buffer_size = 0;
            int offset = 0;

            buffer_size += headerInbetweenData.Length * 4;

            byte[] userMeshRange_data = Utilities.FloatArrayToByteArray(localUserMeshRange, localValidUserMeshPointCounter);
            buffer_size += userMeshRange_data.Length;

            byte[] userMeshPosition_data = Utilities.IntArrayToByteArray(localUserMeshPosition, localValidUserMeshPointCounter * 2);
            buffer_size += userMeshPosition_data.Length;

            byte[] userEdgeRange_data = Utilities.FloatArrayToByteArray(localUserEdgeRange, localValidUserEdgePointCounter);
            buffer_size += userEdgeRange_data.Length;

            byte[] userEdgePosition_data = Utilities.IntArrayToByteArray(localUserEdgePosition, localValidUserEdgePointCounter * 2);
            buffer_size += userEdgePosition_data.Length;

            //MainApp.MyWindow.update_fp.Stop();
            //MainApp.MyWindow.update_fp.Start("create_byte_package");

            //package that will be sent and compressed
            byte[] byte_data = new byte[buffer_size];

            //MainApp.MyWindow.update_fp.Stop();

            //pack points
            offset = packArray(headerInbetweenData, userMeshRange_data, byte_data, offset);
            offset = packArray(headerInbetweenData, userMeshPosition_data, byte_data, offset);
            offset = packArray(headerInbetweenData, userEdgeRange_data, byte_data, offset);
            offset = packArray(headerInbetweenData, userEdgePosition_data, byte_data, offset);

            //MainApp.MyWindow.update_fp.Start("compress");

            byte[] localDataBuffer = byte_data;
            // data compression used to be here.. (determined to be source of instability)

            //MainApp.MyWindow.update_fp.Stop();
            //MainApp.MyWindow.update_fp.Start("create_compressed_package");

            int tagForWholePackage = Encoding.ASCII.GetByteCount("np");
            byte[] headerForWholePackage = new byte[tagForWholePackage + (sizeof(int) * 2)];

            // this is what is delivered:
            byte[] packageToSend = new byte[localDataBuffer.Length + headerForWholePackage.Length];

            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("np"), 0, headerForWholePackage, 0, tagForWholePackage);
            System.Buffer.BlockCopy(BitConverter.GetBytes(localDataBuffer.Length), 0, headerForWholePackage, tagForWholePackage, sizeof(int));
            System.Buffer.BlockCopy(BitConverter.GetBytes(byte_data.Length), 0, headerForWholePackage, tagForWholePackage + 4, sizeof(int));

            System.Buffer.BlockCopy(headerForWholePackage, 0, packageToSend, 0, headerForWholePackage.Length);
            System.Buffer.BlockCopy(localDataBuffer, 0, packageToSend, headerForWholePackage.Length, localDataBuffer.Length);

            //MainApp.MyWindow.update_fp.Stop();

            //System.Diagnostics.Debug.WriteLine("packages compressed:" + package.Length + " uncompressed:" + byte_data.Length + " approx ratio:" + (package.Length / (byte_data.Length * 1.0f)) + " pointPositions:" + (meshPointIndexCounter * 3) + " pointUVs:" + (meshPointIndexCounter * 2) + " whichMeshIndexList:" + meshIndexListCounter);

            //MainApp.MyWindow.update_fp.Start("send_mesh_data");


            if (depthDataWriterVar != null)
            {
                depthDataWriterVar.SendMeshData(packageToSend);
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
            if (writingDataToFile)
                fileWriter.writeDataToFile(packageToSend);
            //MainApp.MyWindow.update_fp.Stop();

            lastTransmitTick = DateTime.UtcNow.Ticks;

            return localValidUserMeshPointCounter;

        }

        private int packArray(byte[] header, byte[] source, byte[] buffer, int offset)
        {
            
            int tagsize = Encoding.ASCII.GetByteCount("kd");

            // [array] SRC, [int] SRC offset, [array] dst, [int] dstoffset, [int] count

            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("kd"), 0, header, 0, tagsize);

            System.Buffer.BlockCopy(BitConverter.GetBytes(source.Length), 0, header, tagsize, sizeof(int));
            //Buffer.BlockCopy(BitConverter.GetBytes(source.Length), 0, header, tagsize, 0);

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

        public void drawGrid(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (displayMode == 0)
                return;
            if (!readyToDraw)
                return;

            bool drawWireframe = false;

            if (displayMode == 2)
                drawWireframe = false;

            Vector4 textureColorOffset = new Vector4(1.0f, 1.0f, 1.0f, 0.8f); // transparent white
            //Vector4 textureColorOffset = new Vector4(0.0f, 0.0f, 1.0f, 1.0f); // blue
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
            

            //GL.DrawElements(BeginMode.Triangles, actualElementCount,
            //    DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.DrawElements(BeginMode.Triangles, actualElementCount, DrawElementsType.UnsignedInt, 0);


            GL.BindVertexArray(0);            
            // ******************************************


            GL.UseProgram(0);


        }
        public void drawReactiveAreas(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            // have their own vao's, and their own shader 
            reactiveAreasVar.draw(whichviewMat, whichProjMat);

        }
        #endregion drawData
        // ************************************************************

        public void initOpenGL()
        {
            bool doUsePerVertColor = true;
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
                shaderlocVertVaryColor = GL.GetAttribLocation(handleShader, "vVaryColor");


            //attributes
            shaderlocModelMatrix = GL.GetUniformLocation(handleShader, "mModelMatrix");
            shaderlocProjMatrix = GL.GetUniformLocation(handleShader, "mProjectionMatrix");
            shaderlocColor = GL.GetUniformLocation(handleShader, "vColorValue");
            shaderlocOffset = GL.GetUniformLocation(handleShader, "vPositionOffset");

            // ****************************************************************************
            // create VAO
            //createVAO();
            if (doUsePerVertColor) 
                createVAO2(handleShader, "vPosition", "", "", "vVaryColor");
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
       
        public bool doUpdateGraphicsOnNextPass = false;

        public void updateDrawGeometry()
        {
            updateGridVao();
        }

        public void toggleDrawEdges()
        {
            doDrawEdges = !doDrawEdges;
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
            bool doDraw = false;

            int drawnCounter = 0;

            unsafe
            {
                for (i = 0; i < actualResolutionX; ++i)
                {
                    for (j = 0; j < actualResolutionY; ++j)
                    {
                        doDraw = false;

                        depthTestValue = minDepthMeasured[i, j];
                        //if ((minDepthMeasured[i, j] < maxTransmitRange) && (minDepthMeasured[i, j] > minTransmitRange))
                        //{
                            if (activatedArea[i, j])
                            {
                                //if ((i % activatedAreaSubSample == 0 && j % activatedAreaSubSample == 0))
                                if ((((i % activatedAreaSubSample) == activatedAreaSubSample / 2) && ((j % activatedAreaSubSample) == 0)) || (((i % activatedAreaSubSample) == 0) && ((j % activatedAreaSubSample) == activatedAreaSubSample / 2)))
                                {
                                    doDraw = true;
                                    drawnCounter += 1;
                                    greyValue = 1f - (depthTestValue - minTransmitRange) / (maxTransmitRange - minTransmitRange);
                                    if (greyValue < 0.45f) // don't let it get too dark (on black background)
                                    {
                                        greyValue = 0.45f;
                                    }
                                }
                            }
                        //}

                        if (doDrawEdges)
                        {
                            if (isEdge[i, j])
                            {
                                //if (wasEdge[i, j])
                                //{
                                    doDraw = true;
                                    greyValue = 1f;

                                //}
                            }
                        }

                        if (doDraw)
                        {
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
                        //}



                    }
                }
            } // end unsafe code

            actualElementCount = indexCounter;
            updatePositionBuffer();
            updateColorBuffer();
            updateIndexBuffer();

            //Console.WriteLine("drawnCounter= " + drawnCounter);
        }

        // ***********************************
        #endregion create and update VBOs
        // ***********************************

        #region PlayAndRecordData

        public void togglePlayingOrRecording()
        {
            if (doEnableLocalFileInterrop)
            {
                if (playAndRecordDataFromFileVar.recordingToFileIsEnabled) // we are in record mode
                {
                    if (playAndRecordDataFromFileVar.isRecording)
                        playAndRecordDataFromFileVar.stopRecord();
                    else
                        playAndRecordDataFromFileVar.startRecord();
                }
                else // we are in playback mode
                {
                    if (playAndRecordDataFromFileVar.isPlaying)
                        playAndRecordDataFromFileVar.stopPlayback();
                    else
                        playAndRecordDataFromFileVar.startPlayback();
                }
            }
        }

        public bool getFileRecordingStatus()
        {
            if (doEnableLocalFileInterrop)
            {
                return playAndRecordDataFromFileVar.isRecording;
            }
            else
                return false;
        }
        public bool getPlayingStatus()
        {
            if (doEnableLocalFileInterrop)
            {
                return playAndRecordDataFromFileVar.isPlaying;
            }
            else
                return false;
        }

        #endregion PlayAndRecordData
        public void onClosing()
        {
            GL.DeleteProgram(handleShader);
            GL.DeleteVertexArrays(1, ref vaoID);
            reactiveAreasVar.exitApp();
        }

    }
}
