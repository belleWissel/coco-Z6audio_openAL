using System;

using System.Xml;
using System.IO;
using System.Windows.Forms; // error message


namespace AudioControlApp.Utils
{
    public class LoadAudioSettingsFromXML
    {
        // ***************************************************************************
        // XML File Variables
        private XmlDocument xDoc;
        private bool XMLFileLoaded = false;

        // ***************************************************************************
        private string pathToXML = "";

        private bool dataIsReady = false;
        // ***************************************************************************

        public string ambientLoopFilePath = "resources\\";
        public string buildFilePath = "resources\\";
        public string highlightFilePath = "resources\\";
        public float ambientVol = 50;
        public float buildVol = 50;
        public float highlightVol = 50;
        public float duckedVol = 20;

        public float buildOverlapTime = 2;
        public float buildRampUpTime = 5;
        public float buildRampDownTime = 5;
        
        public float highlightOverlapTime = 2;
        public float highlightRampUpTime = 5;
        public float highlightRampDownTime = 5;
        
        public float ambientOverlap = 5;

        private static int maxNumberOfProgramInstances = 3;
        public int[] audioDevID = new int[maxNumberOfProgramInstances];

        
        private static int maxNumberOfAmbientLoopFilesPerChannel = 10;
        public int[,] actualNumberOfAmbientLoopFilesPerChannel = new int[maxNumberOfProgramInstances, 2]; // 0 = # on the left, 1 = # on the right
        public string[,] ambientFileName0 = new string[maxNumberOfAmbientLoopFilesPerChannel, 2]; // which file, which channel
        public int[,] ambientFileLength0 = new int[maxNumberOfAmbientLoopFilesPerChannel, 2]; // which file length, which channel
        public string[,] ambientFileName1 = new string[maxNumberOfAmbientLoopFilesPerChannel, 2]; // which file, which channel
        public int[,] ambientFileLength1 = new int[maxNumberOfAmbientLoopFilesPerChannel, 2]; // which file length, which channel
        public string[,] ambientFileName2 = new string[maxNumberOfAmbientLoopFilesPerChannel, 2]; // which file, which channel
        public int[,] ambientFileLength2 = new int[maxNumberOfAmbientLoopFilesPerChannel, 2]; // which file length, which channel

        private static int maxNumberOfStereoFiles = 5;

        public int[] actualNumberOfBuildFiles = new int[maxNumberOfProgramInstances]; 
        public string[] buildFileName = new string[maxNumberOfStereoFiles]; 
        public int[] buildFileLength = new int[maxNumberOfStereoFiles];

        public int[] actualNumberOfHighlightFiles = new int[maxNumberOfProgramInstances]; 
        public string[] highlightFileName = new string[maxNumberOfStereoFiles]; 
        public int[] highlightFileLength = new int[maxNumberOfStereoFiles];

        
        // ***************************************************************************
        
        public LoadAudioSettingsFromXML(string whichSharedAssetPath)
        {
            pathToXML = whichSharedAssetPath + "XML//AudioSettings.xml";


            // reset values
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
                    System.Diagnostics.Debug.WriteLine("[audioXML] ERROR: XML Page Not Found: " + whichXMLFileAndPath + " exception: " + e.Source);
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
                whichNode = "AudioSettings/MasterAudioSettings";
                XmlNode parentNode = xDoc.SelectSingleNode(whichNode);

                string vol01 = parentNode.SelectSingleNode("MasterVolume").Attributes.GetNamedItem("ambientLevels").InnerText;
                string vol02 = parentNode.SelectSingleNode("MasterVolume").Attributes.GetNamedItem("duckedLevels").InnerText;
                string vol03 = parentNode.SelectSingleNode("MasterVolume").Attributes.GetNamedItem("buildLevels").InnerText;
                string vol04 = parentNode.SelectSingleNode("MasterVolume").Attributes.GetNamedItem("highlightLevels").InnerText;
                
                string build01 = parentNode.SelectSingleNode("BuildupTiming").Attributes.GetNamedItem("secondsOfOverlap").InnerText;
                string build02 = parentNode.SelectSingleNode("BuildupTiming").Attributes.GetNamedItem("rampup").InnerText;
                string build03 = parentNode.SelectSingleNode("BuildupTiming").Attributes.GetNamedItem("rampdown").InnerText;
                
                string ambOver = parentNode.SelectSingleNode("AmbientOverlap").Attributes.GetNamedItem("secondsOfOverlap").InnerText;
                
                string high01 = parentNode.SelectSingleNode("HighlightTiming").Attributes.GetNamedItem("secondsOfOverlap").InnerText;
                string high02 = parentNode.SelectSingleNode("HighlightTiming").Attributes.GetNamedItem("rampup").InnerText;
                string high03 = parentNode.SelectSingleNode("HighlightTiming").Attributes.GetNamedItem("rampdown").InnerText;

                
                ambientVol = (float)Convert.ToDouble(vol01);
                buildVol = (float)Convert.ToDouble(vol02);
                highlightVol = (float)Convert.ToDouble(vol03);
                duckedVol = (float)Convert.ToDouble(vol04);
                ambientOverlap =  (float)Convert.ToDouble(ambOver);
                
                buildOverlapTime = (float)Convert.ToDouble(build01);
                buildRampUpTime = (float)Convert.ToDouble(build02);
                buildRampDownTime = (float)Convert.ToDouble(build03);
                
                highlightOverlapTime = (float)Convert.ToDouble(high01);
                highlightRampUpTime = (float)Convert.ToDouble(high02);
                highlightRampDownTime = (float)Convert.ToDouble(high03);

                whichNode = "AudioSettings/FileSettings";
                parentNode = xDoc.SelectSingleNode(whichNode); 
                
                string fol01 = parentNode.SelectSingleNode("AmbientLoopFolder").Attributes.GetNamedItem("folder").InnerText;
                string fol02 = parentNode.SelectSingleNode("BuildFolder").Attributes.GetNamedItem("folder").InnerText;
                string fol03 = parentNode.SelectSingleNode("HighlightFolder").Attributes.GetNamedItem("folder").InnerText;

                ambientLoopFilePath = fol01;
                buildFilePath = fol02;
                highlightFilePath = fol03;
                
                
                int programInstanceIDweAreReading;
                int channelWeAreReading;
                string type, name, length;
                string dev00;
                float inCaseLengthIsNotWhole;
                int closestApproxLength;
                XmlNodeList getAmbientPlayList; 
                // *************************************************************************** program ID 0

                for (programInstanceIDweAreReading = 0; programInstanceIDweAreReading < 3; ++programInstanceIDweAreReading)
                {
                    //programInstanceIDweAreReading = 0;
                    whichNode = "AudioSettings/PlaybackSettings/audioControl" + programInstanceIDweAreReading;
                    parentNode = xDoc.SelectSingleNode(whichNode);
                    dev00 = parentNode.SelectSingleNode("AudioDev").Attributes.GetNamedItem("id").InnerText;
                    audioDevID[programInstanceIDweAreReading] = Convert.ToInt16(dev00);

                    // *************************************************************************** program ID 0 Channel 0

                    for (channelWeAreReading = 0; channelWeAreReading < 2; ++channelWeAreReading)
                    {
                        //channelWeAreReading = 0;
                        whichNode = "AudioSettings/PlaybackSettings/audioControl" + programInstanceIDweAreReading + "/AmbientPlaylistChannel" + channelWeAreReading;
                        parentNode = xDoc.SelectSingleNode(whichNode);

                        getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                        actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;

                        for (i = 0; i < getAmbientPlayList.Count; ++i)
                        {
                            type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                            name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                            length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;
                            
                            inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                            closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);

                            if (programInstanceIDweAreReading == 0)
                            {
                                ambientFileName0[i, channelWeAreReading] = name;
                                ambientFileLength0[i, channelWeAreReading] = closestApproxLength;
                            }
                            else if (programInstanceIDweAreReading == 1)
                            {
                                ambientFileName1[i, channelWeAreReading] = name;
                                ambientFileLength1[i, channelWeAreReading] = closestApproxLength;
                            }
                            else if (programInstanceIDweAreReading == 2)
                            {
                                ambientFileName2[i, channelWeAreReading] = name;
                                ambientFileLength2[i, channelWeAreReading] = closestApproxLength;
                            }


                        }
                    }
                    
                    /*
                    // *************************************************************************** program ID 0 Channel 1
                    channelWeAreReading = 1;
                    whichNode = "AudioSettings/PlaybackSettings/audioControl" + programInstanceIDweAreReading + "/AmbientPlaylistChannel" + channelWeAreReading;

                    parentNode = xDoc.SelectSingleNode(whichNode);
                    getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                    actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                    for (i = 0; i < getAmbientPlayList.Count; ++i)
                    {
                        type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                        name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                        length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                        ambientFileName0[i, channelWeAreReading] = name;
                        inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                        closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                        ambientFileLength0[i, channelWeAreReading] = closestApproxLength;
                    }*/
                }

                XmlNodeList getBuildPlayList;
                XmlNodeList getHighlightPlayList;
                
                for (programInstanceIDweAreReading = 0; programInstanceIDweAreReading < 3; ++programInstanceIDweAreReading)
                {
                    whichNode = "AudioSettings/PlaybackSettings/audioControl" + programInstanceIDweAreReading + "/BuildList";
                    parentNode = xDoc.SelectSingleNode(whichNode);

                    getBuildPlayList = parentNode.SelectNodes("VoiceFile");
                    actualNumberOfBuildFiles[programInstanceIDweAreReading] = getBuildPlayList.Count;

                    for (i = 0; i < getBuildPlayList.Count; ++i)
                    {
                        type = getBuildPlayList[i].Attributes.GetNamedItem("type").InnerText;
                        name = getBuildPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                        length = getBuildPlayList[i].Attributes.GetNamedItem("length").InnerText;

                        inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                        closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);

                        buildFileName[i] = name;
                        buildFileLength[i] = closestApproxLength;
                    }
                    
                    whichNode = "AudioSettings/PlaybackSettings/audioControl" + programInstanceIDweAreReading + "/HighlightList";
                    parentNode = xDoc.SelectSingleNode(whichNode);

                    getHighlightPlayList = parentNode.SelectNodes("VoiceFile");
                    actualNumberOfHighlightFiles[programInstanceIDweAreReading] = getHighlightPlayList.Count;

                    for (i = 0; i < getHighlightPlayList.Count; ++i)
                    {
                        type = getHighlightPlayList[i].Attributes.GetNamedItem("type").InnerText;
                        name = getHighlightPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                        length = getHighlightPlayList[i].Attributes.GetNamedItem("length").InnerText;

                        inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                        closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);

                        highlightFileName[i] = name;
                        highlightFileLength[i] = closestApproxLength;
                    }
                    
                }


                /*
                // *************************************************************************** program ID 0
                //whichNode = "AudioSettings/PlaybackSettings/audioControl0";
                programInstanceIDweAreReading = 0;
                whichNode = "AudioSettings/PlaybackSettings/audioControl"+programInstanceIDweAreReading;

                parentNode = xDoc.SelectSingleNode(whichNode);

                dev00 = parentNode.SelectSingleNode("AudioDev").Attributes.GetNamedItem("id").InnerText;
                
                audioDevID[programInstanceIDweAreReading] = Convert.ToInt16(dev00);

                // *************************************************************************** program ID 0 Channel 0

                channelWeAreReading = 0;
                whichNode = "AudioSettings/PlaybackSettings/audioControl"+programInstanceIDweAreReading+"/AmbientPlaylistChannel"+channelWeAreReading;
                parentNode = xDoc.SelectSingleNode(whichNode);

                getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                

                
                actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                
                
                for (i = 0; i < getAmbientPlayList.Count; ++i)
                {
                    type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                    name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                    length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                    ambientFileName0[i, channelWeAreReading] = name;
                    inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                    closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                    ambientFileLength0[i, channelWeAreReading] = closestApproxLength;
                }
                // *************************************************************************** program ID 0 Channel 1
                channelWeAreReading = 1;
                whichNode = "AudioSettings/PlaybackSettings/audioControl"+programInstanceIDweAreReading+"/AmbientPlaylistChannel"+channelWeAreReading;
                
                parentNode = xDoc.SelectSingleNode(whichNode);
                getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                for (i = 0; i < getAmbientPlayList.Count; ++i)
                {
                    type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                    name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                    length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                    ambientFileName0[i, channelWeAreReading] = name;
                    inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                    closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                    ambientFileLength0[i, channelWeAreReading] = closestApproxLength;
                }
                
                // *************************************************************************** 
                whichNode = "PlaybackSettings/audioControl1";
                programInstanceIDweAreReading = 1;
                string dev01 = parentNode.SelectSingleNode("AudioDev").Attributes.GetNamedItem("id").InnerText;
                audioDevID[programInstanceIDweAreReading] = Convert.ToInt16(dev01);

                whichNode = "PlaybackSettings/audioControl1/AmbientPlaylistChannel0";
                channelWeAreReading = 0;
                parentNode = xDoc.SelectSingleNode(whichNode);
                getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                for (i = 0; i < getAmbientPlayList.Count; ++i)
                {
                    type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                    name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                    length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                    ambientFileName1[i, channelWeAreReading] = name;
                    inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                    closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                    ambientFileLength1[i, channelWeAreReading] = closestApproxLength;
                }
                whichNode = "PlaybackSettings/audioControl1/AmbientPlaylistChannel1";
                channelWeAreReading = 1;
                parentNode = xDoc.SelectSingleNode(whichNode);
                getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                for (i = 0; i < getAmbientPlayList.Count; ++i)
                {
                    type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                    name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                    length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                    ambientFileName1[i, channelWeAreReading] = name;
                    inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                    closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                    ambientFileLength1[i, channelWeAreReading] = closestApproxLength;
                }
                
                // ***************************************************************************
                whichNode = "PlaybackSettings/audioControl2";
                programInstanceIDweAreReading = 2;
                string dev02 = parentNode.SelectSingleNode("AudioDev").Attributes.GetNamedItem("id").InnerText;
                audioDevID[programInstanceIDweAreReading] = Convert.ToInt16(dev02);

                whichNode = "PlaybackSettings/audioControl2/AmbientPlaylistChannel0";
                channelWeAreReading = 0;
                parentNode = xDoc.SelectSingleNode(whichNode);
                getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                for (i = 0; i < getAmbientPlayList.Count; ++i)
                {
                    type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                    name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                    length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                    ambientFileName2[i, channelWeAreReading] = name;
                    inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                    closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                    ambientFileLength2[i, channelWeAreReading] = closestApproxLength;
                }
                whichNode = "PlaybackSettings/audioControl2/AmbientPlaylistChannel1";
                channelWeAreReading = 1;
                parentNode = xDoc.SelectSingleNode(whichNode);
                getAmbientPlayList = parentNode.SelectNodes("VoiceFile");
                actualNumberOfAmbientLoopFilesPerChannel[programInstanceIDweAreReading, channelWeAreReading] = getAmbientPlayList.Count;
                for (i = 0; i < getAmbientPlayList.Count; ++i)
                {
                    type = getAmbientPlayList[i].Attributes.GetNamedItem("type").InnerText;
                    name = getAmbientPlayList[i].Attributes.GetNamedItem("filename").InnerText;
                    length = getAmbientPlayList[i].Attributes.GetNamedItem("length").InnerText;

                    ambientFileName2[i, channelWeAreReading] = name;
                    inCaseLengthIsNotWhole = (float)Convert.ToDouble(length);
                    closestApproxLength = (int)Math.Round(inCaseLengthIsNotWhole);
                    ambientFileLength2[i, channelWeAreReading] = closestApproxLength;
                }
                */
                

                dataIsReady = true;
            }
            catch
            {
                dataIsReady = false;
                System.Diagnostics.Debug.WriteLine("[audioXML] ERROR: unable to read XM dataL");
            }
        }
    }
}