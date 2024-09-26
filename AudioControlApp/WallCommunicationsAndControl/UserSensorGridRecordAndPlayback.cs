using System;

using System.Xml;
using System.IO;
using System.Windows.Forms; // for error popup

namespace AudioControlApp.WallCommunicationsAndControl
{
    class UserSensorGridRecordAndPlayback
    {
        public bool recordingToFileIsEnabled = false;
        public bool dataReadFromFileSuccessfully = false;
        //**********************
        // XML File Variables
        private XmlDocument xDoc;
        private string localFileName = "testCaptureFile.xml";
        private bool XMLFileLoaded = false;

        private int actualResolutionX = 500;
        private int actualResolutionY = 500;
        private int actualResolutionZ = 255;//amount of depth levels to down-sample to
        private int transmissionRate = 0;
        // 3D region being sampled:
        private float actualWidth = 1000;
        private float actualStartY = 1000;
        private float actualHeight = 1000;
        private float actualEndZ = 500;
        private float actualStartZ = -500;

        private static int maxNumberOfFramesToRecord = 30 * 15;
        private int actualNumberOfFrames = 5;
        public bool isRecording = false;
        public bool isPlaying = false;
        private int currentFrame = 0;

        // *******************************
        // data that will be transmitted:
        // *******************************
        // linear array of smoothed EDGE values
        private float[] userEdgeDepthForTransmit;
        // linear array of which indexes are being sent
        private int[] userEdgeIndexes;                  // maximum number of possible mesh depth points * 2 values (x and y point)
        private int[] edgeIndexesPerFrame = new int[maxNumberOfFramesToRecord];
        private int runningEdgeIndexCounter = 0;

        // linear array of smoothed FILL values
        private float[] userMeshDepthForTransmit; // = new float[maxResolution * maxResolution];
        // linear array of which indexes are being sent
        private int[] userMeshIndexes; // = new int[maxResolution * maxResolution * 2]; // maximum number of possible mesh depth points * 2 values (x and y point)
        private int[] meshIndexesPerFrame = new int[maxNumberOfFramesToRecord];
        private int runningMeshIndexCounter = 0;


        public UserSensorGridRecordAndPlayback(int whichActualResX, int whichActualResY, int whichActualResZ, int whichTransRate, bool doRecordToFile, bool doPlayFromFile, string whichFileName)
        {
            if (doPlayFromFile)
                recordingToFileIsEnabled = false; // can't have it both ways
            else
                recordingToFileIsEnabled = true;

            actualResolutionX = whichActualResX;
            actualResolutionY = whichActualResY;
            actualResolutionZ = whichActualResZ;
            transmissionRate = whichTransRate;
            localFileName = whichFileName;

            // local memory storage of data for recording to file:
            if (recordingToFileIsEnabled) // set up internal arrays here
            {
                userEdgeDepthForTransmit = new float[maxNumberOfFramesToRecord * whichActualResX * whichActualResY];
                userEdgeIndexes = new int[maxNumberOfFramesToRecord * whichActualResX * whichActualResY * 2];

                userMeshDepthForTransmit = new float[maxNumberOfFramesToRecord * whichActualResX * whichActualResY];
                userMeshIndexes = new int[maxNumberOfFramesToRecord * whichActualResX * whichActualResY * 2];
            }
            else if (doPlayFromFile)
            {
                openDataXMLFIle();
            }
        }

        private void resetLocalArrays()
        {
            Array.Clear(userEdgeDepthForTransmit, 0, userEdgeDepthForTransmit.Length);
            Array.Clear(userEdgeIndexes, 0, userEdgeIndexes.Length);
            Array.Clear(userMeshDepthForTransmit, 0, userMeshDepthForTransmit.Length);
            Array.Clear(userMeshIndexes, 0, userMeshIndexes.Length);
            /*
            for (int i = 0; i < userEdgeDepthForTransmit.Length; ++i)
            {
                userEdgeDepthForTransmit[i] = 0;
                userEdgeIndexes[i * 2] = 0;
                userEdgeIndexes[(i * 2) + 1] = 0;
            }
            for (int i = 0; i < userMeshDepthForTransmit.Length; ++i)
            {
                userMeshDepthForTransmit[i] = 0;
                userMeshIndexes[i * 2] = 0;
                userMeshIndexes[(i * 2) + 1] = 0;
            }*/
        }

        public void updateMeasurementAreaSize(float whichWidth, float whichStartY, float whichHeight)
        {
            actualWidth = whichWidth;
            actualStartY = whichStartY;
            actualHeight = whichHeight;
        }
        public void updateMeasurementAreaDepth(float whichDepthStart, float whichDepthEnd)
        {
            actualStartZ = whichDepthStart;
            actualEndZ = whichDepthEnd;
        }
        public void startRecord()
        {
            resetLocalArrays();
            currentFrame = 0;
            runningEdgeIndexCounter = 0;
            isRecording = true;
        }

        public void stopRecord()
        {
            isRecording = false;
            if (currentFrame>0)
            {
                recordDataFromMemoryToLocalFile();
            }
        }

        public void startPlayback()
        {
            isPlaying = true;
            resetPlayback();
        }
        public void stopPlayback()
        {
            isPlaying = false;
        }
        public float[] getNextEdgeData()
        {
            float[] dataToReturn = new float[edgeIndexesPerFrame[currentFrame]];
            Array.Copy(userEdgeDepthForTransmit, runningEdgeIndexCounter, dataToReturn, 0, edgeIndexesPerFrame[currentFrame]);
            return dataToReturn;
        }
        public int[] getNextEdgeIndexData()
        {
            int[] dataToReturn = new int[edgeIndexesPerFrame[currentFrame]*2];
            Array.Copy(userEdgeIndexes, runningEdgeIndexCounter*2, dataToReturn, 0, edgeIndexesPerFrame[currentFrame]*2);
            return dataToReturn;
        }
        public float[] getNextMeshData()
        {
            float[] dataToReturn = new float[meshIndexesPerFrame[currentFrame]];
            Array.Copy(userMeshDepthForTransmit, runningMeshIndexCounter, dataToReturn, 0, meshIndexesPerFrame[currentFrame]);
            return dataToReturn;
        }
        public int[] getNextMeshIndexData()
        {
            int[] dataToReturn = new int[meshIndexesPerFrame[currentFrame] * 2];
            Array.Copy(userMeshIndexes, runningMeshIndexCounter * 2, dataToReturn, 0, meshIndexesPerFrame[currentFrame] * 2);
            return dataToReturn;
        }
        public void postDataRetreivedStep() // remember to call this step after the above are called...
        {
            runningEdgeIndexCounter += edgeIndexesPerFrame[currentFrame]; // this is a running counter to quickly index the data from the current frame
            runningMeshIndexCounter += meshIndexesPerFrame[currentFrame];

            currentFrame += 1;
            if (currentFrame >= actualNumberOfFrames)
                resetPlayback();
        }
        private void resetPlayback()
        {
            runningEdgeIndexCounter = 0;
            runningMeshIndexCounter = 0;
            currentFrame = 0;
        }

        /// <summary>
        /// Records ONE FRAME OF DATA TO LOCAL MEMORY
        /// </summary>
        /// <param name="edgeData"></param>
        /// <param name="edgeIndexes"></param>
        /// <param name="numberOfIncomingEdgePoints"></param>
        /// <param name="meshData"></param>
        /// <param name="meshIndexes"></param>
        /// <param name="numberOfIncomingMeshPoints"></param>
        public void recordFrame(float[] edgeData, int[] edgeIndexes, int numberOfIncomingEdgePoints, float[] meshData, int[] meshIndexes, int numberOfIncomingMeshPoints)
        {
            Array.Copy(edgeData, 0, userEdgeDepthForTransmit, runningEdgeIndexCounter, numberOfIncomingEdgePoints); // depth value? for each point
            Array.Copy(edgeIndexes, 0, userEdgeIndexes, runningEdgeIndexCounter * 2, numberOfIncomingEdgePoints * 2); // two indeces for each point (x and y)
            edgeIndexesPerFrame[currentFrame] = numberOfIncomingEdgePoints;
            runningEdgeIndexCounter += numberOfIncomingEdgePoints;

            Array.Copy(meshData, 0, userMeshDepthForTransmit, runningMeshIndexCounter, numberOfIncomingMeshPoints); // depth value? for each point
            Array.Copy(meshIndexes, 0, userMeshIndexes, runningMeshIndexCounter * 2, numberOfIncomingMeshPoints * 2); // two indeces for each point (x and y)
            meshIndexesPerFrame[currentFrame] = numberOfIncomingMeshPoints;
            runningMeshIndexCounter += numberOfIncomingMeshPoints;

            currentFrame += 1;
            if (currentFrame >= maxNumberOfFramesToRecord)
            {
                stopRecord();
            }
        }


        private void openDataXMLFIle()
        {
            xDoc = new XmlDocument();
            string XMLServiceAddress = localFileName;
            if (File.Exists(Utilities.MakeAbsolutePath(XMLServiceAddress)))
            {
                try
                {
                    xDoc.Load(XMLServiceAddress); // testing with server service
                    XMLFileLoaded = true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine("[DATA PLAYBACK] ERROR: XML Page Not Found: " + XMLServiceAddress + " exception: " + e.Source);
                }
            }
            else
            {
                MessageBox.Show("Error Locating Recorded Data File");

            }

            if (XMLFileLoaded)
            {
                readDataXMLFile();
            }
        }

        private void readDataXMLFile()
        {
            string tempString1 = "";
            int actualNumberOfEdgePoints = 0;
            int actualNumberOfFillPoints = 0;
            bool success = false;

            try
            {
                XmlNode getAnimLengthNode = xDoc.SelectSingleNode("UserSensorGridData/DataContext/Length");
                if (getAnimLengthNode != null)
                {
                    tempString1 = getAnimLengthNode.Attributes.GetNamedItem("Frames").InnerText;
                    actualNumberOfFrames = Convert.ToInt32(tempString1);
                    tempString1 = getAnimLengthNode.Attributes.GetNamedItem("TotalEdges").InnerText;
                    actualNumberOfEdgePoints = Convert.ToInt32(tempString1);
                    tempString1 = getAnimLengthNode.Attributes.GetNamedItem("TotalMesh").InnerText;
                    actualNumberOfFillPoints = Convert.ToInt32(tempString1);
                }
                XmlNode getAnimSizeNode = xDoc.SelectSingleNode("UserSensorGridData/DataContext/Size");
                if (getAnimSizeNode != null)
                {
                    tempString1 = getAnimSizeNode.Attributes.GetNamedItem("Width").InnerText;
                    actualWidth = Convert.ToInt32(tempString1);
                    tempString1 = getAnimSizeNode.Attributes.GetNamedItem("Height").InnerText;
                    actualHeight = Convert.ToInt32(tempString1);
                    tempString1 = getAnimSizeNode.Attributes.GetNamedItem("LowerEdge").InnerText;
                    actualStartY = Convert.ToInt32(tempString1);
                    tempString1 = getAnimSizeNode.Attributes.GetNamedItem("NearZ").InnerText;
                    actualStartZ = Convert.ToInt32(tempString1);
                    tempString1 = getAnimSizeNode.Attributes.GetNamedItem("FarZ").InnerText;
                    actualEndZ = Convert.ToInt32(tempString1);
                }
                XmlNode getAnimResNode = xDoc.SelectSingleNode("UserSensorGridData/DataContext/Resolution");
                if (getAnimResNode != null)
                {
                    tempString1 = getAnimResNode.Attributes.GetNamedItem("Horiz").InnerText;
                    actualResolutionX = Convert.ToInt32(tempString1);
                    tempString1 = getAnimResNode.Attributes.GetNamedItem("Vert").InnerText;
                    actualResolutionY = Convert.ToInt32(tempString1);
                    tempString1 = getAnimResNode.Attributes.GetNamedItem("Depth").InnerText;
                    actualResolutionZ = Convert.ToInt32(tempString1);
                }

                XmlNode getAnimRateNode = xDoc.SelectSingleNode("UserSensorGridData/DataContext/DataRate");
                if (getAnimRateNode != null)
                {
                    tempString1 = getAnimRateNode.Attributes.GetNamedItem("TransmitRate").InnerText;
                    transmissionRate = Convert.ToInt32(tempString1);
                }

                // finished reading context
                userEdgeDepthForTransmit = new float[actualNumberOfEdgePoints];
                userEdgeIndexes = new int[actualNumberOfEdgePoints * 2];

                userMeshDepthForTransmit = new float[actualNumberOfFillPoints];
                userMeshIndexes = new int[actualNumberOfFillPoints * 2];

                runningEdgeIndexCounter = 0;
                runningMeshIndexCounter = 0;

                XmlNode getFrameDataNode = xDoc.SelectSingleNode("UserSensorGridData/DataCaptured");
                if (getFrameDataNode != null)
                    success = readDataNodes(getFrameDataNode);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] Error reading data from file: exception: " + e.Source);
            }

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] data read from file successfully | frames = [" + actualNumberOfFrames + " ]");
                dataReadFromFileSuccessfully = true;
            }
            else
                System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] erorr encountered while reading local file");

        }

        private bool readDataNodes(XmlNode whichNode)
        {
            bool success = false;

            XmlNodeList frames = whichNode.SelectNodes("CaptureFrame");
            int numberOfFramesFound = frames.Count;
            if (numberOfFramesFound > maxNumberOfFramesToRecord)
                numberOfFramesFound = maxNumberOfFramesToRecord;

            if (numberOfFramesFound != actualNumberOfFrames)
                System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] Error reading data from file: frame count mismatch");

            for (int i=0; i<numberOfFramesFound; ++i)
            {
                success = readSingleDataNode(i, frames[i]);
            }

            return success;
        }
        private bool readSingleDataNode(int whichFrame, XmlNode whichDataNode)
        {
            string tempString1 = "";
            string tempString2 = "";
            string tempString3 = "";
            string tempString4 = "";
            int currentFrame = -1;
            int numberOfDataToParse = -1;
            int i;
            int tempIndex;
            //int currentIndexCounter = 0;

            XmlNode getFrameNode = whichDataNode.SelectSingleNode("Info");
            XmlNode getEdgesNode = whichDataNode.SelectSingleNode("Edges");
            XmlNode getFillsNode = whichDataNode.SelectSingleNode("Fills");

            if (getFrameNode != null)
            {
                tempString1 = getFrameNode.Attributes.GetNamedItem("FrameNumber").InnerText;
                currentFrame = Convert.ToInt32(tempString1);
            }
            else
                return false;

            if (getEdgesNode != null)
            {
                tempString1 = getEdgesNode.Attributes.GetNamedItem("DataCount").InnerText;
                tempString2 = getEdgesNode.Attributes.GetNamedItem("EdgeData").InnerText;
                tempString3 = getEdgesNode.Attributes.GetNamedItem("EdgeIndex").InnerText;

                numberOfDataToParse = Convert.ToInt32(tempString1);
                edgeIndexesPerFrame[currentFrame] = numberOfDataToParse;

                // read depth values ***************************************
                string[] split2 = tempString2.Split(new Char[] { ',' });
                //if (split2.Length != numberOfDataToParse)
                //    System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] error reading edge data from frame [" + currentFrame + ", " + split2.Length + " != " + numberOfDataToParse + " ]");

                for (i=0; i<numberOfDataToParse; ++i)
                {
                    tempString4 = split2[i];
                    tempIndex = runningEdgeIndexCounter + i;
                    if (tempIndex > userEdgeDepthForTransmit.Length) // out of bounds error
                        return false;
                    userEdgeDepthForTransmit[tempIndex] = (float)Convert.ToDouble(tempString4);
                }

                // read indeces  ***************************************
                string[] split3 = tempString3.Split(new Char[] { ',' });
                //if (split3.Length != numberOfDataToParse*2)
                //    System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] error reading edge data from frame [" + currentFrame + ", " + split3.Length + " != " + (numberOfDataToParse*2) + " ]");

                for (i = 0; i < numberOfDataToParse*2; ++i)
                {
                    tempString4 = split3[i];
                    tempIndex = (runningEdgeIndexCounter * 2) + i;
                    if (tempIndex > userEdgeIndexes.Length) // out of bounds error 
                        return false;
                    userEdgeIndexes[tempIndex] = Convert.ToInt32(tempString4);
                }
                runningEdgeIndexCounter += numberOfDataToParse;
            }
            else
                return false;

            if (getFillsNode != null)
            {
                tempString1 = getFillsNode.Attributes.GetNamedItem("DataCount").InnerText;
                tempString2 = getFillsNode.Attributes.GetNamedItem("MeshData").InnerText;
                tempString3 = getFillsNode.Attributes.GetNamedItem("MeshIndex").InnerText;

                numberOfDataToParse = Convert.ToInt32(tempString1);
                meshIndexesPerFrame[currentFrame] = numberOfDataToParse;

                // read depth values ***************************************
                string[] split2 = tempString2.Split(new Char[] { ',' });
                //if (split2.Length != numberOfDataToParse)
                //    System.Diagnostics.Debug.WriteLine("[DATA PLAYBACK] error reading mesh data from frame [" + currentFrame + " ]");

                for (i = 0; i < numberOfDataToParse; ++i)
                {
                    tempString4 = split2[i];
                    tempIndex = runningMeshIndexCounter + i;
                    if (tempIndex > userMeshDepthForTransmit.Length) // out of bounds error
                        return false;
                    userMeshDepthForTransmit[tempIndex] = (float)Convert.ToDouble(tempString4);
                }

                // read indeces  ***************************************
                string[] split3 = tempString3.Split(new Char[] { ',' });
                for (i = 0; i < numberOfDataToParse * 2; ++i)
                {
                    tempString4 = split3[i];
                    tempIndex = (runningMeshIndexCounter * 2) + i;
                    if (tempIndex > userMeshIndexes.Length) // out of bounds error
                        return false;
                    userMeshIndexes[tempIndex] = Convert.ToInt32(tempString4);
                }
                runningMeshIndexCounter += numberOfDataToParse;
            }
            else
                return false;

            return true;
        }
        // ***********************************************************************
        #region XMLFileIO

        private void recordDataFromMemoryToLocalFile()
        {
            int i, j;
            string tempString1 = "";
            string tempString2 = "";
            int localRunningEdgeIndexCounter = 0;
            int localRunningMeshIndexCounter = 0;

            try
            {
                XmlTextWriter writer = new XmlTextWriter(localFileName, null);
                writer.WriteStartDocument();
                writer.Formatting = Formatting.Indented;
                writer.WriteStartElement("UserSensorGridData");

                // ***********************************************************************
                writer.WriteStartElement("DataContext");

                writer.WriteStartElement("Length");
                writer.WriteAttributeString("Frames", Convert.ToString(currentFrame));
                writer.WriteAttributeString("TotalEdges", Convert.ToString(runningEdgeIndexCounter));
                writer.WriteAttributeString("TotalMesh", Convert.ToString(runningMeshIndexCounter));
                writer.WriteEndElement();

                writer.WriteStartElement("Size");
                writer.WriteAttributeString("Width", Convert.ToString(actualWidth));
                writer.WriteAttributeString("Height", Convert.ToString(actualHeight));
                writer.WriteAttributeString("LowerEdge", Convert.ToString(actualStartY));
                writer.WriteAttributeString("NearZ", Convert.ToString(actualStartZ));
                writer.WriteAttributeString("FarZ", Convert.ToString(actualEndZ));
                writer.WriteEndElement();

                writer.WriteStartElement("Resolution");
                writer.WriteAttributeString("Horiz", Convert.ToString(actualResolutionX));
                writer.WriteAttributeString("Vert", Convert.ToString(actualResolutionY));
                writer.WriteAttributeString("Depth", Convert.ToString(actualResolutionZ));
                writer.WriteEndElement();

                writer.WriteStartElement("DataRate");
                writer.WriteAttributeString("TransmitRate", Convert.ToString(transmissionRate));
                writer.WriteEndElement();

                writer.WriteEndElement();

                // **********************************************************************************************************************************************
                writer.WriteStartElement("DataCaptured");

                for (i = 0; i < currentFrame; ++i)
                {
                    writer.WriteStartElement("CaptureFrame");

                    writer.WriteStartElement("Info");
                    writer.WriteAttributeString("FrameNumber", Convert.ToString(i));
                    writer.WriteEndElement();

                    // ***********************************************************************
                    writer.WriteStartElement("Edges");
                    writer.WriteAttributeString("DataCount", Convert.ToString(edgeIndexesPerFrame[i]));
                    tempString1 = "";
                    tempString2 = "";
                    for (j = localRunningEdgeIndexCounter; j < (localRunningEdgeIndexCounter + edgeIndexesPerFrame[i]); ++j)
                    {
                        tempString1 += "" + userEdgeDepthForTransmit[j] + ", ";
                    }
                    writer.WriteAttributeString("EdgeData", tempString1);
                    for (j = localRunningEdgeIndexCounter * 2; j < (localRunningEdgeIndexCounter + edgeIndexesPerFrame[i]) * 2; ++j)
                    {
                        tempString2 += "" + userEdgeIndexes[j] + ", ";
                    }
                    writer.WriteAttributeString("EdgeIndex", tempString2);
                    localRunningEdgeIndexCounter += edgeIndexesPerFrame[i];
                    writer.WriteEndElement(); // end Edges
                    // ***********************************************************************

                    // ***********************************************************************
                    writer.WriteStartElement("Fills");
                    writer.WriteAttributeString("DataCount", Convert.ToString(meshIndexesPerFrame[i]));
                    tempString1 = "";
                    tempString2 = "";
                    for (j = localRunningMeshIndexCounter; j < (localRunningMeshIndexCounter + meshIndexesPerFrame[i]); ++j)
                    {
                        tempString1 += "" + userMeshDepthForTransmit[j] + ", ";
                    }
                    writer.WriteAttributeString("MeshData", tempString1);
                    for (j = localRunningMeshIndexCounter * 2; j < (localRunningMeshIndexCounter + meshIndexesPerFrame[i]) * 2; ++j)
                    {
                        tempString2 += "" + userMeshIndexes[j] + ", ";
                    }
                    writer.WriteAttributeString("MeshIndex", tempString2);
                    localRunningMeshIndexCounter += meshIndexesPerFrame[i];
                    writer.WriteEndElement(); // end fills
                    // ***********************************************************************

                    writer.WriteEndElement(); // end CaptureFrame
                } // proceed with next frame

                writer.WriteEndElement(); // end datacaptured
                // **********************************************************************************************************************************************

                writer.WriteEndElement(); // end UserSensorGridData

                writer.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("[RecordDataToFile] ERROR writing to XML: " + localFileName + " exception: " + e.Source);

            }


        }
        #endregion XMLFileIO
        // ***********************************************************************

    }
}
