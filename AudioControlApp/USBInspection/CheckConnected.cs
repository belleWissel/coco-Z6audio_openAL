using System;
using System.Collections.Generic;
using System.Management;

using System.Windows.Forms; // for error popup
using System.IO;

using System.Text;

namespace AudioControlApp.USBInspection
{
    class CheckConnected
    {
        public string[] detectedListOfDeviceID = new string[4];
        public string[] storedListOfDeviceID = new string[4];
        public bool[] isConnected = new bool[4];
        private int maxNumberOfDevices = 4;
        public int numberOfDevices = 0;

        public CheckConnected()
        {
            System.Diagnostics.Debug.WriteLine("_____________________________________________");
            System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] init... ");
            openAndReadIdTextFile();

            //System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] Checking USB bus for connected depth cameras... ");
            //searchForCamerasOnUSBbus();
            //countAndAttributeConnectedCameras();
        }

        public int getIDFromPort(string whichPort)
        {
            int i, j;
            int valueToReturn = -1;
            // does it match this port? USB\VID_8086&PID_0B5C&MI_00\7&34F1C99C&1&0000
            // it returned this \\?\usb#vid_8086&pid_0b5c&mi_00#7&34f1c99c&1&0000#{e5323777-f976-4f5b-9b55-b94699c46e44}\global
            string whichTextFileDevice;
            string whichActualDevice;
            string importantChunk1;
            string importantChunk2;

            bool matchFound = false;

            for (i = 0; i < maxNumberOfDevices; ++i)
            {
                whichTextFileDevice = storedListOfDeviceID[i];
                
                // grab the chunks that matter:
                string[] devFromTextFile = whichTextFileDevice.Split('\\');
                string[] devFromSystem = whichPort.Split('#');

                if ((devFromTextFile.Length > 0) && (devFromSystem.Length >2)) // were there any good chunks
                {
                    importantChunk1 = devFromTextFile[0];
                    importantChunk2 = devFromSystem[2];
                    bool areAMatch = string.Equals(importantChunk1.ToUpper(), importantChunk2.ToUpper());
                    // 7&34F1C99C&1&0000 == 
                    // 7&34f1c99c&1&0000
                    if (areAMatch) // we found a match
                    {
                        matchFound = true;
                        System.Diagnostics.Debug.WriteLine("[CHECKCONNECT] found match [" + importantChunk1 + "] and ["+ importantChunk2 + "]");
                        valueToReturn = i;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[CHECKCONNECT] not a match [" + importantChunk1 + "] and ["+ importantChunk2 + "]");
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("[CHECKCONNECT] returning [" + valueToReturn + "]");
            return valueToReturn;
        }
        
        private void countAndAttributeConnectedCameras()
        {
            int i, j;

            string oldID;
            string newID;
            for (i = 0; i < maxNumberOfDevices; ++i) // assume nothing is connected
            {
                isConnected[i] = false;
            }

            // remove amp characters, replace with lc a for simplicity:
            bool doRemoveHampsterStands = false;

            for (i = 0; i < numberOfDevices; ++i)
            {
                oldID = detectedListOfDeviceID[i];
                StringBuilder b = new StringBuilder(oldID);
                b.Replace("&", "a");
                //System.Diagnostics.Debug.WriteLine(b);
                newID = b.ToString();

                if (doRemoveHampsterStands)
                    detectedListOfDeviceID[i] = newID;
                else
                    detectedListOfDeviceID[i] = oldID;
                
                System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] stored device #" + i + " ID: " + detectedListOfDeviceID[i]);
                
                for (j = 0; j < maxNumberOfDevices; ++j)
                {
                    if (detectedListOfDeviceID[i] == storedListOfDeviceID[j])
                    {
                        isConnected[j] = true;
                    }
                }
            }

            
            for (i = 0; i < numberOfDevices; ++i)
            {
                System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] found device #" + i + " ID: " + detectedListOfDeviceID[i]);
            }

            System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] USB scan complete.");
            System.Diagnostics.Debug.WriteLine("_____________________________________________");
        }
        
        private void searchForCamerasOnUSBbus()
        {
            
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;
            string Win32_USBControlerDevice = "Select * From Win32_USBControllerDevice";
            ObjectQuery query = new ObjectQuery(Win32_USBControlerDevice);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject mgmtObj in searcher.Get())
            {
                string strDeviceName = mgmtObj["Dependent"].ToString();
                string strQuotes = "'";
                strDeviceName = strDeviceName.Replace("\"", strQuotes);
                string[] arrDeviceName = strDeviceName.Split('=');
                strDeviceName = arrDeviceName[1];
                string Win32_PnPEntity = "Select * From Win32_PnPEntity " + "Where DeviceID =" + strDeviceName;
                ManagementObjectSearcher mySearcher = new ManagementObjectSearcher(Win32_PnPEntity);
                foreach (ManagementObject mobj in mySearcher.Get())
                {
                    

                    string strDeviceID = mobj["DeviceID"].ToString();
                    string[] arrDeviceID = strDeviceID.Split('\\');

                    //if (mobj["Description"].ToString() == "PrimeSense PS1080")
                    //if (mobj["Description"].ToString() == "USB Video Device") 

                    string strDeviceDisplayName = "NA";
                    if (mobj["Name"] != null)
                    {
                        strDeviceDisplayName = mobj["Name"].ToString();
                    }
                    
                    if (strDeviceDisplayName == "Intel(R) RealSense(TM) Depth Camera 455  Depth")
                    {
                        System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] found camera device:");

                        if (mobj["Description"] != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] Device Description = " +
                                                               mobj["Description"].ToString());
                        }

                        if (mobj["Manufacturer"] != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] Device Manufacturer = " + mobj["Manufacturer"].ToString());
                        }
                        System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] Device Version ID & Vendor ID = " + arrDeviceID[1]);
                        System.Diagnostics.Debug.WriteLine("[CHECKCONNECTED] Device ID = " + arrDeviceID[2].Trim('{', '}'));
                        detectedListOfDeviceID[numberOfDevices] = arrDeviceID[2].Trim('{', '}');
                        numberOfDevices += 1;
                        if (numberOfDevices > maxNumberOfDevices) // keep within bounds of array
                        {
                            numberOfDevices = maxNumberOfDevices - 1;
                        }
                    }
                }
            }

            searcher.Dispose();
        }


        private void openAndReadIdTextFile()
        {
            int i;

            string filePath = "xml/DepthCameraSysID.txt";
            string[] collectionOfInputInformation;
            String readLineFromFile;
            string infoWeWant = "";
            int numberOfSplits = 0;
            int finalPiece = 0;

            int foundIdCounter = 0;
            string[] ids = new string[4];

            for (i = 0; i < ids.Length; ++i)
            {
                ids[i] = "";
            }
            if (File.Exists(Utilities.MakeAbsolutePath(filePath)))
            {
                StreamReader sr = File.OpenText(filePath);
                using (sr)
                {
                    while ((readLineFromFile = sr.ReadLine()) != null)
                    {
                        infoWeWant = "";
                        collectionOfInputInformation = readLineFromFile.Split('\\');

                        numberOfSplits = collectionOfInputInformation.Length;
                        finalPiece = numberOfSplits -1;

                        infoWeWant = collectionOfInputInformation[finalPiece];
                        System.Diagnostics.Debug.WriteLine("read file device ID: " + infoWeWant);
                        
                        ids[foundIdCounter] = infoWeWant;

                        foundIdCounter += 1;
                        if (foundIdCounter > ids.Length)
                            foundIdCounter = ids.Length - 1;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[CHECK_CONNECTED] error reading xml file");
            }

            // assign those ID's that we found:
            storedListOfDeviceID[0] = ids[0];
            storedListOfDeviceID[1] = ids[1];
            storedListOfDeviceID[2] = ids[2];
            storedListOfDeviceID[3] = ids[3];

        }
    }
}
