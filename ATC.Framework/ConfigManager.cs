using ATC.Framework.Debugging;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using System;
using System.Text;

namespace ATC.Framework
{
    public static class ConfigManager
    {
        /// <summary>
        /// The default filename to use.
        /// </summary>
        public const string DefaultFilename = "systemConfig.json";

        /// <summary>
        /// The defaut directory under which to store files (/User)
        /// </summary>
        public static string DefaultDirectory
        {
            get
            {
                return string.Format("{0}{1}User", Directory.GetApplicationRootDirectory(), Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// Attempt to load the default filename from the default directory.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to.</typeparam>
        /// <returns>An object on sucesss, null on failure.</returns>
        public static T Read<T>()
             where T : class
        {
            return Read<T>(DefaultDirectory, DefaultFilename);
        }

        /// <summary>
        /// Attempt to load the specified filename from the specified directory.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to.</typeparam>
        /// <param name="directory">The directory from which to load.</param>
        /// <param name="filename">The filename to attempt to read.</param>
        /// <returns></returns>
        public static T Read<T>(string directory, string filename)
            where T : class
        {
            try
            {
                string filePath = GetFilePath(directory, filename);
                Tracer.PrintLine("ConfigManager.Load() attempting to read file: " + filePath);

                // read file and attempt to deserialize
                string json = File.ReadToEnd(filePath, Encoding.Default);
                var obj = JsonConvert.DeserializeObject<T>(json);

                Tracer.PrintLine("ConfigManager.Load() read file successfully.");
                return obj;
            }
            catch (FileNotFoundException)
            {
                Tracer.PrintLine("ConfigManager.Load() file not found.");
                return default;
            }
            catch (Exception ex)
            {
                Tracer.PrintLine("ConfigManager.Load() exception caught: ", ex);
                return default;
            }
        }

        /// <summary>
        /// Attempt to write to the default filename in the default directory.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <returns>True on success, false on failure.</returns>
        public static bool Write<T>(T obj)
            where T : class
        {
            return Write<T>(obj, DefaultDirectory, DefaultFilename);
        }

        /// <summary>
        /// Attempt to write to the specified filename in the specified directory.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="directory">The directory to save to.</param>
        /// <param name="filename">The filename to use.</param>
        /// <returns></returns>
        public static bool Write<T>(T obj, string directory, string filename)
            where T : class
        {
            try
            {
                string filePath = GetFilePath(directory, filename);
                Tracer.PrintLine("ConfigManager.Write() attempting to write to file: " + filePath);

                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);

                using (FileStream stream = File.Create(filePath))
                {
                    stream.Write(json, Encoding.Default);
                    stream.Close();
                }

                Tracer.PrintLine("ConfigManager.Write() wrote to file succesfully.");
                return true;
            }
            catch (Exception e)
            {
                Tracer.PrintLine("ConfigManager.Write() exception caught: ", e);
                return false;
            }
        }

        private static string GetFilePath(string directory, string filename)
        {
            return String.Format("{0}{1}{2}", directory, Path.DirectorySeparatorChar, filename);
        }
    }
}
