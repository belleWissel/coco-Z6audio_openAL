using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using AudioControlApp.Utils;

namespace AudioControlApp.WallCommunicationsAndControl
{
    class WriteByteDataToFile
    {

        private string pathToDataFile = "";
        private bool directoryCreated = false;
        private int frame = 0;

        // constructor
        public WriteByteDataToFile()
        {

        }

        private void createDirectory()
        {

            DateTime time = DateTime.Now;              // Use current time
            string format = "yyyyMMdd_HHmm";    // Use this format
            string dir = "dataCapture_" + time.ToString(format);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            pathToDataFile = dir + "\\frame";
            directoryCreated = true;
        }

        public bool writeDataToFile(int[] data)
        {
            return writeDataToFile(Utilities.IntArrayToByteArray(data));
        }

        public bool writeDataToFile(byte[] byteArray)
        {
            if (!directoryCreated) createDirectory();

            frame++;
            System.Diagnostics.Debug.WriteLine("write file " + frame);
            string _FileName = pathToDataFile + frame.ToString("#0000") + ".dat";
            try
            {
                // Open file for reading
                System.IO.FileStream _FileStream = new System.IO.FileStream(_FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);

                // Writes a block of bytes to this stream using data from a byte array.
                _FileStream.BeginWrite(byteArray, 0, byteArray.Length, new AsyncCallback(WriteCallBack), _FileStream);

                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                System.Diagnostics.Debug.WriteLine("Exception caught in process: {0}", _Exception.ToString());
            }

            // error occured, return false
            return false;

        }

        public static void WriteCallBack(IAsyncResult ar)
        {
            System.Diagnostics.Debug.WriteLine("end write ");
            FileStream s = (FileStream)ar.AsyncState;
            s.EndWrite(ar);
            s.Close();
        }

    }
}
