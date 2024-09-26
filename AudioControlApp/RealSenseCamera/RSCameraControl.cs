using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Timers;

using AudioControlApp.Shaders;

//#r "Intel.RealSense.dll"
using Intel.RealSense;
using AudioControlApp.USBInspection;

namespace AudioControlApp.RealSenseCamera
{
    public class RSCameraControl
    {
        private static int whichDepthW = 848;
        private static int whichDepthH = 480;
        private static int maxNumberOfDepthCameras = 2; // hard coded limit for number of possible cameras

        
        private CheckConnected readSysIDsFromTxtFile;
        
        public bool[] cameraIsActive = new bool[4];
        // these are shortcuts for optimization of points processing:
        private bool checkBothCams = false;
        private bool checkOnlyCam00 = false;
        private bool checkOnlyCam01 = false;
        
        private bool[] overrideCamActivationFromConfig = new bool[4];
        public string[] listOfDevSN = new string[4];
        public string[] listOfDevFW = new string[4];
        public int currentlyConnectedAndActiveDeviceCount = 0;
        public int deviceCountAtLaunch = 0;
        public int actualNumberOfDepthCameras = -1;
        
        // measure performance
        private float[] cameraUpdateFramesPerRefreshCycle = new float[maxNumberOfDepthCameras];
        private float[] prevFPS = new float[maxNumberOfDepthCameras];

        private float[] currentFPS = new float[maxNumberOfDepthCameras];
        private float localTransmissionRate = 33; // this is set in the MainApp vars section
        public string fpsReport = "NA";
        
        // orientation
        private bool[] mirrorData = new bool[4];
        private bool[] rotateData = new bool[4];
        private bool[] rotateCW = new bool[4];
        private bool[] invertData = new bool[4];

        private bool doApplySensorFilters = false;
        private double[] sensorRangeMax = new double[4]; // prefilter far
        private double[] sensorRangeMin = new double[4]; // prefilter near
        private double[] sensorFilterCeil = new double[4]; // filter out data after calibrated
        private double[] sensorFilterFloor = new double[4]; // filter out data after calibrated


        // Calibration
        private int activeCameraAdjust = -1; // which camera are we curently adjust position/orientation of?
        private double globalDisplayScale = 1.0;


        // class variables
        public RSCameraData[] depthCameraDataVar = new RSCameraData[maxNumberOfDepthCameras];

        public WallCommunicationsAndControl.UserSensorGridNoSmooth userSensorGridVar;
        public int transmitPackageSize = 0;


        // camera settings
        private Sensor depthCameraSensor0;
        private Sensor depthCameraSensor1;
        private RSBackgroundThread readingLoop0;
        private RSBackgroundThread readingLoop1;

        
        private float[] localXyzImageBuffer0 = new float[whichDepthW * whichDepthH * 3];
        private float[] localXyzImageBuffer1 = new float[whichDepthW * whichDepthH * 3];
        //private int cameraFramesRead = 0;

        private bool camera0FrameLoaded = false;
        private bool camera1FrameLoaded = false;

        private bool processingPointsActive = false;
        private float[] tempXyzImageBuffer0 = new float[whichDepthW * whichDepthH * 3];
        private float[] tempXyzImageBuffer1 = new float[whichDepthW * whichDepthH * 3];


        private double newFrameTime;
        private double previousFrameTime;

        private bool isTrackingSkeletons = false;
        private bool sensorIsOn = false;

        // grab data from depth camera on a separate timer (not openGL)
        private System.Timers.Timer proccessAndXmitDataTimer; // use system timer (higher performance)

        // do not start the cameras immediately, wait for moments first:
        private int updateDepthDataTimerWaitCounter = 0;

        private int updateDepthDataTimerWaitCounterLimit = 120; // wait this many ticks before pulling camera(s) for data

        private bool theWaitIsOver = false;
        private bool okayToUpdateDepthDataFromLocalFile = false;

        // ******************************************************************
        // drawing variables
        shaderFileLoader simpleFlatShaderSource;
        int handleVBO, handleVAO, handleEBO, handleShader;

        // local shader variables:
        int shaderlocPosition,
            shaderlocColor,
            shaderlocOffset,
            shaderlocModelMatrix,
            shaderlocProjMatrix;
        // ******************************************************************


        public RSCameraControl(int whichTransmitRate)
        {
            localTransmissionRate = (float)whichTransmitRate;
            int i;
            for (i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                cameraUpdateFramesPerRefreshCycle[i] = 0;
                prevFPS[i] = 0;
                currentFPS[i] = 0;
            }
            
            // reset these status, wait for
            cameraIsActive[0] = false;
            cameraIsActive[1] = false;
            cameraIsActive[2] = false;
            cameraIsActive[3] = false;

            // config file may request NOT to activate camera
            overrideCamActivationFromConfig[0] = false;
            overrideCamActivationFromConfig[1] = false;
            overrideCamActivationFromConfig[2] = false;
            overrideCamActivationFromConfig[3] = false;
            
            readSysIDsFromTxtFile = new CheckConnected();

            // init orientation
            for (i = 0; i < 4; i++)
            {
                listOfDevFW[i] = "NA";
                listOfDevSN[i] = "NA";
                invertData[i] = false;
                rotateData[i] = false;
                rotateCW[i] = false;
                invertData[i] = false;
            }

            updateDepthDataTimerWaitCounterLimit = (int)Math.Ceiling((1000.0f / (float)whichTransmitRate) * 1.5f); // round off to 1.5 seconds wait time
            updateDepthDataTimerWaitCounter = 0; // wait a few seconds before grabbing data from cameras

            proccessAndXmitDataTimer = new System.Timers.Timer();
            proccessAndXmitDataTimer.Interval = whichTransmitRate;
            proccessAndXmitDataTimer.AutoReset = true;
            proccessAndXmitDataTimer.Elapsed += new ElapsedEventHandler(processAndXmitData_Tick);
        }


        public void overideActivationFromConfig(int whichCam, bool whichActivationOverride) // get activation status from config
        {
            overrideCamActivationFromConfig[whichCam] = whichActivationOverride;
        }
        
        public void initOpenGL()
        {
            // ****************************************************************************
            // create object buffers
            // ****************************************************************************
            // load shader source
            simpleFlatShaderSource = new shaderFileLoader();
            simpleFlatShaderSource.loadShaders("shaders\\FlatShader2.vp", "shaders\\FlatShader2.fp");
            //simpleFlatShaderSource.loadShaders("shaders\\PerVertexColor.vp", "shaders\\PerVertexColor.fp"); // TODO can't get per-vert color to work here...

            // ****************************************************************************
            // create shader app
            handleShader = ShaderLoader.CreateProgram(simpleFlatShaderSource.vertexShaderSource,
                simpleFlatShaderSource.fragmentShaderSource);

            GL.UseProgram(handleShader);

            // ****************************************************************************
            // retreive shader locations
            // attributes
            shaderlocPosition = GL.GetAttribLocation(handleShader, "vPosition");
            shaderlocColor = GL.GetUniformLocation(handleShader, "vColorValue"); // color overide (in flat shader)
            shaderlocOffset = GL.GetUniformLocation(handleShader, "vPositionOffset");
            // uniforms
            shaderlocModelMatrix = GL.GetUniformLocation(handleShader, "mModelMatrix");
            shaderlocProjMatrix = GL.GetUniformLocation(handleShader, "mProjectionMatrix");

            // ****************************************************************************
            // create VAO
            // createVAO();
            GL.UseProgram(0);

        }

        

        public void initOpenGLUserSensorGrid()
        {
            userSensorGridVar.initOpenGL();
        }

        public bool initDepthCameraDevice()
        {
            bool valueToReturn = false;
            //writeToEventLog("[K4ACamCtrl] initDepthCameraDevice called");
            System.Diagnostics.Debug.WriteLine("[RSCamCtrl] initDepthCameraDevice called");

            depthCameraDataVar[0] = new RSCameraData(0, true, globalDisplayScale, 0.0, 0.0, whichDepthW, whichDepthH, true, rotateData[0], rotateCW[0], mirrorData[0], invertData[0]);
            depthCameraDataVar[0].setSensorRange(sensorRangeMax[0], sensorRangeMin[0]);
            depthCameraDataVar[0].setSensorFloorCeil(sensorFilterFloor[0], sensorFilterCeil[0]);
            if (doApplySensorFilters)
                depthCameraDataVar[0].toggleSensorFloorCeil(doApplySensorFilters);


            depthCameraDataVar[1] = new RSCameraData(1, true, globalDisplayScale, 0.0, 0.0, whichDepthW, whichDepthH, true, rotateData[1], rotateCW[1], mirrorData[1], invertData[1]);
            depthCameraDataVar[1].setSensorRange(sensorRangeMax[1], sensorRangeMin[1]);
            depthCameraDataVar[1].setSensorFloorCeil(sensorFilterFloor[1], sensorFilterCeil[1]);
            if (doApplySensorFilters)
                depthCameraDataVar[1].toggleSensorFloorCeil(doApplySensorFilters);

            

            
            // threshhold stuff from https://support.intelrealsense.com/hc/en-us/community/posts/360039445773-Json-configuration-change-the-min-max-distance-depth-to-get-an-image-with-the-right-colors-D415-camera-C-?page=1#community_comment_360010898533
            SpatialFilter spatial = new SpatialFilter();
            spatial.Options[Option.FilterMagnitude].Value = 5f;
            spatial.Options[Option.FilterSmoothAlpha].Value = 1f;
            spatial.Options[Option.FilterSmoothDelta].Value = 50f;
            
            // tmporal filter unused?
            TemporalFilter temp = new TemporalFilter();
            // another unused filter?
            HoleFillingFilter holeFill = new HoleFillingFilter();
            Align align_to = new Align(Stream.Depth);
            
            ThresholdFilter threshHold = new ThresholdFilter();
            threshHold.Options[Option.MinDistance].Value = 0.73f;
            threshHold.Options[Option.MaxDistance].Value = 2f;

            
            
            
            if (currentlyConnectedAndActiveDeviceCount>0) // there is a camera and it has been matched to device path
            {
                if (cameraIsActive[0]) // this is only set to true when it has been matched to device path
                {
                    if (readingLoop0 == null)
                    {
                        readingLoop0 = RSBackgroundThread.CreateForDevice(depthCameraSensor0, listOfDevSN[0]); // depthcameraSensor is assigned in the checkForConnected() function

                        readingLoop0.CaptureReady += ReadingLoop_CaptureReady0;
                        readingLoop0.Failed += ReadingLoop_Failed0;
                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] camera0 ready");
                        valueToReturn = true;
                    }
                }
                if (cameraIsActive[1])
                {
                    if (readingLoop1 == null)
                    {
                        readingLoop1 = RSBackgroundThread.CreateForDevice(depthCameraSensor1, listOfDevSN[1]); // depthcameraSensor is assigned in the checkForConnected() function

                        readingLoop1.CaptureReady += ReadingLoop_CaptureReady1;
                        readingLoop1.Failed += ReadingLoop_Failed1;
                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] camera1 ready");
                        valueToReturn = true;
                    }
                }

                // this is optimizing code for testing data
                if (cameraIsActive[0] && cameraIsActive[1])
                    checkBothCams = true;
                else if (cameraIsActive[0] && !cameraIsActive[1])
                    checkOnlyCam00 = true;
                else if (!cameraIsActive[0] && cameraIsActive[1])
                    checkOnlyCam01 = true;

            }
            proccessAndXmitDataTimer.Start();
            return valueToReturn;
        }


        public bool checkForConnectedFromMain()
        {
            bool valueToReturn = false;

            valueToReturn = checkForConnected();
            
            return valueToReturn;
        }
        
        //Sensor sensor;
        private bool checkForConnected()
        {
            string lastError;
            
            bool valueToReturn = false;
            int i;


            string sn, fw; // = e.Capture.Sensor.Info[CameraInfo.SerialNumber];
            string devInstPath; // = e.Capture.Sensor.Info[CameraInfo.PhysicalPort];
            int matchedCameraID = -1;
            
            using (var ctx = new Context())
            {
                //var pipes = new List<Pipeline>;
                using (DeviceList cameras = ctx.QueryDevices())
                {
                    if (cameras.Count == 0)
                    {
                        lastError = "No device detected. Is it plugged in?";
                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] "+ lastError);
                        valueToReturn = false;
                        //throw new Exception(lastError);
                    }
                    else // connecting multiple cameras
                    {
                        actualNumberOfDepthCameras = cameras.Count;
                        valueToReturn = true; // there are cameras connected
                        for (i = 0; i < actualNumberOfDepthCameras; ++i)
                        {
                            using (Device camera = cameras[i])
                            {
                                using (var _sensor = camera.Sensors[0]) // just pick the first in the array (it is the depth)
                                {
                                    devInstPath = _sensor.Info[CameraInfo.PhysicalPort];
                                    sn = _sensor.Info[CameraInfo.SerialNumber];
                                    fw = _sensor.Info[CameraInfo.FirmwareVersion];
                                    
                                    System.Diagnostics.Debug.WriteLine("[RSCamCtrl] checking cam sn [" + sn + "] on dev inst path " + devInstPath);
                                    listOfDevSN[i] = sn;
                                    listOfDevFW[i] = fw;
                                    
                                    matchedCameraID = readSysIDsFromTxtFile.getIDFromPort(devInstPath);
                                    System.Diagnostics.Debug.WriteLine("[RSCamCtrl] received match data: " + matchedCameraID);
                                    if (matchedCameraID == 0)
                                    {
                                        if (overrideCamActivationFromConfig[0]) // only proceed if config file allows
                                        {
                                            depthCameraSensor0 = _sensor;
                                            System.Diagnostics.Debug.WriteLine("[RSCamCtrl] located camera 0");
                                            cameraIsActive[0] = true;
                                            currentlyConnectedAndActiveDeviceCount += 1;
                                        }
                                    }
                                    else if (matchedCameraID == 1)
                                    {
                                        if (overrideCamActivationFromConfig[1]) // only proceed if config file allows
                                        {
                                            depthCameraSensor1 = _sensor;
                                            System.Diagnostics.Debug.WriteLine("[RSCamCtrl] located camera 1");
                                            cameraIsActive[1] = true;
                                            currentlyConnectedAndActiveDeviceCount += 1;
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] no match found for cam " + i + " and " + matchedCameraID);
                                    }
                                }
                            }
                        }
                    }

                    /*
                    // only connecting one camera:
                    using (var dev = list[0])
                    {
                        actualNumberOfDepthCameras = list.Count;
                        Console.WriteLine(dev);
                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] found connected camera: "+ dev.ToString() );
                        using (var _sensor = dev.Sensors[0])
                        {
                            depthCameraSensor0 = _sensor;
                            System.Diagnostics.Debug.WriteLine("[RSCamCtrl] camera info: " + _sensor.Info.GetInfo(0));
                            valueToReturn = true;
                        }
                    }
                    */
                }
            }

            return valueToReturn;
        }

        private void ReadingLoop_Failed0(object sender, FailedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RSCamCtrl] ReadingLoop_Failed 0 with [" + e.Exception.Message + "]");
        }
        private void ReadingLoop_Failed1(object sender, FailedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RSCamCtrl] ReadingLoop_Failed 1 with [" + e.Exception.Message + "]");
        }

        private void ReadingLoop_CaptureReady0(object sender, CaptureReadyEventArgs e)
        {
            if (!sensorIsOn)
                return;

            //string sn = e.Capture.Sensor.Info[CameraInfo.SerialNumber];
            //string physicalPort = e.Capture.Sensor.Info[CameraInfo.PhysicalPort];
            // does it match this port? USB\VID_8086&PID_0B5C&MI_00\7&34F1C99C&1&0000
            // it returned this \\?\usb#vid_8086&pid_0b5c&mi_00#7&34f1c99c&1&0000#{e5323777-f976-4f5b-9b55-b94699c46e44}\global
            
            using (var depthImage = e.Capture)
            {
                if (depthImage == null)
                    return;
                if (depthImage.DataSize < (whichDepthW* whichDepthH * 2)) // in case camera passed a partial image ?
                    return;
                if (processingPointsActive)
                    return;
                /* this crashes the program??
                newFrameTime = depthImage.Timestamp;
                if (newFrameTime == previousFrameTime) // nothing new here...
                {
                    System.Diagnostics.Debug.WriteLine("[RSCamCtrl] openGL update before new data avail");
                    return;
                }
                previousFrameTime = newFrameTime;
                */
                
                var pc = new PointCloud();
                
                using (var points = pc.Process(depthImage).As<Points>())
                {
                    
                    var vertices = new float[points.Count * 3];
                    points.CopyVertices(tempXyzImageBuffer0);
                    
                    Marshal.Copy(points.VertexData, localXyzImageBuffer0, 0, depthImage.DataSize); // faster than points.CopyVertices(tempXyzImageBuffer0) ?
                    //points.VertexData 
                    //System.Diagnostics.Debug.WriteLine("[RSCamCtrl] copying "+xyzImageBuffer.Length +" points into buffer");
                    //int testPointIndexLocation = (848 * 480) / 2;
                    //System.Diagnostics.Debug.WriteLine("[RSCamCtrl] center point pos ["+xyzImageBuffer[testPointIndexLocation] +", "+xyzImageBuffer[testPointIndexLocation+1] +", "+xyzImageBuffer[testPointIndexLocation+2]+"] points into buffer");

                    //Array.Copy(tempXyzImageBuffer0, localXyzImageBuffer0, tempXyzImageBuffer0.Length);
                    
                    //Marshal.Copy(tempXyzImageBuffer0, localXyzImageBuffer0, tempXyzImageBuffer0.Length);
                    //depthCameraDataVar[0].updateXYZData(localXyzImageBuffer0);

                    //CaptureReady?.Invoke(this, new CaptureReadyEventArgs(vertices));
                    //cameraFramesRead += 1;
                    
                }
                /*
                unsafe
                {
                    ushort* depth_data = (ushort*) depthImage.Data.ToPointer();
                    for (int i = 0; i < depthImage.Width * depthImage.Height; ++i)
                    {
                        xyzImageBuffer[i] = depth_data[i];
                    }
                }
                Array.Copy(xyzImageBuffer, localXyzImageBuffer, xyzImageBuffer.Length);
                depthCameraDataVar[0].updateXYZData(localXyzImageBuffer);*/
                cameraUpdateFramesPerRefreshCycle[0] += 1; // performance counter
                camera0FrameLoaded = true;
                //if (camera1FrameLoaded) // do we have two fresh frames?
                //{
                //    camera0FrameLoaded = false;
                //    camera1FrameLoaded = false;
                //    updateDepthData();
                //}
                
                /*if (cameraFramesRead >= currentlyConnectedAndActiveDeviceCount) // this delay originally meant for collecting data from multiple cameras?
                {
                    updateDepthData();
                    cameraFramesRead = 0;
                }*/
            }
        }

        private void ReadingLoop_CaptureReady1(object sender, CaptureReadyEventArgs e)
        {
            if (!sensorIsOn)
                return;
            
            using (var depthImage = e.Capture)
            {
                if (depthImage == null)
                    return;
                if (depthImage.DataSize < (whichDepthW* whichDepthH * 2)) // in case camera passed a partial image ?
                    return;
                if (processingPointsActive)
                    return;
                
                var pc = new PointCloud();
                using (var points = pc.Process(depthImage).As<Points>())
                {
                    //var vertices = new float[points.Count * 3];
                    //points.CopyVertices(tempXyzImageBuffer1);
                    //Array.Copy(tempXyzImageBuffer1, localXyzImageBuffer1, tempXyzImageBuffer1.Length);
                    Marshal.Copy(points.VertexData, localXyzImageBuffer1, 0, depthImage.DataSize);
                    
                    //depthCameraDataVar[1].updateXYZData(localXyzImageBuffer1);
                    //cameraFramesRead += 1;
                    
                }
                cameraUpdateFramesPerRefreshCycle[1] += 1; // performance counter
                
                camera1FrameLoaded = true;
                
                /*
                if (cameraFramesRead >= currentlyConnectedAndActiveDeviceCount) // this delay originally meant for collecting data from multiple cameras?
                {
                    updateDepthData();
                    cameraFramesRead = 0;
                }*/
            }
        }
        

        public void initOpenGLComponents()
        {

            GL.UseProgram(handleShader);

            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                if (depthCameraDataVar[i] != null)
                {
                    depthCameraDataVar[i].initOpenGL();
                    depthCameraDataVar[i].createVAO(handleShader);


                }
                
            }
            GL.UseProgram(0);
        }

       


        // ***************************************
        #region sensorDataUpdate
        // ***************************************
        //object lockobject = new object();
        private void processAndXmitData_Tick(object sender, EventArgs e)
        {
            if (!theWaitIsOver) // wait and initialize cameras some time after program launch...
            {
                updateDepthDataTimerWaitCounter += 1;

                if (updateDepthDataTimerWaitCounter > updateDepthDataTimerWaitCounterLimit)
                {
                    theWaitIsOver = true;
                    System.Diagnostics.Debug.WriteLine("updateDepthData_Tick is begining to pull data from cameras");
                    sensorIsOn = true;
                    if (cameraIsActive[0])
                    {
                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] launching reading loop 0");
                        readingLoop0.Run();
                    }

                    if (cameraIsActive[1])
                    {
                        System.Diagnostics.Debug.WriteLine("[RSCamCtrl] launching reading loop 1");
                        readingLoop1.Run();
                    }
                }
                else
                    System.Diagnostics.Debug.WriteLine("updateDepthData_Tick still waiting on camera start command");

                return;
            }

            if (userSensorGridVar.getPlayingStatus()) //  special case if reading from file...
            {
                okayToUpdateDepthDataFromLocalFile = true;
            }

            if (userSensorGridVar.getPlayingStatus() && actualNumberOfDepthCameras <= 0) // special case if we are reading a file (and no camera is present)..
                updateDepthData();

            // measuring camera performance:
            float timePassedSeconds = localTransmissionRate / 1000f; // refresh rate is in ms
            float diff;
            float newFPS;
            int newFPS_i;
            string fps_st;
            fpsReport = "current FPS: \n";
            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                if (cameraIsActive[i])
                {
                    
                    currentFPS[i] = cameraUpdateFramesPerRefreshCycle[i] / timePassedSeconds;

                    diff = currentFPS[i] - prevFPS[i];
                    newFPS = prevFPS[i] + (diff / 10f);
                    newFPS_i = (int)Math.Round(newFPS);
                    prevFPS[i] = newFPS;
                    if (newFPS_i < 10)
                    {
                        fps_st = "0" + newFPS_i;
                    }
                    else
                    {
                        fps_st = "" + newFPS_i;
                    }
                    fpsReport += "cam " + i + " fps:" + fps_st;
                    cameraUpdateFramesPerRefreshCycle[i] = 0; // reset counter
                    fpsReport += "\n"; // new line per camera easier to read

                }
            }

            
            if (checkBothCams)
            {
                if (camera0FrameLoaded && camera1FrameLoaded) // do we have two fresh frames?
                {
                    processingPointsActive = true;
                    depthCameraDataVar[0].updateXYZData(localXyzImageBuffer0); // generates VBO data
                    depthCameraDataVar[1].updateXYZData(localXyzImageBuffer1);

                    updateDepthData();

                    camera0FrameLoaded = false;
                    camera1FrameLoaded = false;
                    processingPointsActive = false;
                }
            }
            else if (checkOnlyCam00)
            {
                if (camera0FrameLoaded) // do we have two fresh frames?
                {
                    processingPointsActive = true;
                    depthCameraDataVar[0].updateXYZData(localXyzImageBuffer0); // generates VBO data
                    //depthCameraDataVar[1].updateXYZData(localXyzImageBuffer1);

                    updateDepthData();

                    camera0FrameLoaded = false;
                    //camera1FrameLoaded = false;
                    processingPointsActive = false;
                }
            }
            else if (checkOnlyCam01)
            {
                if (camera1FrameLoaded) // do we have two fresh frames?
                {
                    processingPointsActive = true;
                    //depthCameraDataVar[0].updateXYZData(localXyzImageBuffer0); // generates VBO data
                    depthCameraDataVar[1].updateXYZData(localXyzImageBuffer1);

                    updateDepthData();

                    //camera0FrameLoaded = false;
                    camera1FrameLoaded = false;
                    processingPointsActive = false;
                }
            }
        }


        private void transmitDepthData()
        {
            bool doTransmit = false;
            if (userSensorGridVar.getConnectionStatus() > 0)
                doTransmit = true;

            if (userSensorGridVar.doEnableLocalFileInterrop) // if recording: store local data, if playing back: transmit data gathered in "getDataFromRecordedFile" step above
                doTransmit = true;

            if (doTransmit)
                transmitPackageSize = userSensorGridVar.transmitMeshData();
        }

        private void updateUserSensorGrid()
        {
            
        }
        
        private void updateDepthData() // not in the openGL loop
        {
            int i;


            if (sensorIsOn)
            {
                
                
                /*
                for (i = 0; i < maxNumberOfDepthCameras; ++i)
                {

                    if (depthCameraObjectVar[i] != null)
                        depthCameraDataVar[i].updateXYZData(depthCameraObjectVar[i].lastDownSampledDepthData);
                }
                */
                // prepare to smooth the incoming data:
                // userSensorGridVar.updatePreviousValues();
                //userSensorGridVar.resetAvgValues();

                // measure all new minumum range data:
                //userSensorGridVar.resetMinMeasured();

                //after updating data on (n) depth cameras
                // combine data and rectify with image plane
                if (!userSensorGridVar.getPlayingStatus()) // only proceed if we're processing live data...
                {
                    userSensorGridVar.resetMinMeasured();

                    for (i = 0; i < currentlyConnectedAndActiveDeviceCount; ++i)
                    {
                        if (depthCameraDataVar[i] != null)
                        {
                            // optimized: instead of testing each point, pass entire array and test in unsafe loop:
                            userSensorGridVar.testPointsOnGrid(depthCameraDataVar[i].positionVboData, depthCameraDataVar[i].transfPointsToDraw, i);
                        }
                    }
                }


                //userSensorGridVar.updateDrawGeometry(); // can't update this (not in draw loop)
                userSensorGridVar.doUpdateGraphicsOnNextPass = true;

                if (userSensorGridVar.getPlayingStatus()) // if we're playing back from a file
                {
                    if (okayToUpdateDepthDataFromLocalFile) // need to throttle playback speed (otherwise plays back at GL refresh speeds
                    {
                        userSensorGridVar.resetMinMeasured();  // only reset when you're ready to fill with new. but cool persistence effect if you comment this out
                        userSensorGridVar.getDataFromRecordedFile(); // retreive pre-recorded data
                        okayToUpdateDepthDataFromLocalFile = false;
                    }
                }
                else
                {
                    userSensorGridVar.prepUserGridDataForTransmission(); // process last good camera data frame
                }

                //System.Diagnostics.Debug.WriteLine("mid test pnt [" + userSensorGridVar.minDepthMeasured[25, 25] + "]");

                bool userActivationDataChanged = false;
                userActivationDataChanged = userSensorGridVar.checkDataAgainstActivationAreas();


               // transmitData(); if wanted/needed...
            }
        }
        public void checkForDataUpdateForGraphics() // performed inside of openGL loop
        {
            int i;
            for (i = 0; i < maxNumberOfDepthCameras; ++i)
            {

                //if (depthCameraObjectVar[i] != null)
                if (depthCameraDataVar[i] != null)
                {
                    if (depthCameraDataVar[i].doUpdateGraphicsOnNextPass)
                    {
                        depthCameraDataVar[i].doUpdateGraphicsOnNextPass = false;
                        depthCameraDataVar[i].updateDrawGeometry();
                    }
                }
                /*
                if (skeletalDataVar[i] != null)
                {
                    
                    if (skeletalDataVar[i].doUpdateSkelectonOnNextPass)
                    {
                        skeletalDataVar[i].doUpdateSkelectonOnNextPass = false;
                        skeletalDataVar[i].updateSkeletonDrawGeometry();
                    }

                }*/
            }

            if (userSensorGridVar != null)
            {
                if (userSensorGridVar.doUpdateGraphicsOnNextPass)
                {
                    userSensorGridVar.doUpdateGraphicsOnNextPass = false;
                    userSensorGridVar.updateDrawGeometry();
                }
            }
        }
        // ***************************************
        #endregion sensorDataUpdate
        // ***************************************

        // ***************************************
        #region openGLDraw
        // ***************************************

        public void drawCameraData(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            GL.UseProgram(handleShader);

            // udpate uniforms in shaders:
            GL.UniformMatrix4(shaderlocModelMatrix, false, ref whichviewMat);
            GL.UniformMatrix4(shaderlocProjMatrix, false, ref whichProjMat);

            //if (depthCameraObjectVar[0] != null)
            if (depthCameraDataVar[0] != null)
            {
                depthCameraDataVar[0].drawTransformedDataPointsProgPipeline(shaderlocColor);
                //depthCameraDataVar[0].drawTransformedSkeletonProgPipeline(shaderlocColor);
            }
            if (depthCameraDataVar[1] != null)
            {
                depthCameraDataVar[1].drawTransformedDataPointsProgPipeline(shaderlocColor);
            }

            /*
            if (depthCameraObjectVar[1] != null)
            {
                depthCameraDataVar[1].drawTransformedDataPointsProgPipeline(shaderlocColor);
            }
            if (depthCameraObjectVar[2] != null)
            {
                depthCameraDataVar[2].drawTransformedDataPointsProgPipeline(shaderlocColor);
            }
            if (depthCameraObjectVar[3] != null)
            {
                depthCameraDataVar[3].drawTransformedDataPointsProgPipeline(shaderlocColor);
            }
            */
           
            GL.UseProgram(0);
        }

        /*
        public void drawUserSkeletalData(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (isTrackingSkeletons)
            {
                GL.UseProgram(handleShader);

                // udpate uniforms in shaders:
                GL.UniformMatrix4(shaderlocModelMatrix, false, ref whichviewMat);
                GL.UniformMatrix4(shaderlocProjMatrix, false, ref whichProjMat);
                
                
                if (skeletalDataVar[0] != null)
                {
                    skeletalDataVar[0].drawTransformedSkeletonProgPipeline(shaderlocColor);
                    skeletalDataVar[0].drawRingProgPipeline(shaderlocColor, shaderlocOffset);
                }
                GL.UseProgram(0);
            }

        }*/


        public void drawUserGridData(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (userSensorGridVar != null)
                userSensorGridVar.drawGrid(whichviewMat, whichProjMat);
        }
        public void drawReactiveAreas(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (userSensorGridVar != null)
                userSensorGridVar.drawReactiveAreas(whichviewMat, whichProjMat);
        }
        // ***************************************
        #endregion openGLDraw
        // ***************************************


        // ***************************************
        #region sensorOrientation
        // ***************************************


        public void setReverseImage(int whichCam, bool isReversed)
        {
            mirrorData[whichCam] = isReversed;
        }

        public void setTurnDepthCameraOnSide(int whichCam, bool isTurned, bool isTurnedCW)
        {
            rotateData[whichCam] = isTurned;
            rotateCW[whichCam] = isTurnedCW;
        }

        public void setTurnCameraUpsideDown(int whichCam, bool isInverted)
        {
            invertData[whichCam] = isInverted;
        }

        public void ajustActiveSensorAdjustmentTo(int whichDepthCamera)
        {
            activeCameraAdjust = whichDepthCamera;
        }

        public void rotateSensorX(bool increase)
        {
            if (activeCameraAdjust != -1)
            {
                if (depthCameraDataVar[activeCameraAdjust] != null)
                    depthCameraDataVar[activeCameraAdjust].rotateSensorHeadX(increase);
            }
        }
        public void rotateSensorY(bool increase)
        {
            if (activeCameraAdjust != -1)
            {
                if (depthCameraDataVar[activeCameraAdjust] != null)
                    depthCameraDataVar[activeCameraAdjust].rotateSensorHeadY(increase);
            }
        }
        public void rotateSensorZ(bool increase)
        {
            if (activeCameraAdjust != -1)
            {
                if (depthCameraDataVar[activeCameraAdjust] != null)
                    depthCameraDataVar[activeCameraAdjust].rotateSensorHeadZ(increase);
            }
        }

        public void moveSensorZ(bool increase, bool jumpFar)
        {
            if (activeCameraAdjust != -1)
            {
                if (depthCameraDataVar[activeCameraAdjust] != null)
                    depthCameraDataVar[activeCameraAdjust].moveSensorHeadZ(increase, jumpFar);
            }
        }

        public void moveSensorY(bool increase, bool jumpFar)
        {
            if (activeCameraAdjust != -1)
            {
                if (depthCameraDataVar[activeCameraAdjust] != null)
                    depthCameraDataVar[activeCameraAdjust].moveSensorHeadY(increase, jumpFar);
            }
        }

        public void moveSensorX(bool increase, bool jumpFar)
        {
            if (activeCameraAdjust != -1)
            {
                if (depthCameraDataVar[activeCameraAdjust] != null)
                    depthCameraDataVar[activeCameraAdjust].moveSensorHeadX(increase, jumpFar);
            }
        }

        public void setSensorRanges(double whichMax, double whichMin)
        {
            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                sensorRangeMax[i] = whichMax;
                sensorRangeMin[i] = whichMin;
                // when this is called, depthcameradataDoesn't yet exist
                //if (depthCameraDataVar[i] != null)
                //    depthCameraDataVar[i].setSensorRange(whichMax, whichMin);
            }
        }
        public void setSensorFloorCeil(double whichFloor, double whichCeil)
        {
            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                sensorFilterCeil[i] = whichCeil;
                sensorFilterFloor[i] = whichFloor;
                // when this is called, depthcameradataDoesn't yet exist
                //if (depthCameraDataVar[i] != null)
                //    depthCameraDataVar[i].setSensorFloorCeil((float)whichFloor, (float)whichCeil);
            }
            
            //if (isTrackingSkeletons)
            //    skeletalDataVar[0].setFloorPosition((float)whichFloor); // this is null still -
        }

        public void setFilterDefault(bool whichValue)
        {
            doApplySensorFilters = whichValue;
        }

        public void toggleSensorFloorCeil()
        {
            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                if (depthCameraDataVar[i] != null)
                {
                    depthCameraDataVar[i].toggleSensorFloorCeil();
                }
            }
        }

        public void setXmitRanges(double whichNear, double whichFar)
        {
            userSensorGridVar.setDepthRanges(whichNear, whichFar);
        }

        public void setUserGridMeasurement(double whichWidth, double whichStartY, double whichEndY)
        {
            userSensorGridVar.setUserGridMeasurementRanges(whichWidth, whichStartY, whichEndY);
        }

        public void setSkeletalTracking(bool whichSkeletalMode)
        {
            isTrackingSkeletons = whichSkeletalMode;
        }

        public void adjustHFOV(bool incraseIt)
        {
            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                if (depthCameraDataVar[i] != null)
                    depthCameraDataVar[i].adjustFOV(true, incraseIt);
            }
        }
        public void adjustVFOV(bool incraseIt)
        {
            for (int i = 0; i < maxNumberOfDepthCameras; ++i)
            {
                if (depthCameraDataVar[i] != null)
                    depthCameraDataVar[i].adjustFOV(false, incraseIt);
            }

        }
        public void setGlobalScale(double whichPixelScale)
        {
            globalDisplayScale = whichPixelScale; // global display scale is the pixel width divided by pysical width 5120pixels/6858 mm
        }


        // ***************************************
        #endregion sensorOrientation
        // ***************************************

        public void initDepthCommunicationsGrid(int gridResWidth, int gridResHeight, int gridResDepth, bool doActivateDataTransmisssion, int whichTransmisiionRate, bool doEnableLocalFile, bool doRecordToFile, bool doPlayFromFile, string whichDataFileName)
        {
            userSensorGridVar = new AudioControlApp.WallCommunicationsAndControl.UserSensorGridNoSmooth(gridResWidth, gridResHeight, gridResDepth, doActivateDataTransmisssion, whichTransmisiionRate, doEnableLocalFile, doRecordToFile, doPlayFromFile, whichDataFileName);
        }

        public void initDataTransmission(int whichPort)
        {

            userSensorGridVar.initDepthTransmit(whichPort);

        }

        public void togglePlayingOrRecording()
        {
            userSensorGridVar.togglePlayingOrRecording();
        }
        public bool getFileRecordingStatus()
        {
            return userSensorGridVar.getFileRecordingStatus();
        }

        public void haltCameraControl()
        {
            sensorIsOn = false;
            
            
            if (readingLoop0 != null)
            {
                readingLoop0.Failed -= ReadingLoop_Failed0;
                readingLoop0.CaptureReady -= ReadingLoop_CaptureReady0;
                readingLoop0.Dispose();
            }

            if (readingLoop1 != null)
            {
                readingLoop1.Failed -= ReadingLoop_Failed1;
                readingLoop1.CaptureReady -= ReadingLoop_CaptureReady1;
                readingLoop1.Dispose();
            }

            proccessAndXmitDataTimer.Stop();
            proccessAndXmitDataTimer.Dispose();

            /*
            if (depthCameraDevice != null)
            {
                //if (!depthCameraDevice.IsDisposed) // halting the reading loop may also be disposing camera...
                //{
                //    depthCameraDevice.StopCameras();
                    depthCameraDevice.Dispose();
                //}
            }*/
        }

        public void haltDataTransmission() //background app has stopped (reported over network)
        {
            if (userSensorGridVar != null)
                userSensorGridVar.terminateDepthTransmit();
            userSensorGridVar.haltingProgram();
            //backgroundApplicationIsRunning = false; // by setting this, data is never sent
        }

        private void writeToEventLog(string whichMessage)
        {
            try
            {
                System.Diagnostics.EventLog.WriteEntry("SensorCtrlCamClass", whichMessage, System.Diagnostics.EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[CAMCTRL] event log access denied ");
            }
        }
        public void onClosing()
        {
            if (sensorIsOn)
                haltCameraControl();

            haltDataTransmission();

            GL.DeleteProgram(handleShader);

            
            if (readingLoop0!=null)
            {
                readingLoop0.CaptureReady -= ReadingLoop_CaptureReady0;
                readingLoop0.Failed -= ReadingLoop_Failed0;
                readingLoop0.Dispose();
            }
            
            if (readingLoop1 != null)
            {
                readingLoop1.CaptureReady -= ReadingLoop_CaptureReady1;
                readingLoop1.Failed -= ReadingLoop_Failed1;
                readingLoop1.Dispose();
            }

            /*
            if (trackingLoop != null)
            {
                trackingLoop.BodyFrameReady -= TrackingLoop_BodyFrameReady;
                trackingLoop.Failed -= ReadingLoop_Failed;

                trackingLoop.Dispose();
            }*/

            if (depthCameraDataVar[0] != null)
                depthCameraDataVar[0].onClosing();
            if (depthCameraDataVar[1] != null)
                depthCameraDataVar[1].onClosing();

            //if (skeletalDataVar[0] != null)
            //    skeletalDataVar[0].onClosing();

            if (userSensorGridVar != null)
                userSensorGridVar.onClosing();

 
        }
    }
    
}
