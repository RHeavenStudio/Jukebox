using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Jukebox 
{
    /// <summary>
    /// Handles file I/O with riq files
    /// Methods here can be changed to suit the use case of the game
    /// (for example, you may want to load chart contents directly into memory instead of using a file cache)
    /// </summary>
    public static class RiqFileHandler
    {
        public delegate string AudioConverterHandler(string filePath, AudioType audioType, string specificType);
        public static AudioConverterHandler AudioConverter;

        static Guid? CacheID = null;
        static string tmpDir = Application.temporaryCachePath + "/RIQCache/";
        static AudioClip streamedAudioClip;
        static float[] songChunk;
        static bool songChunkLock = false;

        public static string RiqCachePath => tmpDir;

        public static AudioClip StreamedAudioClip
        {
            get
            {
                return streamedAudioClip;
            }
        }

        public static float[] LastSongChunk
        {
            get
            {
                return songChunk;
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
            if (IsCacheLocked()) throw new System.IO.IOException($"RIQ cache is locked, cannot extract RIQ file at path {path}");

            try
            {
                ClearCache();
                ZipFile.ExtractToDirectory(path, tmpDir, true);

                // if we have a legacy style song.ogg, rename it to song.bin
                if (File.Exists(tmpDir + "song.ogg"))
                {
                    File.Move(tmpDir + "song.ogg", tmpDir + "song.bin");
                }

                if (File.Exists(tmpDir + "song.bin"))
                {
                    FileInfo inf = new FileInfo(tmpDir + "song.bin");
                    if (inf.Length == 0) File.Delete(tmpDir + "song.bin");
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
        /// creates an AudioClip from the song file in the temporary cache
        /// </summary>
        /// <param name="stream">should the audio be streamed?</param>
        /// <exception cref="System.IO.FileNotFoundException">song file doesn't exist in temporary cache</exception>
        /// <exception cref="System.IO.InvalidDataException">song file is of unknown type</exception>
        public static IEnumerator LoadSong(bool stream = true)
        {
            string url = "file://" + tmpDir + "song.bin";
            streamedAudioClip = null;
            if (!File.Exists(tmpDir + "song.bin")) throw new System.IO.FileNotFoundException($"Chart song file does not exist at path {tmpDir + "song.bin"}");

            FileInfo inf = new FileInfo(tmpDir + "song.bin");
            if (inf.Length == 0) throw new System.IO.FileNotFoundException($"Chart song file does not exist at path {tmpDir + "song.bin"}");
            
            AudioType audioType = AudioFormats.GetAudioType(tmpDir + "song.bin", out _);
            if (audioType == AudioType.UNKNOWN) throw new System.IO.InvalidDataException($"file at path {tmpDir + "song.bin"} is of unknown type");
            
            using (var www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                ((DownloadHandlerAudioClip)www.downloadHandler).compressed = false;
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = stream;
                www.SendWebRequest();
                while (!(www.result == UnityWebRequest.Result.ConnectionError) && www.downloadedBytes < 4096)
                {
                    yield return null;
                }
            
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log($"error loading song: {www.error}");
                    yield break;
                }

                Debug.Log("loaded song");
                streamedAudioClip = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
                yield return null;
            }
        }

        /// <summary>
        /// fills a buffer with a chunk of the song file in the temporary cache
        /// note: this blocks the main thread
        /// </summary>
        /// <param name="startSample">start of wanted audio</param>
        /// <param name="numSamples">length of buffer</param>
        /// <exception cref="System.IO.FileNotFoundException">song file doesn't exist in temporary cache</exception>
        public static IEnumerator GetSongSamples(int startSample, int numSamples)
        {
            // we can't get sample data from streamed audio
            // temporarily load the entire song into memory to fill a buffer

            string url = "file://" + tmpDir + "song.bin";
            AudioClip audio = null;
            if (!File.Exists(tmpDir + "song.bin")) throw new System.IO.FileNotFoundException("path", $"Chart song file does not exist at path {tmpDir + "song.bin"}");
            
            AudioType audioType = AudioFormats.GetAudioType(tmpDir + "song.bin", out _);
            songChunk = new float[numSamples];
            songChunkLock = true;

            using (var www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = false;
                
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log($"error loading song: {www.error}");
                    songChunkLock = false;
                    songChunk = null;
                    yield break;
                }
                Debug.Log(www.result);

                Debug.Log(Time.realtimeSinceStartup);
                audio = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
            }
            Debug.Log(Time.realtimeSinceStartup);
            audio.GetData(songChunk, startSample);
            audio = null;
            songChunkLock = false;
        }

        /// <summary>
        /// writes a beatmap to the "remix.json" file in the temporary cache
        /// </summary>
        /// <param name="beatmap">RiqBeatmap to serialize</param>
        public static void WriteRiq(RiqBeatmap beatmap)
        {
            if (IsCacheLocked()) throw new System.IO.IOException($"RIQ cache is locked, cannot write RIQ file");
            if (!Directory.Exists(tmpDir))
                Directory.CreateDirectory(tmpDir);
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
            if (IsCacheLocked()) throw new System.IO.IOException($"RIQ cache is locked, cannot write audio file");
            if (songPath == string.Empty || songPath == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");
            if (!File.Exists(songPath)) throw new System.IO.FileNotFoundException("path", $"Audio file does not exist at path {songPath}");

            if (AudioConverter != null)
            {
                AudioType type = AudioFormats.GetAudioType(songPath, out string specificType);
                songPath = AudioConverter(songPath, type, specificType);
                if (!File.Exists(songPath)) throw new System.IO.FileNotFoundException("path", $"converted audio file does not exist at path {songPath}");
            }

            // check if songPath is a valid audio file
            if (AudioFormats.GetAudioType(songPath, out _) == AudioType.UNKNOWN) 
            {
                // if no user processing is defined on unknown file type, or user processing returns unknown filetype, throw exception
                throw new System.IO.InvalidDataException($"file at path {songPath} is of unknown type");
            }

            if (!Directory.Exists(tmpDir))
                Directory.CreateDirectory(tmpDir);

            string songDest = tmpDir + "song.bin";
            File.Copy(songPath, songDest, true);
        }

        /// <summary>
        /// packs the contents of the temporary cache into a .riq file
        /// </summary>
        /// <param name="destPath">where the new .riq will be saved to</param>
        public static void PackRiq(string destPath, bool backup = true)
        {
            if (tmpDir == string.Empty || tmpDir == null) throw new System.ArgumentNullException("path", "temporary directory cannot be null or empty");
            if (destPath == string.Empty || destPath == null) throw new System.ArgumentNullException("path", "destination path cannot be null or empty");

            string jsonPath = tmpDir + "remix.json";
            if (!File.Exists(jsonPath)) throw new System.IO.FileNotFoundException("path", $"riq chart file does not exist at path {jsonPath}, was an RIQ file properly created?");

            if (File.Exists(destPath))
            {
                if (backup)
                    File.Copy(destPath, destPath + ".bak", true);

                File.Delete(destPath);
            }
            ZipFile.CreateFromDirectory(tmpDir, destPath, System.IO.Compression.CompressionLevel.Optimal, false);
        }

        /// <summary>
        /// makes a backup of the current .riq file in the temporary cache
        /// and puts it in persistent data
        /// </summary>
        public async static Task BackupRiq()
        {
            string bakDir = Application.persistentDataPath + "/RIQBackup/";
            if (!Directory.Exists(bakDir))
                Directory.CreateDirectory(bakDir);
            await Task.Run(() => PackRiq(bakDir + "backup.riq", true));
        }

        /// <summary>
        /// clears the temporary cache if it exists
        /// </summary>        
        public static void ClearCache()
        {
            if (!IsCacheLocked())
            {
                if (Directory.Exists(tmpDir))
                {
                    foreach (string file in Directory.GetFiles(tmpDir, "*", SearchOption.AllDirectories))
                    {
                        if (file != tmpDir + "lock") File.Delete(file);
                    }
                }
            }
        }

        /// <summary>
        /// checks if the temporary cache has a lock set
        /// use to safeguard against multiple processes accessing the cache at once
        /// </summary>
        /// <returns>is the cache locked? or false if the cache doesn't exist</returns>
        public static bool IsCacheLocked()
        {
            if (!Directory.Exists(tmpDir)) return false;
            if (!File.Exists(tmpDir + "lock")) return false;
            string lockID = File.ReadAllText(tmpDir + "lock");
            return lockID != CacheID.ToString();
        }

        /// <summary>
        /// locks the riq cache
        /// only needs to be called on app boot
        /// don't call if locking is not necessary
        /// </summary>
        public static void LockCache()
        {
            if (CacheID != null) return;

            if (!Directory.Exists(tmpDir))
                Directory.CreateDirectory(tmpDir);
            
            CacheID = Guid.NewGuid();

            if (Directory.Exists(tmpDir) && !File.Exists(tmpDir + "lock"))
            {
                File.WriteAllText(tmpDir + "lock", CacheID.ToString());
            }
        }

        /// <summary>
        /// unlocks the riq cache
        /// only needs to be called on app exit
        /// </summary>
        public static void UnlockCache()
        {
            if (!Directory.Exists(tmpDir)) return;
            if (!File.Exists(tmpDir + "lock")) return;
            string lockID = File.ReadAllText(tmpDir + "lock");
            if (lockID == CacheID.ToString())
            {
                File.Delete(tmpDir + "lock");
            }
        }
    }
}