using System;
// read/write to XML:
using System.Xml;
using System.IO;
using System.Windows.Forms; // for error popup

namespace AudioControlApp.RealSenseCamera
{
    class RSPositionCalibration
    {
        //**********************
        // XML File Variables
        private XmlDocument xDoc;
        private string localFileName;
        private bool XMLFileLoaded = false;

        private int cameraID = 0;

        //**********************
        public double sensorPosnX = 0;
        public double sensorPosnY = -800;
        public double sensorPosnZ = 0;

        public double addedOffsetX = 0;
        public double addedOffsetY = 0;
        public double addedOffsetZ = 0;

        public double sensorTiltX = 0;
        public double sensorTiltY = 0;
        public double sensorYaw = 0;


        private bool kinectIsRotated;
        private bool kinectIsRotatedCW;


        public RSPositionCalibration(int whichCameraID, bool isRotated, bool isRotatedCW)
        {
            cameraID = whichCameraID;

            localFileName = "xml/DepthCameraPosn" + cameraID + ".xml";

            kinectIsRotated = isRotated;
            kinectIsRotatedCW = isRotatedCW;

            openPosnXMLFIle();
        }

        public void rotateSensorHeadX(bool rotateUp)
        {
            if (rotateUp)
                sensorTiltX += 1;
            else
                sensorTiltX -= 1;
           //System.Diagnostics.Debug.WriteLine("[DEPTHCALIB " + cameraID + "] new sensor Tilt:{0}", sensorTiltX);
        }
        public void rotateSensorHeadY(bool rotateUp)
        {
            if (rotateUp)
                sensorTiltY += 1;
            else
                sensorTiltY -= 1;
            //System.Diagnostics.Debug.WriteLine("[DEPTHCALIB " + cameraID + "] new sensor Tilt:{0}", sensorTiltX);
            writeToPosnXMLFIle();
        }
        public void rotateSensorHeadZ(bool rotateLeft)
        {
            if (rotateLeft)
                sensorYaw += 1;
            else
                sensorYaw -= 1;

            //System.Diagnostics.Debug.WriteLine("[DEPTHCALIB " + cameraID + "] new sensor Yaw:{0}", sensorYaw);
        }
        public void moveSensorHeadZ(bool lowerIt, bool jumpFar)
        {
            double displace = 20;
            if (jumpFar) displace = 100;

            if (lowerIt)
            {
                addedOffsetZ -= displace;
            }
            else
            {
                addedOffsetZ += displace;
            }
            //System.Diagnostics.Debug.WriteLine("[KINECTDATA] new sensor Z:{0},{0},{0}", sensorPosnX, sensorPosnY, sensorPosnZ);
            System.Diagnostics.Debug.WriteLine("[DEPTHCALIB " + cameraID + "] new sensor offset:[" + addedOffsetX + ", " + addedOffsetY + ", " + addedOffsetZ + "]");
            writeToPosnXMLFIle();
        }
        public void moveSensorHeadY(bool moveOut, bool jumpFar)
        {
            double displace = 20;
            if (jumpFar) displace = 100;
            if (moveOut)
            {
                addedOffsetY -= displace;
            }
            else
            {
                addedOffsetY += displace;
            }
            //System.Diagnostics.Debug.WriteLine("[KINECTDATA] new sensor X:{0},{0},{0}", sensorPosnX, sensorPosnY, sensorPosnZ);
            System.Diagnostics.Debug.WriteLine("[DEPTHCALIB " + cameraID + "] new sensor offset:[" + addedOffsetX + ", " + addedOffsetY + ", " + addedOffsetZ + "]");

            writeToPosnXMLFIle();
        }
        public void moveSensorHeadX(bool moveLeft, bool jumpFar)
        {
            double displace = 20;
            if (jumpFar) displace = 100;
            if (moveLeft)
            {
                addedOffsetX -= displace;
            }
            else
            {
                addedOffsetX += displace;
            }
            //System.Diagnostics.Debug.WriteLine("[KINECTDATA] new sensor X:{0},{0},{0}", sensorPosnX, sensorPosnY, sensorPosnZ);
            System.Diagnostics.Debug.WriteLine("[DEPTHCALIB " + cameraID + "] new sensor offset:[" + addedOffsetX + ", " + addedOffsetY + ", " + addedOffsetZ + "]");

            writeToPosnXMLFIle();
        }

        #region XMLFileIO
       private void openPosnXMLFIle()
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
                    System.Diagnostics.Trace.WriteLine("[DEPTHCALIB " + cameraID + "] ERROR: XML Page Not Found: " + XMLServiceAddress + " exception: " + e.Source);
                }
            }
            else
            {
                MessageBox.Show("Error Locating XML/DepthCameraPosn file");

            }

            if (XMLFileLoaded)
            {
                readPosnXMLFIle();
            }

        }
        private void readPosnXMLFIle()
        {
            double newXPosn = -1;
            double newYPosn = -1;
            double newZPosn = -1;

            double newXRotn = -1;
            double newYRotn = -1;
            double newZRotn = -1;

            double newAddedXPosn = -1;
            double newAddedYPosn = -1;
            double newAddedZPosn = -1;

            XmlNode posnNodes = xDoc.SelectSingleNode("DepthCameraPosnData/DepthCamera/Position");
            if (posnNodes != null)
            {
                newXPosn = Convert.ToDouble(posnNodes.Attributes.GetNamedItem("x").InnerText);
                newYPosn = Convert.ToDouble(posnNodes.Attributes.GetNamedItem("y").InnerText);
                newZPosn = Convert.ToDouble(posnNodes.Attributes.GetNamedItem("z").InnerText);
            }

            XmlNode rotnNodes = xDoc.SelectSingleNode("DepthCameraPosnData/DepthCamera/Rotation");
            if (posnNodes != null)
            {
                newXRotn = Convert.ToDouble(rotnNodes.Attributes.GetNamedItem("x").InnerText);
                newYRotn = Convert.ToDouble(rotnNodes.Attributes.GetNamedItem("y").InnerText);
                newZRotn = Convert.ToDouble(rotnNodes.Attributes.GetNamedItem("z").InnerText);
            }

            XmlNode addedPosnNodes = xDoc.SelectSingleNode("DepthCameraPosnData/AdditionalOffsets/Position");
            if (posnNodes != null)
            {
                newAddedXPosn = Convert.ToDouble(addedPosnNodes.Attributes.GetNamedItem("x").InnerText);
                newAddedYPosn = Convert.ToDouble(addedPosnNodes.Attributes.GetNamedItem("y").InnerText);
                newAddedZPosn = Convert.ToDouble(addedPosnNodes.Attributes.GetNamedItem("z").InnerText);
            }


            System.Diagnostics.Trace.WriteLine("[DEPTHCALIB " + cameraID + "] read posn: [" + newXPosn + ", " + newYPosn + ", " + newZPosn + "] rotn: [" + newXRotn + ", " + newYRotn + ", " + newZRotn + "]");


            if (newXPosn != -1)
            {
                sensorPosnX = newXPosn;
                sensorPosnY = newYPosn;
                sensorPosnZ = newZPosn;
            }

            if (newXRotn != -1)
            {
                sensorTiltX = newXRotn;
                sensorTiltY = newYRotn;
                sensorYaw = newZRotn;
            }

            if (newAddedXPosn != -1)
            {
                addedOffsetX = newAddedXPosn;
                addedOffsetY = newAddedYPosn;
                addedOffsetZ = newAddedZPosn;
            }

        }

        private void writeToPosnXMLFIle()
        {
            try
            {
                XmlTextWriter writer = new XmlTextWriter(localFileName, null);

                writer.WriteStartDocument();
                writer.Formatting = Formatting.Indented;
                writer.WriteStartElement("DepthCameraPosnData");

                writer.WriteStartElement("DepthCamera");

                writer.WriteStartElement("Camera");
                writer.WriteAttributeString("ID", Convert.ToString(cameraID));
                writer.WriteEndElement();

                writer.WriteStartElement("Position");
                writer.WriteAttributeString("x", Convert.ToString(sensorPosnX));
                writer.WriteAttributeString("y", Convert.ToString(sensorPosnY));
                writer.WriteAttributeString("z", Convert.ToString(sensorPosnZ));
                writer.WriteEndElement();

                writer.WriteStartElement("Rotation");
                writer.WriteAttributeString("x", Convert.ToString(sensorTiltX));
                writer.WriteAttributeString("y", Convert.ToString(sensorTiltY));
                writer.WriteAttributeString("z", Convert.ToString(sensorYaw));
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteStartElement("AdditionalOffsets");

                writer.WriteStartElement("Position");
                writer.WriteAttributeString("x", Convert.ToString(addedOffsetX));
                writer.WriteAttributeString("y", Convert.ToString(addedOffsetY));
                writer.WriteAttributeString("z", Convert.ToString(addedOffsetZ));
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("[DEPTHCALIB " + cameraID + "] ERROR writing to XML: " + localFileName + " exception: " + e.Source);
            }
        }

        #endregion XMLFileIO
    }
}
