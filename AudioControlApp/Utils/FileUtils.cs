using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace AudioControlApp
{
    public class FileUtils
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
    }
}
