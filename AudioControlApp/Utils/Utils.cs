using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace AudioControlApp
{
    public class Utilities
    {
        //calculates the relative path between mainDir to absoluteir
        public static string EvaluateRelativePath(string mainDirPath, string absoluteFilePath)
        {
            string[] firstPathParts = mainDirPath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string[] secondPathParts = absoluteFilePath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

            string seperator = "/";

            int sameCounter = 0;
            for (int i = 0; i < Math.Min(firstPathParts.Length, secondPathParts.Length); i++)
            {
                if (!firstPathParts[i].ToLower().Equals(secondPathParts[i].ToLower()))
                {
                    break;
                }
                sameCounter++;
            }

            if (sameCounter == 0)
            {
                return absoluteFilePath;
            }

            string newPath = String.Empty;
            for (int i = sameCounter; i < firstPathParts.Length; i++)
            {
                if (i > sameCounter)
                {
                    newPath += seperator;//Path.DirectorySeparatorChar;
                }
                newPath += "..";
            }
            if (newPath.Length == 0)
            {
                newPath = ".";
            }
            for (int i = sameCounter; i < secondPathParts.Length; i++)
            {
                newPath += seperator;//Path.DirectorySeparatorChar;
                newPath += secondPathParts[i];
            }
            return newPath;
        }

        public static string MakeAbsolutePath(string file)
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


        //round about way to convert int array to bytes
        public static byte[] IntArrayToByteArray(int[] arr, int length = -1)
        {
            byte[] raw = new byte[arr.Length * 4];
            if (length == -1) length = arr.Length;

            int offset = 0;
            for (int i = 0; i < length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(arr[i]), 0, raw, offset, 4);
                offset += 4;
            }
            return raw;
        }


        //round about way to convert float array to bytes (same as int to byte array)
        public static byte[] FloatArrayToByteArray(float[] arr, int length = -1)
        {
            byte[] raw = new byte[arr.Length * 4];
            if (length == -1) length = arr.Length;

            int offset = 0;
            for (int i = 0; i < length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(arr[i]), 0, raw, offset, 4);
                offset += 4;
            }
            return raw;
        }
    }
}
