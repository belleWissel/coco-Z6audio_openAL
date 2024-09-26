using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Windows.Forms; // error message

namespace SensorControlApp.WallCommunicationsAndControl
{
    class ReactiveAreasDataTwoDimensional
    {
        // ***************************************************************************
        // XML File Variables
        private XmlDocument xDoc;
        private bool XMLFileLoaded = false;

        // ***************************************************************************
        public int actualNumberOfActivationAreasW = -1;
        public int actualNumberOfActivationAreasH = -1;

        public float sensorActivationAreaWidthPercentOfTotal = 0.1f;
        public float sensorActivationAreaHeightPercentOfTotal = 0.15f;

        public float sensorTriggerDepth = 0.0f;

        public double triggerOn = 0.5f;
 
        public double triggerOff = 0.5f;

        public string filePath = "\\motion\\myfile.txt";

        // ***************************************************************************
        private string pathToXML = "";

        public bool dataIsReady = false;

        public ReactiveAreasDataTwoDimensional()
        {
            pathToXML = "XML//ActivationAreaSizesTwoDimensional.xml";

            loadXML(pathToXML);
        }



        private void loadXML(string whichXMLFileAndPath)
        {


            if (xDoc == null) // first time loading a file
                xDoc = new XmlDocument();

            if (File.Exists(SensorControlApp.FileUtils.MakeAbsolutePath(whichXMLFileAndPath)))
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

            string getNumberOfW = parentNode.SelectSingleNode("NumberOfAreas").Attributes.GetNamedItem("width").InnerText;
            string getNumberOfH = parentNode.SelectSingleNode("NumberOfAreas").Attributes.GetNamedItem("height").InnerText;


            XmlNode sizeNode = parentNode.SelectSingleNode("AreaSizes");

            string getWidth = sizeNode.SelectSingleNode("AreaWidthPercentTotalWidth").Attributes.GetNamedItem("data").InnerText;
            string getHeight = sizeNode.SelectSingleNode("AreaHeightPercentTotalHeight").Attributes.GetNamedItem("data").InnerText;

            XmlNode depthsNode = parentNode.SelectSingleNode("AreaDepths");

            string getTriggerDepth = depthsNode.SelectSingleNode("range").Attributes.GetNamedItem("data").InnerText;


            actualNumberOfActivationAreasW = Convert.ToInt32(getNumberOfW);
            actualNumberOfActivationAreasH = Convert.ToInt32(getNumberOfH);

            sensorActivationAreaWidthPercentOfTotal = (float)Convert.ToDouble(getWidth);
            sensorActivationAreaHeightPercentOfTotal = (float)Convert.ToDouble(getHeight);

            sensorTriggerDepth = (float)Convert.ToDouble(getTriggerDepth);


            XmlNode timingNode = parentNode.SelectSingleNode("userTimingSettings");

            XmlNode activateNode = timingNode.SelectSingleNode("reactiveAreasActivation");
            XmlNode deactivateNode = timingNode.SelectSingleNode("reactiveAreasDeactivation");

            string getNearAct = activateNode.SelectSingleNode("activateTriggerRegionTime").Attributes.GetNamedItem("data").InnerText;
            string getNearDeact = deactivateNode.SelectSingleNode("deactivateRegionTime").Attributes.GetNamedItem("data").InnerText;

            triggerOn = Convert.ToDouble(getNearAct);
            triggerOff = Convert.ToDouble(getNearDeact);


            dataIsReady = true;
        }
    }
}
