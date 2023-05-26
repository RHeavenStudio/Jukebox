using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;

namespace Jukebox 
{
    public class RIQReader
    {
        static string tmpDir = Application.temporaryCachePath + "/RIQCache/";

        /// <summary>
        /// Extracts the contents of a RIQ file into a temporary directory.
        /// For Heaven Studio and other Unity Engine games, this uses the normal temporaryCachePath
        /// </summary>
        /// <param name="path">path to the .riq file</param>
        /// <returns>path to the extracted riq contents</returns>
        public static string ExtractRiq(string path)
        {
            if (path == string.Empty || path == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");
            if (!File.Exists(path)) throw new System.IO.FileNotFoundException("path", $"RIQ file does not exist at path {path}");

            try
            {
                ZipFile.ExtractToDirectory(path, tmpDir, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error extracting RIQ file: {e.Message}");
                throw e;
            }

            return tmpDir;
        }

        /// <summary>
        /// reads a .riq json file into a RiqBeatmap object
        /// </summary>
        /// <param name="path">directory to extracted riq JSON</param>
        /// <returns>an instance of RiqBeatmap</returns>
        public static RiqBeatmap ReadRiq()
        {
            if (tmpDir == string.Empty || tmpDir == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");

            string jsonPath = tmpDir + "remix.json";
            if (!File.Exists(jsonPath)) throw new System.IO.FileNotFoundException("path", $"riq chart file does not exist at path {jsonPath}, was an RIQ file properly extracted?");
            string json = File.ReadAllText(jsonPath);
            RiqBeatmap beatmap = new RiqBeatmap(json);

            return beatmap;
        }
    }
}