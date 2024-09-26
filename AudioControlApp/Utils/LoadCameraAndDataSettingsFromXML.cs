using System;

using System.Xml;
using System.IO;
using System.Windows.Forms; // error message

namespace AudioControlApp.Utils
{
    class LoadCameraAndDataSettingsFromXML
    {
        // ***************************************************************************
        // XML File Variables
        private XmlDocument xDoc;
        private bool XMLFileLoaded = false;

        // ***************************************************************************

        //private static int maxNumberOfActivationAreas = 20;
        public int actualNumberOfActivationAreas = -1;

        public float sensorActivationAreaWidthPercentOfTotal = 0.1f;
        public float sensorActivationAreaBottomPercentOfTotalHeight = 0.15f;
        public float sensorActivationAreaTopPercentOfTotalHeight = 0.6f;


        public float sensorDepthNear = 0.0f;
        public float sensorDepthMid = 1.0f;
        public float sensorDepthFar = 2.0f;

        public double triggerOnNear = 0.5f;
        public double triggerOnMid = 0.5f;
        public double triggerOnFar = 0.5f;

        public double triggerOffNear = 0.5f;
        public double triggerOffMid = 0.5f;
        public double triggerOffFar = 0.5f;

        public string filePath = "\\xml\\CameraAndDataSettings.xml";

        // ***************************************************************************
        private string pathToXML = "";

        private bool dataIsReady = false;

        public double userSensorGridWidth = 1000.0;
        public double userSensorGridStartHeight = -1000.0;
        public double userSensorGridEndHeight = 1000.0;
        public double userSensorGridNearLimit = 0.0;
        public double userSensorGridFarLimit = 0.0;
        public int userSensorGridResolutionWidth = 50;
        public int userSensorGridResolutionHeight = 50;
        public int userSensorGridResolutionDepth = 50;

        public bool[] doActivateCamera = new bool[4];
        public bool[] cameraIsRotated = new bool[4];
        public bool[] doMirrorData = new bool[4];
        public bool[] doRotateCameraCW = new bool[4];
        public bool[] doRotateCameraCCW = new bool[4];
        public bool[] doRotateCameraUpsideDown = new bool[4];

        public double setCameraMaxRange = 4500.0;
        public double setCameraMinRange = 500.0;
        public double setCeilFilter = 1010.0;
        public double setFloorFilter = -1010.0;
        public bool doApplyFiltersByDefault = false;

        public bool doSenseSkeletons = false;

        public LoadCameraAndDataSettingsFromXML()
        {
            pathToXML = "XML//CameraAndDataSettings.xml";


            // reset values
            for(int i=0; i<4; ++i)
            {
                doActivateCamera[i] = false;
                doMirrorData[i] = false;
                cameraIsRotated[i] = false;
                doRotateCameraCW[i] = false;
                doRotateCameraCCW[i] = false;
                doRotateCameraUpsideDown[i] = false;
            }

            loadXML(pathToXML);

        }

        private void loadXML(string whichXMLFileAndPath)
        {


            if (xDoc == null) // first time loading a file
                xDoc = new XmlDocument();

            if (File.Exists(AudioControlApp.FileUtils.MakeAbsolutePath(whichXMLFileAndPath)))
            {
                try
                {
                    xDoc.Load(whichXMLFileAndPath); // testing with server service
                    XMLFileLoaded = true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("[camXML] ERROR: XML Page Not Found: " + whichXMLFileAndPath + " exception: " + e.Source);
                }
            }
            else
            {
                MessageBox.Show("Error Locating XML file: \n" + whichXMLFileAndPath);
            }

            if (XMLFileLoaded)
            {
                readXMLFile();
            }
        }

        private void readXMLFile()
        {
            int i;
            string whichNode;
            string isMirrored, isRotated, isRotatedCW, isRotatedCCW, isInverted;
            try
            {
                whichNode = "CameraSettings/CameraActivation";

                XmlNode parentNode = xDoc.SelectSingleNode(whichNode);

                string activationState0 = parentNode.SelectSingleNode("Camera0").Attributes.GetNamedItem("doActivate").InnerText;
                string activationState1 = parentNode.SelectSingleNode("Camera1").Attributes.GetNamedItem("doActivate").InnerText;
                string activationState2 = parentNode.SelectSingleNode("Camera2").Attributes.GetNamedItem("doActivate").InnerText;
                string activationState3 = parentNode.SelectSingleNode("Camera3").Attributes.GetNamedItem("doActivate").InnerText;

                doActivateCamera[0] = Convert.ToBoolean(activationState0);
                doActivateCamera[1] = Convert.ToBoolean(activationState1);
                doActivateCamera[2] = Convert.ToBoolean(activationState2);
                doActivateCamera[3] = Convert.ToBoolean(activationState3);

                for (i = 0; i < 4; ++i)
                {
                    whichNode = "CameraSettings/CameraPhysicalOrientation/Camera" + i;
                    parentNode = xDoc.SelectSingleNode(whichNode);
                    isMirrored = parentNode.SelectSingleNode("Reversed").Attributes.GetNamedItem("mirrored").InnerText;
                    isRotatedCW = parentNode.SelectSingleNode("Orientation").Attributes.GetNamedItem("isRotated90DegCW").InnerText;
                    isRotatedCCW = parentNode.SelectSingleNode("Orientation").Attributes.GetNamedItem("isRotated90DegCCW").InnerText;
                    isInverted = parentNode.SelectSingleNode("Orientation").Attributes.GetNamedItem("isUpsideDown").InnerText;

                    doMirrorData[i] = Convert.ToBoolean(isMirrored);
                    doRotateCameraCW[i] = Convert.ToBoolean(isRotatedCW);
                    doRotateCameraCCW[i] = Convert.ToBoolean(isRotatedCCW);
                    doRotateCameraUpsideDown[i] = Convert.ToBoolean(isInverted);

                    if (doRotateCameraCW[i])
                        cameraIsRotated[i] = true;
                    else if (doRotateCameraCCW[i])
                        cameraIsRotated[i] = true;
                    //else if (doRotateCameraUpsideDown[i])
                    //    cameraIsRotated[i] = true;
                }

                // *******************************************************

                whichNode = "CameraSettings/CameraDataFilters";
                parentNode = xDoc.SelectSingleNode(whichNode);

                string getMaxRange = parentNode.SelectSingleNode("PreCalibrationRangeFilter").Attributes.GetNamedItem("maxRangeMeasured").InnerText;
                string getMinRange = parentNode.SelectSingleNode("PreCalibrationRangeFilter").Attributes.GetNamedItem("minRangeMeasured").InnerText;
                string getDefaultRangeFilterMode = parentNode.SelectSingleNode("PreCalibrationRangeFilter").Attributes.GetNamedItem("applyFilterAtLaunch").InnerText;

                string getCeil = parentNode.SelectSingleNode("PostCalibrationFilter").Attributes.GetNamedItem("filterOutDataAbove").InnerText;
                string getFloor = parentNode.SelectSingleNode("PostCalibrationFilter").Attributes.GetNamedItem("filterOutDataBelow").InnerText;
                string getDefaultUpDownFilterMode = parentNode.SelectSingleNode("PostCalibrationFilter").Attributes.GetNamedItem("applyFilterAtLaunch").InnerText;

                setCameraMaxRange = Convert.ToDouble(getMaxRange);
                setCameraMinRange = Convert.ToDouble(getMinRange);

                setCeilFilter = Convert.ToDouble(getCeil);
                setFloorFilter = Convert.ToDouble(getFloor);

                bool doApplyPreFilter = Convert.ToBoolean(getDefaultRangeFilterMode);
                bool doApplyPostFilter = Convert.ToBoolean(getDefaultUpDownFilterMode);

                if (doApplyPostFilter || doApplyPreFilter)
                    doApplyFiltersByDefault = true;

                // *******************************************************

                whichNode = "CameraSettings/UserSensorGrid/GridSize";
                parentNode = xDoc.SelectSingleNode(whichNode);

                string getWidth = parentNode.SelectSingleNode("Width").Attributes.GetNamedItem("value").InnerText;
                string getBottom = parentNode.SelectSingleNode("Height").Attributes.GetNamedItem("bottomEdge").InnerText;
                string getTop = parentNode.SelectSingleNode("Height").Attributes.GetNamedItem("topEdge").InnerText;
                string getNear = parentNode.SelectSingleNode("Depth").Attributes.GetNamedItem("nearPlane").InnerText;
                string getFar = parentNode.SelectSingleNode("Depth").Attributes.GetNamedItem("farPlane").InnerText;

                userSensorGridWidth = Convert.ToDouble(getWidth);
                userSensorGridStartHeight = Convert.ToDouble(getBottom);
                userSensorGridEndHeight = Convert.ToDouble(getTop);
                userSensorGridNearLimit = Convert.ToDouble(getNear);
                userSensorGridFarLimit = Convert.ToDouble(getFar);

                // *******************************************************

                whichNode = "CameraSettings/UserSensorGrid/GridResolution";
                parentNode = xDoc.SelectSingleNode(whichNode);

                string getXRes = parentNode.SelectSingleNode("WidthPixels").Attributes.GetNamedItem("value").InnerText;
                string getYRes = parentNode.SelectSingleNode("HeightPixels").Attributes.GetNamedItem("value").InnerText;
                string getZRes = parentNode.SelectSingleNode("DepthPixels").Attributes.GetNamedItem("value").InnerText;

                userSensorGridResolutionWidth = Convert.ToInt16(getXRes);
                userSensorGridResolutionHeight = Convert.ToInt16(getYRes);
                userSensorGridResolutionDepth = Convert.ToInt16(getZRes);

                // *******************************************************

                whichNode = "CameraSettings/SkeletalTracking";
                parentNode = xDoc.SelectSingleNode(whichNode);

                string getSkeletalMode = parentNode.SelectSingleNode("Activation").Attributes.GetNamedItem("doSenseSkeletons").InnerText;
                doSenseSkeletons = Convert.ToBoolean(getSkeletalMode);

                dataIsReady = true;
            }
            catch
            {
                dataIsReady = false;
                System.Diagnostics.Debug.WriteLine("[camXML] ERROR: unable to read XM dataL");
            }
        }
    }
}
