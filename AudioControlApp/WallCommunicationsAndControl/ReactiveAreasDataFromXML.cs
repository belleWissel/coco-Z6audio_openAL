using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Windows.Forms; // error message

namespace AudioControlApp.WallCommunicationsAndControl
{
    class ReactiveAreasDataFromXML
    {
        // ***************************************************************************
        // XML File Variables
        private XmlDocument xDoc;
        private bool XMLFileLoaded = false;

        // ***************************************************************************

        //private static int maxNumberOfActivationAreas = 20;
        public int actualNumberOfActivationAreas = -1;
        public int areaStartIndex = 0;

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

        public string filePath = "\\motion\\myfile.txt";

        // ***************************************************************************
        private string pathToXML = "";

        public bool dataIsReady = false;

        public ReactiveAreasDataFromXML()
        {
            pathToXML = "XML//ActivationAreaSizes.xml";

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
                    System.Diagnostics.Trace.WriteLine("[BgXML] ERROR: XML Page Not Found: " + whichXMLFileAndPath + " exception: " + e.Source);
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
            XmlNode parentNode = xDoc.SelectSingleNode("AreaActivationSizes");

            string getNumberOf = parentNode.SelectSingleNode("NumberOfAreas").Attributes.GetNamedItem("data").InnerText;
            string getGetStartIndex = "0";
            if (parentNode.SelectSingleNode("AreaStartIndex").Attributes.GetNamedItem("data").InnerText != null)
            {
                getGetStartIndex = parentNode.SelectSingleNode("AreaStartIndex").Attributes.GetNamedItem("data").InnerText;
            }

            XmlNode sizeNode = parentNode.SelectSingleNode("AreaSizes");

            string getWidth = sizeNode.SelectSingleNode("AreaWidthPercentTotalWidth").Attributes.GetNamedItem("data").InnerText;
            string getBottom = sizeNode.SelectSingleNode("AreaBottomPercentTotalHeight").Attributes.GetNamedItem("data").InnerText;
            string getTop = sizeNode.SelectSingleNode("AreaTopPercentTotalHeight").Attributes.GetNamedItem("data").InnerText;

            XmlNode depthsNode = parentNode.SelectSingleNode("AreaDepths");

            string getNearDepth = depthsNode.SelectSingleNode("nearRange").Attributes.GetNamedItem("data").InnerText;
            string getMidDepth = depthsNode.SelectSingleNode("midRange").Attributes.GetNamedItem("data").InnerText;
            string getFarDepth = depthsNode.SelectSingleNode("farRange").Attributes.GetNamedItem("data").InnerText;


            actualNumberOfActivationAreas = Convert.ToInt32(getNumberOf);
            areaStartIndex = Convert.ToInt32(getGetStartIndex);

            sensorActivationAreaWidthPercentOfTotal = (float)Convert.ToDouble(getWidth);
            sensorActivationAreaBottomPercentOfTotalHeight = (float)Convert.ToDouble(getBottom);
            sensorActivationAreaTopPercentOfTotalHeight = (float)Convert.ToDouble(getTop);

            sensorDepthNear = (float)Convert.ToDouble(getNearDepth);
            sensorDepthMid = (float)Convert.ToDouble(getMidDepth);
            sensorDepthFar = (float)Convert.ToDouble(getFarDepth);


            XmlNode timingNode = parentNode.SelectSingleNode("userTimingSettings");

            XmlNode activateNode = timingNode.SelectSingleNode("reactiveAreasActivation");
            XmlNode deactivateNode = timingNode.SelectSingleNode("reactiveAreasDeactivation");

            string getNearAct = activateNode.SelectSingleNode("triggerNearRegionTime").Attributes.GetNamedItem("data").InnerText;
            string getMidAct = activateNode.SelectSingleNode("triggerMidRegionTime").Attributes.GetNamedItem("data").InnerText;
            string getFarAct = activateNode.SelectSingleNode("triggerFarRegionTime").Attributes.GetNamedItem("data").InnerText;
            string getNearDeact = deactivateNode.SelectSingleNode("deactivateNearRegionTime").Attributes.GetNamedItem("data").InnerText;
            string getMidDeact = deactivateNode.SelectSingleNode("deactivateMidRegionTime").Attributes.GetNamedItem("data").InnerText;
            string getFarDeact = deactivateNode.SelectSingleNode("deactivateFarRegionTime").Attributes.GetNamedItem("data").InnerText;

            triggerOnNear = Convert.ToDouble(getNearAct);
            triggerOnMid = Convert.ToDouble(getMidAct);
            triggerOnFar = Convert.ToDouble(getFarAct);

            triggerOffNear = Convert.ToDouble(getNearDeact);
            triggerOffMid = Convert.ToDouble(getMidDeact);
            triggerOffFar = Convert.ToDouble(getFarDeact);

            dataIsReady = true;
        }
    }
}
