using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;

namespace Jukebox 
{
    /// <summary>
    /// Handles file I/O with riq files
    /// Methods here can be changed to suit the use case of the game
    /// (for example, you may want to load chart contents directly into memory instead of using a file cache)
    /// </summary>
    public class RiqFileHandler
    {
        static string tmpDir = Application.temporaryCachePath + "/RIQCache/";
        static AudioClip audioClip;

        public static AudioClip LoadedAudioClip
        {
            get
            {
                return audioClip;
            }
        }

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
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
                ZipFile.ExtractToDirectory(path, tmpDir, true);

                // if we have a legacy style song.ogg, rename it to song.bin
                if (File.Exists(tmpDir + "song.ogg"))
                {
                    File.Move(tmpDir + "song.ogg", tmpDir + "song.bin");
                }
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

        /// <summary>
        /// asynchronously creates an audio clip to stream from the song.ogg file in the temporary directory
        /// </summary>
        public static IEnumerator LoadSong()
        {
            string url = "file://" + tmpDir + "song.bin";
            audioClip = null;
            if (!File.Exists(tmpDir + "song.bin")) throw new System.IO.FileNotFoundException("path", $"Chart song file does not exist at path {tmpDir + "song.bin"}");
            using (var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();
            
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log($"error loading song: {www.error}");
                    yield break;
                }
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;
                audioClip = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
                yield return null;
            }
        }

        /// <summary>
        /// writes a beatmap to the "remix.json" file in the temporary cache
        /// </summary>
        /// <param name="beatmap">RiqBeatmap to serialize</param>
        public static void WriteRiq(RiqBeatmap beatmap)
        {
            string jsonPath = tmpDir + "remix.json";
            string json = beatmap.Serialize();
            File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// copies a song file to the temporary cache, renaming it to "song.bin"
        /// </summary>
        /// <param name="songPath">path to a song file</param>
        public static void WriteSong(string songPath)
        {
            if (songPath == string.Empty || songPath == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");
            if (!File.Exists(songPath)) throw new System.IO.FileNotFoundException("path", $"RIQ file does not exist at path {songPath}");

            // check if songPath is a valid audio file
            try
            {
                using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + songPath, AudioType.UNKNOWN))
                {
                    www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Debug.Log($"error loading song: {www.error}");
                        throw new System.Exception($"error loading song: {www.error}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading song: {e.Message}");
                throw e;
            }

            string songDest = tmpDir + "song.bin";
            File.Copy(songPath, songDest, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destPath"></param>
        public static void PackRiq(string destPath)
        {
            if (tmpDir == string.Empty || tmpDir == null) throw new System.ArgumentNullException("path", "temporary directory cannot be null or empty");
            if (destPath == string.Empty || destPath == null) throw new System.ArgumentNullException("path", "destination path cannot be null or empty");

            string jsonPath = tmpDir + "remix.json";
            if (!File.Exists(jsonPath)) throw new System.IO.FileNotFoundException("path", $"riq chart file does not exist at path {jsonPath}, was an RIQ file properly extracted?");

            ZipFile.CreateFromDirectory(tmpDir, destPath);
        }
    }
}