using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageUpdater
{
    public static class FileHelpers
    {
        public static bool SaveToFile(String filename, String text, bool append = false)
        {
            try
            {
                string exist = "";
                if (append && File.Exists(filename))
                    exist = LoadFile(filename);

                var fs = new FileStream(filename, FileMode.Create);
                var sw = new StreamWriter(fs);
                sw.Write(text);
                sw.Close();
                fs.Close();
                return true;
            }
            catch (Exception e)
            {
            }
            return false;
        }

        public static string LoadFile(String filename)
        {
            try
            {
                var fs = new FileStream(filename, FileMode.Open);
                var sr = new StreamReader(fs);
                string filestr = sr.ReadToEnd();
                sr.Close();
                fs.Close();
                return filestr;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// get all the files under a folder
        /// </summary>
        /// <param name="absolutePath">must be the absolute path, not the relative path</param>
        /// <returns></returns>
        public static IEnumerable<string> GetFilesRecursive(string absolutePath, string searchpattern)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(absolutePath, searchpattern);
            foreach (string fileName in fileEntries)
            {
                // do something with fileName
                yield return fileName;
            }

            // Recurse into subdirectories of this directory.
            string[] subdirEntries = Directory.GetDirectories(absolutePath);

            var ret = new List<string>();
            foreach (string subdir in subdirEntries)
            {
                try
                {
                    // Do not iterate through reparse points
                    if ((File.GetAttributes(subdir) &
                         FileAttributes.ReparsePoint) !=
                        FileAttributes.ReparsePoint)

                        ret.AddRange(GetFilesRecursive(subdir, searchpattern));
                }
                catch
                {
                }
            }

            foreach (string r in ret)
                yield return r;
        }
    }
}
