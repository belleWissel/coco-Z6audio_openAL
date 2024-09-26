using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;

namespace AudioControlApp.Utils
{
    public class RecordHUDLogToFile
    {
        private bool monitoringIsActive = false;
        private bool recordToLocalFile = false;

        //**********************
        // XML File Variables
        private XmlDocument xDoc;
        private bool XMLFileIsAvailable = false;
        private string localFileAndPath = "XML\\recordOfShowCtrlEvents.xml";
        private string localPath = "XML\\";
        private bool flagForSaveXML = false;
        private bool XMLalreadyLoaded = false;
        private string currentDateOfFile = "2021xxxx"; // forcefully check for file availability upon startup

        public RecordHUDLogToFile(bool doRecordEverything)
        {
            monitoringIsActive = doRecordEverything; // this is assigned in the app.config file
            xDoc = new XmlDocument();
        }

        // this is called right after defining
        public void setModeAndVariables(bool doMakeFile, string whichPath)
        {
            recordToLocalFile = doMakeFile;
            localPath = whichPath;

            if (monitoringIsActive)
            {
                if (recordToLocalFile)
                    createLocalFileIfNecessary(); // redundant call? - no, need to establish xDoc variable prior to writing

                recordLaunchingMonitor();
            }
        }

        public void onProgramExit()
        {
            recordShuttingDownMonitor();
        }

        private void createLocalFileIfNecessary()
        {
            //string localPathToXML = Application.dataPath + "//streamingAssets//apisettings.xml"; // datapath vs persistentdatapath?

            localFileAndPath = localPath + "//streamingAssets//recordOfEvents-" + getCurrentDateFormatted() + ".xml";

            if (!isXMLFileAvailable()) // if there isn't a file already, then create it
            {
                createNewXMLFile();
                //bool success = isXMLFileAvailable();
                xDoc.Load(localFileAndPath); // now load it
                XMLalreadyLoaded = true;
                System.Diagnostics.Debug.WriteLine("[FILE IO] new data file created and loaded");
            }
            else // is a file already present? load it (if it hasn't alredy been loaded)
            {

                try
                {
                    if (!XMLalreadyLoaded)
                    {
                        System.Diagnostics.Debug.WriteLine("[FILE IO] loading existing file: " + localFileAndPath);

                        xDoc.Load(localFileAndPath);
                        XMLalreadyLoaded = true;
                    }
                }
                catch // file exists, but it's corrupted or otherwise won't load. (overwrite with new)
                {
                    createNewXMLFile();
                    //bool success = isXMLFileAvailable();
                    xDoc.Load(localFileAndPath); // now load it
                    XMLalreadyLoaded = true;
                    System.Diagnostics.Debug.WriteLine("[FILE IO] new data file created and loaded");
                }
                //readMostRecentRecordedValues();
            }
        }

        #region dateFormatting

        private string getCurrentDateFormatted()
        {
            // yyyymmdd_hhmmss
            string valueToReturn = "20171005";

            string yr = DateTime.Now.Year.ToString("0000");
            string mnth = DateTime.Now.Month.ToString("00");
            string day = DateTime.Now.Day.ToString("00");

            valueToReturn = yr + mnth + day;
            return valueToReturn;
        }

        private string getCurrentDateAndTimeFormatted()
        {
            // yyyymmdd_hhmmss
            string valueToReturn = "20171005_153515";

            string yr = DateTime.Now.Year.ToString("0000");
            string mnth = DateTime.Now.Month.ToString("00");
            string day = DateTime.Now.Day.ToString("00");

            string hr = DateTime.Now.Hour.ToString("HH");
            string min = DateTime.Now.Minute.ToString("00");
            string sec = DateTime.Now.Second.ToString("00");

            string nowTime = DateTime.Now.ToString("HH_mm_ss");

            valueToReturn = yr + mnth + day + "_" + nowTime;
            return valueToReturn;
        }

        #endregion dateFormatting

        #region remoteCalls

        public void recordGenericEvent(string whichText)
        {
            if (!monitoringIsActive)
                return;

            if (recordToLocalFile)
            {
                XmlNode newEntry = xDoc.SelectSingleNode("RecordOfExhibitEvents");
                if (newEntry != null)
                {
                    XmlElement newNode = xDoc.CreateElement("logMessage");
                    newNode.SetAttribute("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                    newNode.SetAttribute("KIOSK", whichText);
                    newEntry.AppendChild(newNode);


                    flagForSaveXML = true;
                }

                recordDataToFile();
            }
        }

        public void recordDisconnectedEvent(string whichKiosk)
        {
            if (!monitoringIsActive)
                return;

            if (recordToLocalFile)
            {
                XmlNode newEntry = xDoc.SelectSingleNode("RecordOfExhibitEvents");
                if (newEntry != null)
                {
                    XmlElement newNode = xDoc.CreateElement("disconnected");
                    newNode.SetAttribute("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                    newNode.SetAttribute("KIOSK", whichKiosk);
                    newEntry.AppendChild(newNode);


                    flagForSaveXML = true;
                }

                recordDataToFile();
            }

        }

        /*
        public void recordConnectedEvent(string whichKiosk)
        {
            if (!monitoringIsActive)
                return;

            if (recordToLocalFile)
            {
                XmlNode newEntry = xDoc.SelectSingleNode("RecordOfExhibitEvents");
                if (newEntry != null)
                {
                    XmlElement newNode = xDoc.CreateElement("connected");
                    newNode.SetAttribute("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                    newNode.SetAttribute("KIOSK", whichKiosk);
                    newEntry.AppendChild(newNode);


                    flagForSaveXML = true;
                }
                recordDataToFile();
            }
        }
        */
        public void recordShuttingDownMonitor()
        {
            if (!monitoringIsActive)
                return;

            if (recordToLocalFile)
            {

                XmlNode newEntry = xDoc.SelectSingleNode("RecordOfExhibitEvents");
                if (newEntry != null)
                {
                    XmlElement newNode = xDoc.CreateElement("monitorHalted");
                    newNode.SetAttribute("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                    newEntry.AppendChild(newNode);


                    flagForSaveXML = true;
                }

                recordDataToFile();
            }
        }

        private void recordLaunchingMonitor()
        {
            if (!monitoringIsActive)
                return;

            if (recordToLocalFile)
            {

                XmlNode newEntry = xDoc.SelectSingleNode("RecordOfExhibitEvents");
                if (newEntry != null)
                {
                    XmlElement newNode = xDoc.CreateElement("monitorLaunched");
                    newNode.SetAttribute("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                    newEntry.AppendChild(newNode);

                    flagForSaveXML = true;
                }

                recordDataToFile();
            }
        }

        /*
        public void updateRunStatus(string whichKiosk, bool whichStatus)
        {
            if (!monitoringIsActive)
                return;

            if (recordToLocalFile)
            {
                XmlNode newEntry = xDoc.SelectSingleNode("RecordOfExhibitEvents");
                if (newEntry != null)
                {
                    XmlElement newNode = xDoc.CreateElement("applicationStatus");
                    newNode.SetAttribute("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                    newNode.SetAttribute("KIOSK", whichKiosk);
                    newNode.SetAttribute("ISRUNNING", whichStatus.ToString());
                    newEntry.AppendChild(newNode);


                    flagForSaveXML = true;
                }

                recordDataToFile();
            }
        }*/

        #endregion remoteCalls

        #region localFileRW

        private void recordDataToFile()
        {
            if (XMLalreadyLoaded)
            {
                if (flagForSaveXML)
                {
                    if (currentDateOfFile != getCurrentDateFormatted()) // if local date has changed (simpler test)
                        createLocalFileIfNecessary(); // take a moment to create new file;
                    flagForSaveXML = false;
                    try
                    {
                        //createLocalFileIfNecessary();
                        System.Diagnostics.Debug.WriteLine("[FILE IO] saving updates to file");
                        xDoc.Save(localFileAndPath);
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("[FILE IO] error saving updates to XML file");
                    }
                }
            }
        }

        private bool isXMLFileAvailable()
        {
            XMLFileIsAvailable = false;

            if (File.Exists(MakeAbsolutePath(localFileAndPath)))
            {
                try
                {
                    //xDoc.Load(localFileAndPath); // testing with server service
                    XMLFileIsAvailable = true;
                }
                catch (Exception e)
                {
                    XMLFileIsAvailable = false;
                    System.Diagnostics.Debug.WriteLine("[FILE IO] ERROR: XML found but won't load: " + localFileAndPath + " exception: " + e.Source);
                }
            }
            else
            {
                XMLFileIsAvailable = false;
                System.Diagnostics.Debug.WriteLine("[FILE IO] ERROR: XML not found: " + localFileAndPath);
                //MessageBox.Show("[READXML] ERROR: XML not found: " + targetFileName); // this interrupts program
            }

            return XMLFileIsAvailable;
        }


        private void createNewXMLFile()
        {
            System.Diagnostics.Debug.WriteLine("[FILE IO] creating new XML file");
            XMLalreadyLoaded = false;
            currentDateOfFile = getCurrentDateFormatted();

            try
            {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.Indent = true;
                xmlWriterSettings.NewLineOnAttributes = true;

                XmlWriter writer = XmlWriter.Create(localFileAndPath, xmlWriterSettings);

                writer.WriteStartDocument();
                writer.WriteStartElement("RecordOfExhibitEvents");

                // <nextPortInfo UTCTIME="2017-03-29|13:36:57" LOCALDATEOFPORTCHANGE="2017-03-29" LOCALTIMEOFPORTCHANGE="08:15" DESTINATION="FORT LAUDERDALE" ETADATE="2017-03-29" ETATIME="07:00"/>
                writer.WriteStartElement("fileCreation");
                writer.WriteAttributeString("LOCALPCTIME", getCurrentDateAndTimeFormatted());
                writer.WriteEndElement();


                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[FILE IO] error creating new XML file: " + e.Message);
            }
        }

        #endregion localFileRW

        private string MakeAbsolutePath(string file)
        {
            string result = file;
            //create folder if it does not exist
            string folder = Path.GetDirectoryName(result);
            if (folder == String.Empty)
            {
                //if so prepend current directory
                result = Directory.GetCurrentDirectory() + "\\" + file;
            }

            return result;
        }
    }
}