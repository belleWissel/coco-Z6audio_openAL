using System;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input; // mouse and keyboard input
//using OpenTK.Audio;
//using OpenTK.Audio.OpenAL; // for Audio effects

// for config file
//using System.Collections;
using System.Collections.Specialized;

// for app event handlers
using SecondstoryCommon;

using AudioControlApp.CameraViewControl;
//using SensorControlApp.OpenGLProgrammablePipeline;
using AudioControlApp.Utils;

//using SensorControlApp.DepthCamera;
//using SensorControlApp.K4ACamera;
//using AudioControlApp.RealSenseCamera;

using AudioControlApp.WallCommunicationsAndControl;
using AudioControlApp.USBInspection;

using AudioControlApp.DisplayEnvironmentModel;
using AudioControlApp.ReactiveAreaManagement;

using AudioControlApp.UserInputControl;

//using SensorControlApp.AudioSystems;

using System.Timers;
using OpenTKAudio.Engine; // high(er) performance timer

namespace MainApp
{
    partial class MyWindow : GameWindow
    {

        #region variables

        //***************************************
        // global kiosk states and options
        //***************************************
        public static bool debugMode = true;
        public static bool graphicsLightMode;
        private static bool runMinimized = false;

        private string communicationsIP;
        private int communicationsPort1;
        private int communicationsPort2;


        //private static bool isMultiCamera = true;
        private static int maxNumberOfCameras = 2; // needs to match RSCamControl

        private int kioskID = -1; // which displays is this app responsible for?
        private bool ignoreEventsUntilInitialized = true;
        private int ignoreEventsUntilInitializedCounter = 0;

        Matrix4 projectionMatrix, modelviewMatrix;

        //***************************************
        // class variables
        //***************************************

        // DRAWING ELEMENTS:
        //private static int depthDataTransmitRate = 30; // 33 fps
        //private static int depthDataTransmitRate = 33; // 30 fps
        //private static int depthDataTransmitRate = 67; // 15 fps
        //private static int depthDataTransmitRate = 50; // 20 fps
        //private static int depthDataTransmitRate = 1000; // every 1 sec for testing purposes
        //private RSCameraControl02 depthCameraControlVar;
        //private RSCameraControlDualCam03 depthCameraControlVar;

        // UTILITIES:
        private SceneViewCameraControl SceneViewCameraControlVar;
        private DrawGridOnScreen drawGridPlaneVar;
        private AreaActivationControl areaActivationTrackerVar;
        private KeyboardInputCtrl keyboardControlVar;
        private LoadCameraAndDataSettingsFromXML getCameraDataVar;

        //***************************************
        // timing utilities
        //***************************************
        private DrawFPSToScreen showFPSVar; // has to be true to calculate fps
        private DrawHUDToScreen showHUDVar;
 
        HiPerfTimer fpsTimer;
        private int fpsFrameCounter;
        private float fpsEstimate;

        HiPerfTimer actualFpsTimer;
        private int actualFpsFrameCounter;
        private float actualFpsEstimate;

        private int animationFrameCounter;
        private int animationFrameDivAthousand;

        // IF graphics are running without frame lock, timer is needed to throttle animations/updates so they run at approx 60fps
        //HiPerfTimer throttleGrahicsUpdateTimer;
        //private double graphicsUpdateTime = 0.016667; // force it to run at abour 60fps
        //private long graphicsUpdateTimeL = 100; // force it to run at abour 60fps

        /// <summary>
        /// Timer to run garbage collection
        /// </summary>
        private System.Windows.Forms.Timer collectTimer;

        //***************************************
        // scene view variables
        //***************************************
        private bool draw2D = false;
        private bool readyToDraw = false;
        private bool doShowFPS = false;
        private bool doShowGrid = true; // used for debug mode
        private bool doShowReactiveAreas = true;
        private bool drawDepthPoints = true;
        private bool drawUserSensorGrid = true;
        private bool doDrawHUD = true;
        private bool doShowHelp = false;

        private bool fullScreenMode = false;
        private int targetWindowW = 1920; // when at full screen
        private int targetWindowH = 1080; // when at full screen
        private int targetWindowPosnX = 0;
        private int targetWindowPosnY = 0;

        static int floatingWindowW = 400;
        static int floatingWindowH = 300;
        static int floatingWindowPosX = 50;
        static int floatingWindowPosY = 50;
        public static float globalScale; // pixel scale
        private float twoDdrawingScale = 1.0f; // scale when drawing flat view

        private double globalPixelScale = 0.7456; // this is physical width of display divided by number of pixels 5120 pixels/6858 mm

        public static float globalDisplayRatio = floatingWindowW / floatingWindowH; // screen width/height

        private float cameraFOVdeg = 120.0f; // set this to appropriate value
        private float cameraFOVrad; // compute this on init


        // used for orthogrphic projection:
        Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 1500.0f);
        // used for both orthogrphic and perspective projection:
        Vector3 cameraLookAtPosition = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

        private float cameraNearClip = 1.0f;
        private float cameraFarClip = 27500.0f;

        //***************************************
        // comunications variables
        //***************************************
        public static string kioskName = "AudioControlApp";
        //public static string controlKioskName;
        //public static string backgroundKioskName;
        public static string kioskIPaddress;
        private InterComputerClient interComputerClient1;
        private InterComputerClient interComputerClient2;
        public static string validIPStringTest = "192";

        public static string EVENTLOG_SOURCENAME = "Audio Control App";

        //***************************************
        // checking status of connected cameras
        //***************************************
        private CheckConnected checkUSBConnectionsVar;
        
        
        //***************************************
        // camera disconnected status indicators:
        //***************************************
        //private bool depthCameraIntializationFailed = true;
        //private string missingCameraReportToScreen = "";
        //private bool didReportCameraLoss = false; // used to flag when camera is disconnected
        //private int numberOfCamerasExpected = 1;


        //***************************************
        // display 3d model of environment
        //***************************************
        //private bool doLoadLocalOBJFile = false;
        //private string threeDModelFile = "models\\cube2.obj";
        //private DisplayEnvironmentControl displayModelVar;

        //***************************************
        // user input controls (new to OpenTK 3.0)
        //***************************************
        private bool mouseIsDown = false;
        private int currentMouseX = 0;
        private int currentMouseY = 0;
        private int currentMouseWheel = 0;
        // debugging key and mouse input over VNC:
        private string addToFeedback = "";
        private bool doUseVNCCompatibility = false;


        //***************************************
        // FileIO 
        //***************************************
        private bool isUsingLocalFileIO = false;
        private bool isRecordingToFile = false;
        private bool isPlayingFromFile = false;
        private string localDataFileName = "sampleData.xml";

        //***************************************
        // Audio
        //***************************************
        private AudioControl audioControl;

        private string sharedAssetFolderPath;
        //private AudioControl myAudioControl01;
        
        #endregion variables


        /// <summary>Creates a window with the specified title.</summary>
        public MyWindow()
            : base(floatingWindowW, floatingWindowH, // width, height
            new GraphicsMode(32, 24, 0, 16), //32bpp (8 bits per channel, like specified above), 24 bit depth buffer, no stencil buffer needed, 16 samples for antialiasing
            "BWCO Audio Control", // window title
            0, // game window flags
            DisplayDevice.Default, 3, 1, // use the default display device, request a 3.1 OpenGL context
            GraphicsContextFlags.Default) //this will help us track down bugs
        {
            VSync = VSyncMode.On; // YOU HAVE TO ADJUST SETTING IN OpenGLAppEntryPoint AS WELL
            //VSync = VSyncMode.Off;
            if (doUseVNCCompatibility)
            {
                // doubling up bc OpenTK.input does not work over VNC connection...
                this.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
                this.MouseDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
                this.MouseUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);
                this.MouseMove += new EventHandler<MouseMoveEventArgs>(Mouse_Move);
                this.MouseWheel += new EventHandler<MouseWheelEventArgs>(Mouse_Wheel);
            }
        }

        #region ONLOAD

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            System.Drawing.Icon myIcon = new System.Drawing.Icon("SensorCtrl.ico");
            Icon = myIcon;

            readWallIDFromConfigFile();

            fpsTimer = new HiPerfTimer();
            actualFpsTimer = new HiPerfTimer();
            //throttleGrahicsUpdateTimer = new HiPerfTimer();

            globalDisplayRatio = ((float)ClientSize.Width / (float)ClientSize.Height);  // use the screens aspect ratio to determine new limits of view

            SceneViewCameraControlVar = new SceneViewCameraControl(ClientSize.Width, ClientSize.Height);
            SceneViewCameraControlVar.init();
            
            //depthCameraControlVar = new RSCameraControlDualCam03(depthDataTransmitRate);

            keyboardControlVar = new KeyboardInputCtrl(doUseVNCCompatibility);
            getCameraDataVar = new LoadCameraAndDataSettingsFromXML();

            resetMouseInputs();

            // ************************************************************************
            // define graphics classes here:
            drawGridPlaneVar = new DrawGridOnScreen(5000f, 5000f, 11);
            showFPSVar = new DrawFPSToScreen();
            showHUDVar = new DrawHUDToScreen();

            // ************************************************************************
            // call this before initializing display components
            readConfigFile();
            //************************
            //if (doLoadLocalOBJFile)
            //    displayModelVar = new DisplayEnvironmentControl(threeDModelFile);

            interComputerClient1 = new InterComputerClient(communicationsIP, communicationsPort1);
            interComputerClient1.OnCommand += new ClientCommandEventHandler(interComputerClient_OnCommand1);
            interComputerClient2 = new InterComputerClient(communicationsIP, communicationsPort2);
            interComputerClient2.OnCommand += new ClientCommandEventHandler(interComputerClient_OnCommand2);
            areaActivationTrackerVar = new AreaActivationControl();


            // position window according to values in config file:
            // NATIVE WINDOW PROPERTIES:
            Width = MyWindow.floatingWindowW;
            Height = MyWindow.floatingWindowH;
            X = floatingWindowPosX;
            Y = floatingWindowPosY;

            // set up intitial view:

            cameraFOVrad = cameraFOVdeg * (float)Math.PI / 180.0f;

            SetProjectionMatrix(Matrix4.CreatePerspectiveFieldOfView(cameraFOVrad, globalDisplayRatio, cameraNearClip, cameraFarClip));
            SetModelviewMatrix(Matrix4.LookAt(cameraPosition, cameraLookAtPosition, cameraUpVector));


            // ************************************************************************
            // intialize display components:
            drawGridPlaneVar.initOpenGL();
            showFPSVar.initOpenGL();
            showHUDVar.initOpenGL();

            //depthCameraControlVar.initOpenGL();
            // user sensor grid was established when we read config file, so initOpenGL on userSensorGrid too.
            //depthCameraControlVar.initOpenGLUserSensorGrid();

            //displayModelVar.initOpenGL((float)globalPixelScale);
            //if (doLoadLocalOBJFile)
            //    displayModelVar.initOpenGL(1000f);

            // ************************************************************************
            // intialize cameras:
            //bool doConnectToCameras = depthCameraControlVar.checkForConnectedFromMain(); // have moved this function to the camera control...

            // ************************************************************************
            // now attempt to connect to cameras:

            /*
            if (doConnectToCameras)
            {
                try
                {
                    if (depthCameraControlVar.initDepthCameraDevice())
                    {
                        depthCameraControlVar.initOpenGLComponents();
                        depthCameraIntializationFailed = false;
                    }
                    else
                    {
                        depthCameraIntializationFailed = true;

                        System.Diagnostics.Debug.WriteLine("[MAIN] cameras failed to start...");
                        writeToEventLog("[MainApp] Camera initialization failed at program startup");
                    }
                }
                catch (Exception exc)
                {
                    depthCameraIntializationFailed = true;

                    System.Diagnostics.Debug.WriteLine("[MAIN] cameras failed to start (exception caught)... "+exc.Message);
                    writeToEventLog("[MainApp] Caught exception during camera initialization");
                }
            }*/

            /*
            if (depthCameraIntializationFailed)
            {
                // special case if we are running from a local file
                if (isPlayingFromFile)
                {
                    depthCameraControlVar.initOpenGLComponents();
                    System.Diagnostics.Debug.WriteLine("[MAINAPP] CAMERA FAILURE: but proceeding with recorded file playback ");
                }
                else
                {
                    depthCameraControlVar.haltDataTransmission();
                    System.Diagnostics.Debug.WriteLine("[MAINAPP] CAMERA FAILURE: HALTING DATA TRANSMISSION ");
                }
            }*/
            
            // finished connecting to cameras
            // ************************************************************************

            // Other state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.0f, 0.0f, 0f, 1.0f);
            //GL.ClearColor(1f, 1f, 1f, 1.0f);
            //GL.ClearColor(0.5f, 0.5f, 0.6f, 1.0f);

            // moved from draw loop to avoid redundant calls:
            GL.CullFace(CullFaceMode.Back);
            //GL.Enable(EnableCap.CullFace); // will not draw backs of polygons
            GL.Disable(EnableCap.CullFace); // will draw backs of polygons

            // set this here to remove redundant calls later:
            GL.Enable(EnableCap.Blend);
             // new openTK:
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            readyToDraw = true;

            if (!debugMode) // skip to full screen, hide mouse, etc
            {
                System.Windows.Forms.Cursor.Hide(); // hide the mouse

            }

            if (graphicsLightMode)
            {
                doShowGrid = false;
                doShowFPS = false;
                drawDepthPoints = false;
                drawUserSensorGrid = false;
            }
            else
            {
                doShowGrid = true;
                doShowFPS = true;
                drawDepthPoints = true;
                drawUserSensorGrid = true;
            }


            /*
            try
            {
                depthCameraControlVar.userSensorGridVar.reactiveAreasVar.OnEvent += new AppEventHandler(reactiveArea_OnEvent);
                areaActivationTrackerVar.assignActualNumberOfAreas(depthCameraControlVar.userSensorGridVar.reactiveAreasVar.actualNumberOfReactiveRegions);
                areaActivationTrackerVar.assignAreaStartIndex(depthCameraControlVar.userSensorGridVar.reactiveAreasVar.regionStartIndex);
                
            }
            catch
            {
                Console.WriteLine("error connecting to videoSensorGrid " + e);
            }*/

            // use this to throttle graphics update
            //graphicsUpdateTimeL = throttleGrahicsUpdateTimer.getLongLimit(graphicsUpdateTime);

            //throttleGrahicsUpdateTimer.Start();
            areaActivationTrackerVar.init();

            collectTimer = new System.Windows.Forms.Timer();
            collectTimer.Interval = 60000;
            collectTimer.Tick += new EventHandler(collecTimer_Tick);
            collectTimer.Start();

            interComputerClient1.requestConnections();
            interComputerClient2.requestConnections();

            audioControl = new AudioControl(kioskID, sharedAssetFolderPath); // kiosk name identifies which 

            audioControl.initialize();

            resetAllActivationAreas();

            //myAudioControl00 = new AudioControl(0);
            //myAudioControl01 = new AudioControl(1);
            //myAudioControl00.initialize();
            //myAudioControl01.initialize();
        }

        #endregion ONLOAD

        // ******************************************************************
        // ******************************************************************
        // draw and update commands

        #region draw and update
        private void SetModelviewMatrix(Matrix4 matrix)
        {
            modelviewMatrix = matrix;
        }

        private void SetProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrix = matrix;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            checkForGraphicsUpdate();

            // check for mouse/keyboard changes:
            if (this.Focused) // but only if the window has focus
            {
                MouseState mState = Mouse.GetCursorState();
                checkForMouseEvents(mState);
                KeyboardState kState = Keyboard.GetState();
                Key keyStrike = keyboardControlVar.checkForKeyboardEvents(kState);
                if (keyStrike != Key.Unknown)
                {
                    processKeyStrike(keyStrike);
                }
            }

            // UTILITIES:
            if (doShowFPS) // used for fps calculation
            {
                fpsFrameCounter += 1;
                if (fpsFrameCounter >= 10)
                {
                    fpsTimer.Stop();
                    fpsEstimate = (float)fpsFrameCounter / (float)fpsTimer.Duration; // number of frames past divided by time elapsed

                    fpsFrameCounter = 0;
                    fpsTimer.Start();
                }
            }
        }

        private void checkForGraphicsUpdate()
        {
            // (place any custom graphics synchronization here (and return if not ready to update animations) )
            //if (throttleGrahicsUpdateTimer.ElapsedL() > graphicsUpdateTimeL)
            //{
            // throttleGrahicsUpdateTimer.Stop();

            updateGraphics();
            //sendActivationStatusIfNeeded();
            
            
            
            if (doShowFPS) // used for fps calculation
            {
                actualFpsFrameCounter += 1;
                if (actualFpsFrameCounter >= 100)
                {
                    actualFpsTimer.Stop();

                    actualFpsEstimate = (float)actualFpsFrameCounter / (float)actualFpsTimer.Duration;
                    actualFpsFrameCounter = 0;

                    actualFpsTimer.Start();
                }

                
                if ((actualFpsEstimate < 1) || (actualFpsEstimate > 100))
                {
                    showFPSVar.update(kioskName + "\nunlocked fps: " + fpsEstimate + "\nlocked fps: (calculating)\nframe: " + animationFrameDivAthousand + " (x100) ");
                }
                else
                {
                    //showFPSVar.update("unlocked fps: " + fpsEstimate + "\nlocked fps: " + actualFpsEstimate + "\nframe: " + animationFrameDivAthousand + " (x100)\nlast packet size: " + depthCameraControlVar.transmitPackageSize);
                    showFPSVar.update(kioskName + "\nunlocked fps: " + fpsEstimate + "\nlocked fps: " + actualFpsEstimate + "\nframe: " + animationFrameDivAthousand + " (x100)");
                }

            }

            //     throttleGrahicsUpdateTimer.Start();
            //}
        }


        private void updateGraphics()
        {
            if (ignoreEventsUntilInitialized) // custom delay for initialization of program and classes:
            {
                ignoreEventsUntilInitializedCounter += 1;
                if (ignoreEventsUntilInitializedCounter > 30)
                {
                    ignoreEventsUntilInitialized = false;
                    if (runMinimized)
                        this.WindowState = WindowState.Minimized;
                    if (kioskID == 0)
                        broadcastAmbientModeStart();

                }
            }

            audioControl.update();
            
            // force update of 3d view cameraPosition:
            if (!draw2D)
            {
                int doUpdateCamera = SceneViewCameraControlVar.plotCameraPos();
            }

            //depthCameraControlVar.update();

            /*
            if (!graphicsLightMode)
            {
                if ((drawDepthPoints) || (drawUserSensorGrid))
                {
                    depthCameraControlVar.checkForDataUpdateForGraphics();
                }
            }*/

            if (doShowFPS)
            {
                // UTILITIES:
                animationFrameCounter += 1;
                if (animationFrameCounter > 100)
                {
                    animationFrameDivAthousand += 1;
                    animationFrameCounter = 0;
                    if (animationFrameDivAthousand > 1000) // reset 
                    {
                        animationFrameDivAthousand = 0;
                    }
                }
            }

            if (doDrawHUD)
            {
                updateOnScreenInformation();
            }

        }

        private void updateOnScreenInformation()
        {
            /*
            int numberOfDepthPointsToTransmit = (int)Math.Round((double)depthCameraControlVar.userSensorGridVar.validUserMeshPointCounter);

            string currentPntCount = "Current User Grid Pionts: " + numberOfDepthPointsToTransmit + "\n";
            string currentConnectionStatus0 = "network status: disabled\n";
            string currentConnectionStatus1 = "number of connected: 0\n";
            bool currentConnectionStatusFlag0 = depthCameraControlVar.userSensorGridVar.connectionStatus;
            int numberOfConnected = depthCameraControlVar.userSensorGridVar.connectionCount;

            if (currentConnectionStatusFlag0)
            {
                currentConnectionStatus0 = "network status: enabled  \n";
            }

            if (numberOfConnected > 0)
            {
                currentConnectionStatus1 = "number of net connections: " + numberOfConnected + "\n";
            }

            string cameraReport = "";
            string missingCameraText = "";
            string cameraInitFailed = "";
            string activationState = "";
            */
            
            /*
            //if (depthCameraIntializationFailed)
            //{
            //    cameraInitFailed = "camera initialization failed\ncheck camera connections and restart computer\n";
            //    cameraInitFailed += "cameras missing @ startup: " + missingCameraReportToScreen;
            //}
            //else
            //{
                cameraReport = "Cam1: is on:" + depthCameraControlVar.cameraIsActive[0] + " SN "+depthCameraControlVar.listOfDevSN[0] + " FW "+depthCameraControlVar.listOfDevFW[0] + " \n";
                cameraReport += "Cam2: is on:" + depthCameraControlVar.cameraIsActive[1] + " SN "+depthCameraControlVar.listOfDevSN[1] + " FW "+depthCameraControlVar.listOfDevFW[1] + " \n";
                //cameraReport += "Cam2: connected:" + checkUSBConnectionsVar.isConnected[1] + " is on:" + depthCameraControlVar.cameraIsActive[1] + "\n";
                //cameraReport += "Cam3: connected:" + checkUSBConnectionsVar.isConnected[2] + " is on:" + depthCameraControlVar.cameraIsActive[2] + "\n";
                //cameraReport += "Cam4: connected:" + checkUSBConnectionsVar.isConnected[3] + " is on:" + depthCameraControlVar.cameraIsActive[3] + "\n";

                // TODO not implemented in cameraControl:
                //if (depthCameraControlVar.currentlyConnectedDeviceCount != depthCameraControlVar.deviceCountAtLaunch)
                //{
                //    missingCameraText = "a camera has become disconnected\ncheck camera connections and restart computer\n";
                //    reportDisconnectedCameraToEventLog();
                //}

                //cameraReport += "devices dectected on USB bus: " + checkUSBConnectionsVar.numberOfDevices + " \n";
                cameraReport += "devices dectected by RS scan: " + depthCameraControlVar.actualNumberOfDepthCameras + " \n";
                cameraReport += "devices with devpath match and activated in config: " + depthCameraControlVar.currentlyConnectedAndActiveDeviceCount + " \n";
                cameraReport += depthCameraControlVar.fpsReport;
            //}
            */
            
            string UDPConnectionsReport = IPConnectionReport;

            
            
            //activationState = areaActivationTrackerVar.currentActivationFeedback;

            // this is the string of feedback on screen from activation tracker...
            /*string textToSendToHUD = areaActivationTrackerVar.currentActivationFeedback + "\n";
            textToSendToHUD += audioDevReport;
            
            if (cameraInitFailed != "")
            {
                textToSendToHUD += cameraInitFailed;
            }
            else if (missingCameraText != "")
            {
                textToSendToHUD += missingCameraText;
            }
            else
                textToSendToHUD += currentConnectionStatus0 + currentConnectionStatus1 + cameraReport + UDPConnectionsReport;
            //textToSendToHUD += currentPntCount + currentConnectionStatus0 + currentConnectionStatus1 + cameraReport;
            */
            
            string audioDevReport = audioControl.getAudioDevReport();
            string textToSendToHUD = audioDevReport;

            textToSendToHUD += sensingStatusReport;
            
            showHUDVar.updateWithText(textToSendToHUD);
        }

        private int IPcount = 0;
        private string[] collectionOfConnectedIPs = new string[10];
        private string IPConnectionReport = "\n no connections...";
        private void updateConnectedFeedback(string whichNewIP)
        {
            collectionOfConnectedIPs[IPcount] = whichNewIP;
            
            IPcount += 1;
            IPConnectionReport = "\n connected to: ";
            for (int i = 0; i < IPcount; ++i)
            {
                IPConnectionReport += collectionOfConnectedIPs[i] + ", ";
            }

            IPConnectionReport += "\n";
        }
        
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!readyToDraw)
                return;

            drawAllSceneObjects();

            if (doShowFPS)
                showFPSVar.draw();

            //if (sensingIsDirty())
            //{
                //checkForCenterActivation();
                checkForDeepActivation();
            //}
            GL.Flush();
            SwapBuffers();
        }

        private void enableSmoothing()
        {
            //GL.Enable(EnableCap.PointSmooth);
            GL.Enable(EnableCap.LineSmooth);
            //GL.Enable(EnableCap.PointSmooth);
            //GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
            //GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            //GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);

        }

        private void disableSmoothing()
        {
            GL.Disable(EnableCap.PointSmooth);
            GL.Disable(EnableCap.LineSmooth);
            GL.Disable(EnableCap.PointSmooth);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Fastest);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Fastest);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Fastest);
        }

        private void drawAllSceneObjects()
        {
            /*
            if (isUsingLocalFileIO)
            {
                if (depthCameraControlVar.getFileRecordingStatus())
                    GL.ClearColor(0.3f, 0.0f, 0f, 1.0f); // add red tint
                else
                    GL.ClearColor(0.0f, 0.0f, 0f, 1.0f);
            }*/
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Vector3 cameraPositionForWorldSpace = SceneViewCameraControlVar.getCameraPosition();
            Vector3 cameraTargetForWorldSpace = SceneViewCameraControlVar.getCameraTarget();

            // moved this to OPENGL INIT to avoid redundant calls:
            /*GL.CullFace(CullFaceMode.Back);
            //GL.Enable(EnableCap.CullFace); // will not draw backs of polygons
            GL.Disable(EnableCap.CullFace); // will draw backs of polygons
            */

            if (draw2D)
            {
                float currentWidth = Width;
                float depth = 1084f / globalDisplayRatio; // TODO determine what this 1084 number is about
                float scaledDepth = depth;
                float x = 0.0f;
                float y = 0.0f;
                cameraPositionForWorldSpace = new Vector3(x, y, scaledDepth);
                cameraTargetForWorldSpace = new Vector3(x, y, 0f);
            }
            else
            {
                SetModelviewMatrix(Matrix4.LookAt(SceneViewCameraControlVar.getCameraPosition(), SceneViewCameraControlVar.getCameraTarget(), cameraUpVector));
            }

            //if (doLoadLocalOBJFile)
            //    displayModelVar.draw(modelviewMatrix, projectionMatrix);

            if (doShowGrid)
            {
                drawGridPlaneVar.draw(modelviewMatrix, projectionMatrix);
                
            }

            
            /*
            if (doShowReactiveAreas)
            {
                depthCameraControlVar.drawReactiveAreas(modelviewMatrix, projectionMatrix);
            }

            if (!graphicsLightMode)
            {
                if (drawDepthPoints)
                {
                    depthCameraControlVar.drawCameraData(modelviewMatrix, projectionMatrix);
                }

                if (drawUserSensorGrid)
                {
                    depthCameraControlVar.drawUserGridData(modelviewMatrix, projectionMatrix);

                    //depthCameraControlVar.drawUserSkeletalData(modelviewMatrix, projectionMatrix);
                }
            }*/

            if (doDrawHUD)
                showHUDVar.draw();
            
            if (doLaunchAmbientLoopOnNextUpdate)
            {
                audioControl.startAmbientLoops();
                doLaunchAmbientLoopOnNextUpdate = false;
            }
            
            if (doHaltAmbientLoopOnNextUpdate)
            {
                audioControl.stopAmbientLoops();
                doHaltAmbientLoopOnNextUpdate = false;
            }
        }

        private bool doLaunchAmbientLoopOnNextUpdate = false;
        private bool doHaltAmbientLoopOnNextUpdate = false;
        
        protected override void OnResize(EventArgs e)
        {
            int newWidth = ClientSize.Width;
            int newHeight = ClientSize.Height;

            globalDisplayRatio = (float)newWidth / (float)newHeight;
            globalScale = (float)newHeight;

            SceneViewCameraControlVar.setWindowSize(newWidth, newHeight);

            GL.Viewport(0, 0, Width, Height);

            // force update to projection matrix:
            draw2D = !draw2D;
            toggle2DdrawingMode();

            showFPSVar.onWindowResize((float)newWidth, (float)newHeight, globalDisplayRatio, globalScale, twoDdrawingScale);
            showHUDVar.onWindowResize((float)newWidth, (float)newHeight, globalDisplayRatio, globalScale, twoDdrawingScale);
        }
        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);
            if (this.Focused) // it just regained focus
            {
                //resetMouseValues
                resetMouseInputs();
            }
        }
        #endregion draw and update


        // ******************************************************************
        // ******************************************************************
        // toggles

        #region toggles

        private void broadcastAmbientModeStart()
        {
            interComputerClient1.sendStartAmbientMode();
        }
        private void broadcastAmbientModeStop()
        {
            interComputerClient1.sendStopAmbientMode();
        }

        private void toggle2DdrawingMode()
        {
            if (draw2D) // is currently drawing in 2D - switch to 3D (live camera)
            {
                SetProjectionMatrix(Matrix4.CreatePerspectiveFieldOfView(cameraFOVrad, globalDisplayRatio, cameraNearClip, cameraFarClip));
                SetModelviewMatrix(Matrix4.LookAt(SceneViewCameraControlVar.getCameraPosition(), SceneViewCameraControlVar.getCameraTarget(), cameraUpVector));

                draw2D = false;
            }
            else // is currently drawing in 3D - switch to 2D (orthographic)
            {
                SetProjectionMatrix(Matrix4.CreateOrthographic(globalDisplayRatio * globalScale * twoDdrawingScale, globalScale * twoDdrawingScale, cameraNearClip, cameraFarClip));
                SetModelviewMatrix(Matrix4.LookAt(cameraPosition, cameraLookAtPosition, cameraUpVector));

                draw2D = true;
            }
        }

        private void toggleFullWindowMode()
        {
            fullScreenMode = !fullScreenMode;
            if (!fullScreenMode) // switch back to windowed mode
            {
                // adjust "Native Window" properties
                WindowState = WindowState.Normal;
                WindowBorder = WindowBorder.Resizable;

                Width = MyWindow.floatingWindowW;
                Height = MyWindow.floatingWindowH;
                X = MyWindow.floatingWindowPosX;
                Y = MyWindow.floatingWindowPosY;
            }
            else // switch to fullscreen (target window size/position from config file)
            {
                WindowBorder = WindowBorder.Hidden;
                Width = targetWindowW;
                Height = targetWindowH;
                X = targetWindowPosnX;
                Y = targetWindowPosnY;
            }
        }


        private void toggleFPSDisplay()
        {
            doShowFPS = !doShowFPS;
        }

        private void toggleGridDisplay()
        {
            doShowGrid = !doShowGrid;
        }
        private void toggleReactiveAreaDisplay()
        {
            doShowReactiveAreas = !doShowReactiveAreas;
        }
        private void toggleMouseDisplay()
        {
            System.Windows.Forms.Cursor.Show(); // show the mouse
        }

        /*
        private void toggleLearnBackground() // applies min/max elevation filter from app.config file
        {
            //depthCameraControlVar.applyLearnBackground();
        }
       

        private void toggleCalibiration()
        {
            //for (int i = 0; i < depthCameraControlVar.maxNumberOfDepthCameras; ++i)
            //{
            //    if (depthCameraControlVar.depthCameraDataVar[i] != null)
            //        depthCameraControlVar.depthCameraDataVar[i].initiateCalibration();
            //}
        }
         */
        /*
        private void saveBgThreshold()
        {
            string path = "threshold_";
            for (int i = 0; i < depthCameraControlVar.actualNumberOfDepthCameras; ++i)
            {
                if (depthCameraControlVar.depthCameraObjectVar[i] != null)
                    depthCameraControlVar.depthCameraObjectVar[i].saveBackgroundCapture(path + i + ".bin");
            }
        }

        private void loadBgThreshold()
        {
            string path = "threshold_";
            for (int i = 0; i < depthCameraControlVar.actualNumberOfDepthCameras; ++i)
            {
                if (depthCameraControlVar.depthCameraObjectVar[i] != null)
                    depthCameraControlVar.depthCameraObjectVar[i].loadBackgroundCapture(path + i + ".bin");
            }
        }
        */

        
        private void toggleRecordingMode()
        {
            //depthCameraControlVar.togglePlayingOrRecording();
        }

        private void toggleFloorCeilFilterOnTransformedData()
        {
            //depthCameraControlVar.toggleSensorFloorCeil();
        }

        private void toggleDepthPointDisplay()
        {
            drawDepthPoints = !drawDepthPoints;
            System.Diagnostics.Debug.WriteLine("toggling point display to :" + drawDepthPoints);
        }
        private void toggleHUD()
        {
            doDrawHUD = !doDrawHUD;
        }

        private void toggleHelp()
        {
            doShowHelp = !doShowHelp;
        }
        private void toggleUserSensorGridDisplay()
        {
            drawUserSensorGrid = !drawUserSensorGrid;
            System.Diagnostics.Debug.WriteLine("toggling user grid display to :" + drawUserSensorGrid);
        }

        /*
        private void toggleGroundFilter() // applies min/max elevation filter from app.config file
        {
            depthCameraControlVar.applyGroundFilter();
        }
        private void toggleGroundFilter(bool forceItTo) // applies min/max elevation filter from app.config file
        {
            depthCameraControlVar.applyGroundFilter(forceItTo);
        }
        */
        #endregion toggles



        // ******************************************************************
        // ******************************************************************
        #region mouseAndKeyInput


        private void resetMouseInputs()
        {
            MouseState mState = Mouse.GetCursorState();
            currentMouseWheel = mState.Wheel;
            currentMouseX = mState.X;
            currentMouseY = mState.Y;
        }

        private void checkForMouseEvents(MouseState whichMouseState)
        {
            //if (!this.Focused) // only react to mouse events if the graphics window has focus
            //    return;
            

            int mousePosX = whichMouseState.X;
            int mousePosY = whichMouseState.Y;
            int mouseWheel = whichMouseState.Wheel;

            bool isMoved = false;
            if (whichMouseState.IsButtonDown(MouseButton.Left))
            {
                if (mouseIsDown) // was it already down before?
                {
                    // has it moved?
                    if (mousePosX != currentMouseX)
                        isMoved = true;
                    if (mousePosY != currentMouseY)
                        isMoved = true;

                    if (isMoved)
                    {
                        currentMouseX = mousePosX;
                        currentMouseY = mousePosY;
                        SceneViewCameraControlVar.mouseMove(currentMouseX, currentMouseY);
                    }
                }
                else
                {
                    if (!mouseIsDown) // it wasn't down before
                    {
                        mouseIsDown = true;
                        SceneViewCameraControlVar.mouseDn(mousePosX, mousePosY);
                    }
                    else
                    {

                    }
                }
            }
            else // mouse button is not down
            {
                if (mouseIsDown) // was it down previously?
                {
                    mouseIsDown = false;
                    SceneViewCameraControlVar.mouseUp();
                }
            }

            if (mouseWheel != currentMouseWheel)
            {                
                //if (this.Focused) // ignore mousewheel events outside of program window:
                //{
                    //System.Diagnostics.Debug.WriteLine("[MAIN] Mouse_Wheel "+ mouseWheel);
                    SceneViewCameraControlVar.mouseWheel(mouseWheel - currentMouseWheel);
                    currentMouseWheel = mouseWheel;
                //}
            }
            //SceneViewCameraControlVar.mouseDn(e.X, e.Y);
            //SceneViewCameraControlVar.mouseUp();
            //SceneViewCameraControlVar.mouseMove(e.X, e.Y);
        }
        // ************************************************************************
        // OLD MOUSE EVENT STUFF
         void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("[MAIN] Mouse_ButtonDown ");
            SceneViewCameraControlVar.mouseDn(e.X, e.Y);
            addToFeedback += "m";
        }
        
        
        void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("[MAIN] Mouse_ButtonUp ");
            SceneViewCameraControlVar.mouseUp();
        }



        void Mouse_Move(object sender, MouseMoveEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("[MAIN] Mouse_Move ");
            SceneViewCameraControlVar.mouseMove(e.X, e.Y);
        }
        void Mouse_Wheel(object sender, MouseWheelEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("[MAIN] Mouse_Wheel delta: " + e.Delta);
            SceneViewCameraControlVar.mouseWheel(e.Delta);
        }
        void processKeyStrike(Key whichKey)
        {
            switch (whichKey)
            {
                case OpenTK.Input.Key.Escape: exitProgram(); break;
                //case OpenTK.Input.Key.F2: toggle2DdrawingMode(); break;
                //case OpenTK.Input.Key.F3: toggleFullWindowMode(); break;
                case OpenTK.Input.Key.Z: toggle2DdrawingMode(); break;
                case OpenTK.Input.Key.X: toggleFullWindowMode(); break;

                case OpenTK.Input.Key.F: toggleFPSDisplay(); break;
                case OpenTK.Input.Key.G: toggleGridDisplay(); break;
                //case OpenTK.Input.Key.H: toggleReactiveAreaDisplay(); break;
                case OpenTK.Input.Key.M: toggleMouseDisplay(); break;

                /*
                case OpenTK.Input.Key.P: toggleDepthPointDisplay(); break;
                case OpenTK.Input.Key.Q: toggleUserSensorGridDisplay(); break;

                // SENSOR CALIBRATION
                case OpenTK.Input.Key.Number1: beginSensorAdjust(0); break;
                case OpenTK.Input.Key.Number2: beginSensorAdjust(1); break;
                case OpenTK.Input.Key.Number3: beginSensorAdjust(2); break;
                case OpenTK.Input.Key.Number4: beginSensorAdjust(3); break;

                case OpenTK.Input.Key.Left: adjustSensorPosnX(true, false); break;
                case OpenTK.Input.Key.Right: adjustSensorPosnX(false, false); break;
                case OpenTK.Input.Key.Up: adjustSensorPosnZ(true, false); break;
                case OpenTK.Input.Key.Down: adjustSensorPosnZ(false, false); break;
                case OpenTK.Input.Key.Keypad8: adjustSensorPosnY(true, false); break;
                case OpenTK.Input.Key.Keypad2: adjustSensorPosnY(false, false); break;
                
                // SENSOR ROLL:
                case OpenTK.Input.Key.Keypad4: adjustSensorRotZ(true); break;
                case OpenTK.Input.Key.Keypad6: adjustSensorRotZ(false); break;

                // SENSOR ELEVATION
                case OpenTK.Input.Key.Keypad9: adjustSensorRotX(true); break;
                case OpenTK.Input.Key.Keypad3: adjustSensorRotX(false); break;

                // SENSOR YAW
                case OpenTK.Input.Key.Keypad7: adjustSensorRotY(true); break;
                case OpenTK.Input.Key.Keypad1: adjustSensorRotY(false); break;
                */
                
                // testing testing
                case OpenTK.Input.Key.Space: 
                    //myAudioControl00.toggleSoundPlayback(); 
                    audioControl.toggleAmbientPlayback(); 
                    break;
                //case OpenTK.Input.Key.Space: GeneralAudio.PlaySound("0"); break;

                //case OpenTK.Input.Key.V: toggleFloorCeilFilterOnTransformedData(); break;
                //case OpenTK.Input.Key.B: GeneralAudio.PlaySound("2"); break;
                //case OpenTK.Input.Key.N: GeneralAudio.PlaySound("3"); break;
                //case OpenTK.Input.Key.R: toggleRecordingMode(); break;

                case OpenTK.Input.Key.A:
                    broadcastAmbientModeStart();
                    break;
                case OpenTK.Input.Key.S:
                    broadcastAmbientModeStop();
                    //audioControl.checkAudioDevAgain(); 
                    break;
                case OpenTK.Input.Key.B:
                    audioControl.setStateTo("build"); break;
                case OpenTK.Input.Key.N:
                    audioControl.setStateTo("nopresence"); break;
                case OpenTK.Input.Key.H:
                    audioControl.setStateTo("highlight"); break;
                case OpenTK.Input.Key.J:
                    audioControl.setStateTo("nopresence"); break;

            }
        }

        
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            
            keyboardControlVar.keyboardEntryFromWindow(e.Key);

            /*
            //System.Diagnostics.Debug.WriteLine("[MAIN] KeyDownHandler " + e.Key);
            switch (e.Key)
            {
                case OpenTK.Input.Key.Escape: exitProgram(); break;
                case Key.F2: toggle2DdrawingMode(); break;
                case Key.F3: toggleFullWindowMode(); break;

                case Key.F: toggleFPSDisplay(); break;
                case Key.G: toggleGridDisplay(); break;
                case Key.M: toggleMouseDisplay(); break;
                case Key.L: sendTestActivation(); break;
                case Key.K: sendTestDeactivation(); break;

                // SENSOR CALIBRATION
                case Key.Number1: beginSensorAdjust(0); break;
                case Key.Number2: beginSensorAdjust(1); break;
                case Key.Number3: beginSensorAdjust(2); break;
                case Key.Number4: beginSensorAdjust(3); break;

                case Key.Left: adjustSensorPosnX(true, false); break;
                case Key.Right: adjustSensorPosnX(false, false); break;
                case Key.Up: adjustSensorPosnZ(true, false); break;
                case Key.Down: adjustSensorPosnZ(false, false); break;
                case Key.Keypad8: adjustSensorPosnY(true, false); break;
                case Key.Keypad2: adjustSensorPosnY(false, false); break;

                // SENSOR ROLL:
                case Key.Keypad4: adjustSensorRotZ(true); break;
                case Key.Keypad6: adjustSensorRotZ(false); break;

                // SENSOR ELEVATION
                case Key.Keypad9: adjustSensorRotX(true); break;
                case Key.Keypad3: adjustSensorRotX(false); break;

                // SENSOR YAW
                case Key.Keypad7: adjustSensorRotY(true); break;
                case Key.Keypad1: adjustSensorRotY(false); break;


                case Key.Comma: adjustVFOV(true); break;
                case Key.Period: adjustVFOV(false); break;

                case Key.P: toggleDepthPointDisplay(); break;
                case Key.Q: toggleUserSensorGridDisplay(); break;

                case Key.A: sendSampleSoundRequest(); break;

                case Key.E: depthCameraControlVar.toggleDrawEdges(); break;
            }*/
        }
        

        #endregion mouseAndKeyInput
        // ******************************************************************
        // ******************************************************************
        #region sensorAdjustments

        private void beginSensorAdjust(int whichSensor)
        {
            //if (depthCameraControlVar.cameraIsActive[whichSensor]) // only allow adjustments to callibration if camera is active/connected
            //    depthCameraControlVar.ajustActiveSensorAdjustmentTo(whichSensor);
        }

        private void adjustHFOV(bool increaseIt)
        {
            //depthCameraControlVar.adjustHFOV(increaseIt);
        }
        private void adjustVFOV(bool increaseIt)
        {
            //depthCameraControlVar.adjustVFOV(increaseIt);
        }

        private void adjustSensorRotX(bool increaseAngle)
        {
            //depthCameraControlVar.rotateSensorX(increaseAngle);
        }

        private void adjustSensorRotY(bool increaseAngle)
        {
            //depthCameraControlVar.rotateSensorY(increaseAngle);

        }


        private void adjustSensorRotZ(bool increaseAngle)
        {
            //depthCameraControlVar.rotateSensorZ(increaseAngle);

        }

        private void adjustSensorPosnZ(bool increaseValue, bool jumpFar)
        {
            //depthCameraControlVar.moveSensorZ(increaseValue, jumpFar);
        }

        private void adjustSensorPosnX(bool increaseValue, bool jumpFar)
        {
            //depthCameraControlVar.moveSensorX(increaseValue, jumpFar);
        }

        private void adjustSensorPosnY(bool increaseValue, bool jumpFar)
        {
            //depthCameraControlVar.moveSensorY(increaseValue, jumpFar);
        }

        #endregion sensorAdjustments
        // ******************************************************************
        // ******************************************************************
        #region utils

        /// <summary>
        /// Force Garbage Collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void collecTimer_Tick(object sender, EventArgs e)
        {
            collect();
        }

        /// <summary>
        /// Force garbage collection
        /// </summary>
        void collect()
        {
            GC.Collect(0);
            GC.WaitForPendingFinalizers();
        }

        private void exitProgram()
        {
            System.Diagnostics.Debug.WriteLine("[MAIN] exiting program execution...");
            this.Exit();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            cleanUpGraphicsClasses();           
        }

        private void cleanUpGraphicsClasses()
        {
            // remove all VAOs, shader programs, etc.
            showFPSVar.onClosing();
            showHUDVar.onClosing();
            drawGridPlaneVar.onClosing();
            //depthCameraControlVar.onClosing();
            //if (doLoadLocalOBJFile)
            //    displayModelVar.exitApp();
            audioControl.onClosing();
            //myAudioControl01.onClosing();
            //if (isUsingAudio)
            //    GeneralAudio.exitApp();
        }


        #endregion utils


        // ******************************************************************
        // ******************************************************************
        #region readConfigFile

        
        private void readWallIDFromConfigFile()
        {
            NameValueCollection modeSettingsCollection = new NameValueCollection();
            NameValueCollection idCollection = new NameValueCollection();
            try
            {
                modeSettingsCollection = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("globalSettings/modes");
                idCollection = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("globalSettings/cartApplicationsList");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error in config file " + e);
            }
            if (modeSettingsCollection != null)
            {
                debugMode = Convert.ToBoolean(modeSettingsCollection["debugMode"]);
                graphicsLightMode = Convert.ToBoolean(modeSettingsCollection["graphicsLightMode"]);
                runMinimized = Convert.ToBoolean(modeSettingsCollection["runAsMinimized"]);
            }
            
            if (idCollection != null)
            {
                kioskID = Convert.ToInt32(idCollection["myAppID"]);
                int numberOfApps = Convert.ToInt32(idCollection["numberOfApps"]);
                string newAppID = "";
                for (int i = 0; i < numberOfApps; ++i)
                {
                    newAppID = idCollection[Convert.ToString(i)];
                    //System.Diagnostics.Debug.WriteLine("found application " + newAppID);
                    if (i == kioskID)
                    {
                        kioskName = newAppID;
                        //System.Diagnostics.Debug.WriteLine("I am application: " + newAppID);
                    }

                }
            }
            
        }

        private void readConfigFile()
        {
            // Read custom sectionGroup
            NameValueCollection modeSettingsCollection = new NameValueCollection();
            NameValueCollection networkCommCollection = new NameValueCollection();
            NameValueCollection assetCollection = new NameValueCollection();
            NameValueCollection userGridCommCollection = new NameValueCollection();

            NameValueCollection graphicsWindowCollection = new NameValueCollection();

            NameValueCollection cameraCollection = new NameValueCollection();
            NameValueCollection viewportCollection = new NameValueCollection();

            //NameValueCollection sensorOrientationCollection = new NameValueCollection();
            //NameValueCollection sensorRangeCollection = new NameValueCollection();
            //NameValueCollection userGridCollection = new NameValueCollection();

            //NameValueCollection sensorActivationCollection = new NameValueCollection();
            NameValueCollection fileInterropCollection = new NameValueCollection();
            NameValueCollection modelWindowCollection = new NameValueCollection();

            try
            {
                modeSettingsCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("globalSettings/modes");
                networkCommCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("globalSettings/commVariables");
                assetCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("globalSettings/assetVariables");
                userGridCommCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("globalSettings/userGridMeshTransportUDPVariables");

                graphicsWindowCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("graphicsSettings/graphicsWindowTarget");

                cameraCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("viewSettings/camera");
                viewportCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("viewSettings/viewport");
                fileInterropCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("localFileSettings/localDataFileInterrop");
                modelWindowCollection = (NameValueCollection) System.Configuration.ConfigurationManager.GetSection("localFileSettings/loadAndViewModel");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error in config file " + e);
            }

            // size and position of window as drawn on desktop:
            if (graphicsWindowCollection != null)
            {
                globalPixelScale = Convert.ToDouble(graphicsWindowCollection["globalPixelScale"]);
                //depthCameraControlVar.setGlobalScale(globalPixelScale);

                targetWindowW = Convert.ToInt32(graphicsWindowCollection["w"]);
                targetWindowH = Convert.ToInt32(graphicsWindowCollection["h"]);
                targetWindowPosnX = Convert.ToInt32(graphicsWindowCollection["x"]);
                targetWindowPosnY = Convert.ToInt32(graphicsWindowCollection["y"]);
                twoDdrawingScale = (float) Convert.ToDouble(graphicsWindowCollection["globalScale"]);
            }

            /*
            if (modelWindowCollection != null)
            {
                string doLoadLocalOBJFile_st = modelWindowCollection["doLoadOBJFileIntoViewer"];
                doLoadLocalOBJFile = Convert.ToBoolean(doLoadLocalOBJFile_st);
                threeDModelFile = modelWindowCollection["modelPath"];
            }*/

            if (cameraCollection != null)
            {
                this.cameraFOVdeg = Convert.ToSingle(cameraCollection["fov"]);
            }

            if (viewportCollection != null)
            {
                MyWindow.floatingWindowW = Convert.ToInt32(viewportCollection["width"]);
                MyWindow.floatingWindowH = Convert.ToInt32(viewportCollection["height"]);
                MyWindow.floatingWindowPosX = Convert.ToInt32(viewportCollection["x"]);
                MyWindow.floatingWindowPosY = Convert.ToInt32(viewportCollection["y"]);
                this.cameraNearClip = Convert.ToSingle(viewportCollection["near"]);
                this.cameraFarClip = Convert.ToSingle(viewportCollection["far"]);
            }

            // ************************
            // assign these (local) variables from the config file here
            // ************************

            if (modeSettingsCollection != null)
            {
                debugMode = Convert.ToBoolean(modeSettingsCollection["debugMode"]);
                graphicsLightMode = Convert.ToBoolean(modeSettingsCollection["graphicsLightMode"]);
            }

            //***************************************
            // communicate between computers:
            //***************************************

            //string portString = "null";

            getIPaddress();

            if (networkCommCollection != null)
            {
                communicationsIP = Convert.ToString(networkCommCollection["udp_transmit_ip"]);
                communicationsPort1 = Convert.ToInt32(networkCommCollection["udp_transmit_port1"]);
                communicationsPort2 = Convert.ToInt32(networkCommCollection["udp_transmit_port2"]);
                //interComputerClient = new InterComputerClient(communicationsIP, communicationsPort);
                //interComputerClient.OnCommand += new ClientCommandEventHandler(interComputerClient_OnCommand);
                if (networkCommCollection["validateLocalIP"] != null)
                {

                    validIPStringTest = networkCommCollection["validateLocalIP"];
                }
                else
                {
                    validIPStringTest = "192";
                }
            }


            if (assetCollection != null)
            {
                sharedAssetFolderPath = Convert.ToString(assetCollection["sharedAssetPath"]);
            }
            // ***************************************
            // sensor settings, orientation and resolution:
            // ***************************************
            // 
            // 
            // 


            /*
            int i;
            numberOfCamerasExpected = 0;

            // replacing orienation to use getCameraData from XML file
            for (i = 0; i < maxNumberOfCameras; ++i)
            {
                depthCameraControlVar.setReverseImage(i, getCameraDataVar.doMirrorData[i]);
                depthCameraControlVar.setTurnDepthCameraOnSide(i, getCameraDataVar.cameraIsRotated[i],
                    getCameraDataVar.doRotateCameraCCW[i]);
                depthCameraControlVar.setTurnCameraUpsideDown(i, getCameraDataVar.doRotateCameraUpsideDown[i]);
                if (getCameraDataVar.doActivateCamera[i])
                {
                    numberOfCamerasExpected += 1;
                    depthCameraControlVar.overideActivationFromConfig(i, getCameraDataVar.doActivateCamera[i]);
                }
                else
                {
                    depthCameraControlVar.overideActivationFromConfig(i, getCameraDataVar.doActivateCamera[i]);
                }
            }*/




            /************************************************************************************************************************/

            // assign sensor limits and filters:
            /*
            if (sensorRangeCollection != null)
            {
                depthCameraControlVar.setSensorRanges(Convert.ToDouble(sensorRangeCollection["maxMeasuredRange"]), Convert.ToDouble(sensorRangeCollection["minMeasuredRange"]));
                depthCameraControlVar.setSensorFloorCeil(Convert.ToDouble(sensorRangeCollection["filterOutDataBelow"]), Convert.ToDouble(sensorRangeCollection["filterOutDataAbove"]));
                depthCameraControlVar.setFilterDefault(Convert.ToBoolean(sensorRangeCollection["applyTheseFiltersByDefault"]));
            }
            */

            //depthCameraControlVar.setSensorRanges(getCameraDataVar.setCameraMaxRange,
            //    getCameraDataVar.setCameraMinRange);
            //depthCameraControlVar.setSensorFloorCeil(getCameraDataVar.setFloorFilter, getCameraDataVar.setCeilFilter);
            //depthCameraControlVar.setFilterDefault(getCameraDataVar.doApplyFiltersByDefault);

            //***************************************
            // depth data communications:
            //***************************************
            bool activateDataTransmission = false;
            int transmitPort = 3000;

            if (fileInterropCollection != null)
            {
                isUsingLocalFileIO = Convert.ToBoolean(fileInterropCollection["doEnableFileInterrop"]);
                isRecordingToFile = Convert.ToBoolean(fileInterropCollection["setupForRecord"]);
                isPlayingFromFile = Convert.ToBoolean(fileInterropCollection["setupForPlayback"]);
                localDataFileName = fileInterropCollection["localDataFileName"];
            }

            /*
            // assign data communications
            if ((userGridCommCollection != null))
            {
                activateDataTransmission = Convert.ToBoolean(userGridCommCollection["activateDataTransmission"]);

                depthCameraControlVar.initDepthCommunicationsGrid(
                    getCameraDataVar.userSensorGridResolutionWidth,
                    getCameraDataVar.userSensorGridResolutionHeight,
                    getCameraDataVar.userSensorGridResolutionDepth,
                    activateDataTransmission,
                    depthDataTransmitRate,
                    isUsingLocalFileIO,
                    isRecordingToFile,
                    isPlayingFromFile,
                    localDataFileName
                );

                transmitPort = Convert.ToInt32(userGridCommCollection["depthData_transmit_port"]);
                depthCameraControlVar.initDataTransmission(transmitPort);
            }*/

            /*
            // user sensor grid has now been initiated, set up user sensor grid variables
            if (userGridCollection != null)
            {
                // setting up usersensor grid
                depthCameraControlVar.setXmitRanges(Convert.ToDouble(userGridCollection["gridMinUserRangeLimit"]), Convert.ToDouble(userGridCollection["gridMaxUserRangeLimit"]));
                depthCameraControlVar.setUserGridMeasurement(Convert.ToDouble(userGridCollection["gridMeasuredWidth"]), Convert.ToDouble(userGridCollection["gridMeasuredStartHeight"]), Convert.ToDouble(userGridCollection["gridMeasuredEndHeight"]));

                // for drawing blue box on grid:
                drawGridPlaneVar.setXmitRanges(Convert.ToDouble(userGridCollection["gridMinUserRangeLimit"]), Convert.ToDouble(userGridCollection["gridMaxUserRangeLimit"]));
                drawGridPlaneVar.setUserGridMeasurement(Convert.ToDouble(userGridCollection["gridMeasuredWidth"]), Convert.ToDouble(userGridCollection["gridMeasuredStartHeight"]), Convert.ToDouble(userGridCollection["gridMeasuredEndHeight"]));
            }
            */

            //depthCameraControlVar.setXmitRanges(getCameraDataVar.userSensorGridNearLimit,
            //    getCameraDataVar.userSensorGridFarLimit);
            //depthCameraControlVar.setUserGridMeasurement(getCameraDataVar.userSensorGridWidth,
            //    getCameraDataVar.userSensorGridStartHeight, getCameraDataVar.userSensorGridEndHeight);
            drawGridPlaneVar.setXmitRanges(getCameraDataVar.userSensorGridNearLimit,
                getCameraDataVar.userSensorGridFarLimit);
            drawGridPlaneVar.setUserGridMeasurement(getCameraDataVar.userSensorGridWidth,
                getCameraDataVar.userSensorGridStartHeight, getCameraDataVar.userSensorGridEndHeight);

            //depthCameraControlVar.setSkeletalTracking(getCameraDataVar.doSenseSkeletons);
            /*
            bool applyGroundFilter = false;
            bool learnBackground = false;
            bool doLoadSavedBackground = false;

            if (userGridCollection != null)
            {
                applyGroundFilter = Convert.ToBoolean(userGridCollection["applyGroundFilter"]);
                learnBackground = Convert.ToBoolean(userGridCollection["learnBackgroundUponStartup"]);
                doLoadSavedBackground = Convert.ToBoolean(userGridCollection["usedSavedBackground"]);
            }
            

            // apply ground filter (these are false by default, so toggle makes them true)
            //if (applyGroundFilter)
            //    toggleGroundFilter(true);


            //if (doLoadSavedBackground)
            //    loadBgThreshold();


            if (learnBackground)
            {
                toggleLearnBackground();
                drawDepthPoints = true;
            }
            */
        }

        #endregion readConfigFile

        // ******************************************************************
        // ******************************************************************

        #region cameras and communication

        private void sendSampleSoundRequest()
        {
            interComputerClient1.sendAudioSampleRequest("ding");
        }



        /*
        private void checkForConnectedCameras()
        {
            int i;
            
            if (checkUSBConnectionsVar == null)
                checkUSBConnectionsVar = new CheckConnected();
            
            //if (!debugMode)
            //{
                if (checkUSBConnectionsVar.numberOfDevices != numberOfCamerasExpected) // expected cameras are missing
                {
                    writeToEventLog("MISSING DEPTH CAMERA: only [" + checkUSBConnectionsVar.numberOfDevices + "] of [" + numberOfCamerasExpected + "] cameras detected");
                    System.Diagnostics.Debug.WriteLine("[MAIN] MISSING DEPTH CAMERA: only [" + checkUSBConnectionsVar.numberOfDevices + "] of [" + numberOfCamerasExpected + "] cameras detected");
                    for (i = 0; i < numberOfCamerasExpected; ++i)
                    {
                        string writeToEvent = "";
                        if (!checkUSBConnectionsVar.isConnected[i])
                        {
                            writeToEvent += "DEPTH CAMERA #[" + i + "] is not detected. ";
                            missingCameraReportToScreen += "[" + i + "], ";
                        }
                        writeToEventLog(writeToEvent);
                    }
                }
            //}
        }*/


        private void checkForDeepActivation()
        {
            int i, j;
            bool skipThisZone = false;
            bool skipThisMid = false;
            sensingStatusReport = "";
            bool nearIsActivated = false;
            bool midIsActivated = false;

            
            for (i = 0; i < actualNumberOfPanels; ++i) // check all of the zones
            {
                if (i == 3)
                    skipThisZone = true;
                else if (i == 4)
                    skipThisZone = true;
                else
                    skipThisZone = false;

                if (!skipThisZone)
                {
                    if (deepActivationStatus[i, 0] == true) // if any near are activated
                    {
                        nearIsActivated = true;
                    }
                    if (deepActivationStatus[i, 1] == true) // if any mid are activated
                    {
                        // some mids are stuck on (hardware interference)
                        if (i == 1) // 0 and 2 are good
                            skipThisMid = true;
                        else if (i == 6) // 5 and 7 are good
                            skipThisMid = true;
                        else if (i == 9) // 8 and 10 are good
                            skipThisMid = true;
                        else
                        {
                            skipThisMid = false;
                            midIsActivated = true;
                        }
                    }
                }
                
                if (skipThisZone)
                    sensingStatusReport += "X" + i + ", " + deepActivationStatus[i, 0] +", "+deepActivationStatus[i, 1]+ "X ";
                else
                {
                    sensingStatusReport += "[" + i + ", c " + deepActivationStatus[i, 0] +", m "+deepActivationStatus[i, 1]+ "] ";
                }

                if (i == 2)
                    sensingStatusReport += "\n";
                if (i == 4)
                    sensingStatusReport += "\n";
                if (i == 7)
                    sensingStatusReport += "\n";

            }
            
            
            sensingStatusReport += "\n";
            
            if (nearIsActivated)
            {
                audioControl.setStateTo("highlight");
            }
            else
            {
                if (midIsActivated)
                    audioControl.setStateTo("build");
                else
                {
                    audioControl.setStateTo("noPresence"); // comment this line out when testing with keyboard
                }
            }
            
            /*
            if (someoneIsNearTheMiddle)
            {
                audioControl.edgePresenceDetected();
            }
            else
            {
                audioControl.edgePresenceNotDetected();
            }*/
        }
        
        /*
        private void checkForCenterActivation()
        {
            int i;
            bool someoneIsInTheMiddle = false;
            bool someoneIsNearTheMiddle = false;
            bool skipThisZone = false;

            sensingStatusReport = "";
            for (i = 0; i < actualNumberOfPanels; ++i) // check all of the zones
            {
                if (i == 3)
                    skipThisZone = true;
                else if (i == 4)
                    skipThisZone = true;
                else
                    skipThisZone = false;

                if (!skipThisZone)
                {
                    if (activationStatus[i] == 0)
                    {
                        someoneIsInTheMiddle = true;
                    }

                    if (activationStatus[i] == 1)
                    {
                        someoneIsNearTheMiddle = true;
                    }
                }

                if (skipThisZone)
                    sensingStatusReport += "X" + i + ", " + activationStatus[i] + "X ";
                else
                {
                    sensingStatusReport += "[" + i + ", " + activationStatus[i] + "] ";
                }
            }

            sensingStatusReport += "\n";
            
            if (someoneIsInTheMiddle)
            {
                audioControl.centerPresenceDetected();
            }
            else
            {
                audioControl.centerPresenceNotDetected();
            }
            
            if (someoneIsNearTheMiddle)
            {
                audioControl.edgePresenceDetected();
            }
            else
            {
                audioControl.edgePresenceNotDetected();
            }
            
            
        }*/
        
        
        void interComputerClient_OnCommand1(object sender, ClientCommandEventsArgs e)
        {

            int whichWallRegion;
            System.Diagnostics.Debug.WriteLine("[SENSORCONTROL] Client 01 sender =[" + e.source + "] method =[" + e.method + "] args =[" + e.args + "]");
            switch (e.method)
            {
                case "connected":
                    string whichIP = e.args;
                    updateConnectedFeedback(whichIP);
                    break;
                case "launchAmbientLoops":
                    doLaunchAmbientLoopOnNextUpdate = true;
                    //audioControl.startAmbientLoops(); // threading issue
                    break;
                case "haltAmbientLoops":
                    doHaltAmbientLoopOnNextUpdate = true;
                    break;
                case "regionUpdate":
                    updateIndividualRegionStatus(e.args);
                    break;
                case "activateRegionNear":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 0, true);
                    break;
                case "activateRegionMid":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 1, true);
                    break;
                case "activateRegionFar":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 2, true);
                    break;
                case "deactivateRegionNear":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 0, false);
                    break;
                case "deactivateRegionMid":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 1, false);
                    break;
                case "deactivateRegionFar":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 2, false);
                    break;                   
            }
        }
        
        void interComputerClient_OnCommand2(object sender, ClientCommandEventsArgs e)
        {

            //System.Diagnostics.Debug.WriteLine("[MAIN] interComputerClient sender =[" + e.source + "] method =[" + e.method + "] args =[" + e.args + "]");
            //switch (e.method)
            //{
            //}
            int whichWallRegion;
            
            System.Diagnostics.Debug.WriteLine("[SENSORCONTROL] Client 02 sender =[" + e.source + "] method =[" + e.method + "] args =[" + e.args + "]");
            switch (e.method)
            {
                case "connected":
                    string whichIP = e.args;
                    updateConnectedFeedback(whichIP);
                    break;
                case "launchAmbientLoops":
                    doLaunchAmbientLoopOnNextUpdate = true;
                    //audioControl.startAmbientLoops(); // threading issue
                    break;
                case "haltAmbientLoops":
                    doHaltAmbientLoopOnNextUpdate = true;
                    break;
                case "activateRegionNear":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 0, true);
                    break;
                case "activateRegionMid":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 1, true);
                    break;
                case "activateRegionFar":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 2, true);
                    break;
                case "deactivateRegionNear":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 0, false);
                    break;
                case "deactivateRegionMid":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 1, false);
                    break;
                case "deactivateRegionFar":
                    whichWallRegion = Convert.ToInt32(e.args);
                    updateActivationStatus(whichWallRegion, 2, false);
                    break;            
            }
        }

        private static int maxNumberOfPanels = 50;
        private int actualNumberOfPanels = 11;
        //private  AreaActivationState[] activationState = new AreaActivationState[maxNumberOfPanels];
        private int[] activationStatus = new int[maxNumberOfPanels]; // 0 = close 1 = mid, 2 = far, 3 = none
        private bool[] activationStatusChanged = new bool[maxNumberOfPanels]; // 0 = close 1 = mid, 2 = far, 3 = none

        private bool[,] deepActivationStatus = new bool[maxNumberOfPanels, 4];
        
        private string sensingStatusReport = "NA";
        // **************************************
        // GET/SET has state changed?
        // **************************************
        public bool activationIsdirty = false;
        public bool sensingIsDirty()
        {
            bool valueToReturn = activationIsdirty;
            if (activationIsdirty)
            {
                activationIsdirty = false;
            }
            return valueToReturn;
        }

        private void resetAllActivationAreas()
        {
            int i, j;
            
            for ( i = 0; i < maxNumberOfPanels; ++i)
            {
                activationStatus[i] = 3;
                for (j = 0; j < 4; ++j)
                    deepActivationStatus[i, j] = false;
            }
            
             
        }

        private void updateActivationStatus(int whichRegion, int whichActivation, bool whichStatus)
        {
            
            System.Diagnostics.Debug.WriteLine("[SENSORCONTROL] region =[" + whichRegion + "] which =[" + whichActivation + "] status =[" + whichStatus + "]");

            deepActivationStatus[whichRegion, whichActivation] = whichStatus;
            
        }
        
        private void updateIndividualRegionStatus(string whichNewStatus) // this is the newer "individual" message (0 2 S)
        {
            try
            {
                // remove parenthesis:
                string statusWithoutParenth = whichNewStatus.Replace("(", "").Replace(")", "");
                // pull values from string:
                string[] split = statusWithoutParenth.Split(' ');  // incoming values are SPACE delineated
                int availableValues = split.Length;
                int whichRegion = -1;  // will be a sensing region between 0 and (however many regions there are)
                int whichStatus = 3; // 0 = closest, 1 = close, 2 = far, 3 = inactive
                string shortOrTall = "T";  // will either be S or T;
                for (int i = 0; i < availableValues; ++i) // there should ALWAYS be three values... regionID, regionStatus, shortOrTall
                {
                    if (i==0)
                        whichRegion =  Convert.ToInt32(split[i]);
                    else if (i==1)
                        whichStatus = Convert.ToInt32(split[i]);
                    else if (i==2)
                        shortOrTall = split[i];
                }

                if (activationStatus[whichRegion] != whichStatus)
                {
                    activationStatusChanged[whichRegion] = true;
                    activationStatus[whichRegion] = whichStatus;
                    activationIsdirty = true; // flag that something changed 
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[SENSORLISTENER] unable to parse region update: "+whichNewStatus);
            }
        }

        private void getIPaddress()
        {
            try
            {

                // Get host name
                String strHostName = System.Net.Dns.GetHostName();
                //System.Diagnostics.Debug.WriteLine("Host Name: " + strHostName);

                // Find host by name
                System.Net.IPHostEntry iphostentry = System.Net.Dns.GetHostEntry(strHostName);

                // Enumerate IP addresses
                string whichIPaddress = "NA";
                foreach (System.Net.IPAddress ipaddress in iphostentry.AddressList)
                {
                    if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        whichIPaddress = ipaddress.ToString();
                        System.Diagnostics.Debug.WriteLine("client could use ip : " + whichIPaddress);
                        if (whichIPaddress.Substring(0, validIPStringTest.Length) == validIPStringTest) // test for valid IP address... (starts with 192.)
                        {
                            kioskIPaddress = whichIPaddress;
                            System.Diagnostics.Debug.WriteLine("client using ip : " + kioskIPaddress);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                kioskIPaddress = "192.168.0.127";
                System.Diagnostics.Debug.WriteLine("error getting IP using default: " + kioskIPaddress + " ERROR message: " + e.Message);
            }
        }


        // ***********************************
        // start/stop video events:
        // ***********************************

        /*
        private void reactiveArea_OnEvent(object sender, AppEvent e)
        {
            if (e.EventSource == "reactiveAreaControl")
            {
                int whichWallRegion = -1;

                bool doSendUpdatedState = false;

                switch (e.EventString)
                {
                    case "activateRegionNear":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendActivateNearRegion(whichWallRegion);
                        doSendUpdatedState = true;
                        break;
                    case "activateRegionMid":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendActivateMidRegion(whichWallRegion);
                        doSendUpdatedState = true;
                        break;
                    case "activateRegionFar":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendActivateFarRegion(whichWallRegion);
                        doSendUpdatedState = true;
                        //if (isUsingAudio)
                        //    GeneralAudio.PlaySound(audioFilesVar.audioName[whichWallRegion]);
                        //Beep(500, 250);
                        break;
                    case "deactivateRegionNear":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendDeactivateNearRegion(whichWallRegion);
                        doSendUpdatedState = true;
                        break;
                    case "deactivateRegionMid":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendDeactivateMidRegion(whichWallRegion);
                        doSendUpdatedState = true;
                        break;
                    case "deactivateRegionFar":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendDeactivateFarRegion(whichWallRegion);
                        doSendUpdatedState = true;
                        break;
                }

                //if (doSendUpdatedState)
                //    sendActivationStatus();
            }*/
        
            /*
            else if (e.EventSource == "reactiveArea2DControl")
            {
                int whichWallRegion = -1;


                switch (e.EventString)
                {
                    case "activateRegion2DNear":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendActivateNearRegion(whichWallRegion);
                        break;
                    case "deactivateRegion2DNear":
                        whichWallRegion = Convert.ToInt32(e.EventArgs[0]);
                        sendDeactivateNearRegion(whichWallRegion);
                        break;
                }
            }*/
        //}

        private void sendTestActivation()
        {
            areaActivationTrackerVar.updateActivation(0, 0, true);
        }

        private void sendTestDeactivation()
        {
            areaActivationTrackerVar.updateActivation(0, 0, false);
        }

        /*
        private void sendActivationStatusIfNeeded()
        {
            if (areaActivationTrackerVar.isDirty())
            {
                interComputerClient1.sendActivationStatus(areaActivationTrackerVar.currentActivationFeedbackForClient);
                interComputerClient2.sendActivationStatus(areaActivationTrackerVar.currentActivationFeedbackForClient);
            }
        }*/
        
        private void sendActivateNearRegion(int whichRegion)
        {
            areaActivationTrackerVar.updateActivation(whichRegion, 0, true);
            //if (areaActivationTrackerVar.isDirty())
                //interComputerClient.sendActivateNearEvent(whichRegion);
        }

        private void sendDeactivateNearRegion(int whichRegion)
        {
            areaActivationTrackerVar.updateActivation(whichRegion, 0, false);
            //if (areaActivationTrackerVar.isDirty())
            //    interComputerClient.sendDeactivateNearEvent(whichRegion);
        }

        private void sendActivateMidRegion(int whichRegion)
        {
            areaActivationTrackerVar.updateActivation(whichRegion, 1, true);
            //if (areaActivationTrackerVar.isDirty())
            //    interComputerClient.sendActivateMidEvent(whichRegion);
        }
        private void sendDeactivateMidRegion(int whichRegion)
        {
            areaActivationTrackerVar.updateActivation(whichRegion, 1, false);
            //if (areaActivationTrackerVar.isDirty())
            //    interComputerClient.sendActivateMidEvent(whichRegion);
        }

        private void sendActivateFarRegion(int whichRegion)
        {
            areaActivationTrackerVar.updateActivation(whichRegion, 2, true);
            //if (areaActivationTrackerVar.isDirty())
            //    interComputerClient.sendActivateFarEvent(whichRegion);
        }

        private void sendDeactivateFarRegion(int whichRegion)
        {
            areaActivationTrackerVar.updateActivation(whichRegion, 2, false);
            //if (areaActivationTrackerVar.isDirty())
            //    interComputerClient.sendDeactivateFarEvent(whichRegion);
        }

        #endregion cameras and communication


        private void reportDisconnectedCameraToEventLog()
        {
           //if (!didReportCameraLoss) // only report loss once:
           // {
           //     writeToEventLog("[MainApp] Depth Camera has been disconnected");
           // }

           // didReportCameraLoss = true;
        }

        private void writeToEventLog(string whichMessage)
        {
            try
            {
                EventLog.WriteEntry(EVENTLOG_SOURCENAME, whichMessage, EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[MAINAPP] event log access denied ");
            }
        }

        void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                EventLog.WriteEntry(EVENTLOG_SOURCENAME, "Unhandled Thread Exception", EventLogEntryType.Error);
                EventLog.WriteEntry(EVENTLOG_SOURCENAME, e.ToString(), EventLogEntryType.Error);
            }
            catch (Exception e2)
            {
                System.Diagnostics.Debug.WriteLine("[MAINAPP] event log access denied ");
            }
        }
    }
    
}
