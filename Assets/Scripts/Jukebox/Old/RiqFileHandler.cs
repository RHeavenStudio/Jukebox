using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Jukebox
{
#if JUKEBOX_V1
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
        static string tmpDir = Path.Combine(Application.temporaryCachePath, "RIQCache");
        static string treeDir = Path.Combine(tmpDir, "Current");
        static string resDir = Path.Combine(treeDir, "Resources");
        static AudioClip streamedAudioClip;
        static UnityWebRequest streamedAudioRequest;
        static float[] songChunk;
        static bool songChunkLock = false;

        public static string RiqCachePath => treeDir;

        public static AudioClip StreamedAudioClip
        {
            get
            {
                return streamedAudioClip;
            }
        }

        public static UnityWebRequest StreamedAudioRequest
        {
            get
            {
                return streamedAudioRequest;
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
                ZipFile.ExtractToDirectory(path, treeDir, true);

                string oldSongPath = Path.Combine(treeDir, "song.ogg");
                string songPath = Path.Combine(treeDir, "song.bin");

                // if we have a legacy style song.ogg, rename it to song.bin
                if (File.Exists(oldSongPath))
                {
                    File.Move(oldSongPath, songPath);
                }

                if (File.Exists(songPath))
                {
                    FileInfo inf = new FileInfo(songPath);
                    if (inf.Length == 0) File.Delete(songPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error extracting RIQ file: {e.Message}");
                throw e;
            }

            return treeDir;
        }

        /// <summary>
        /// reads a .riq json file into a RiqBeatmap object
        /// </summary>
        /// <param name="path">directory to extracted riq JSON</param>
        /// <returns>an instance of RiqBeatmap</returns>
        public static RiqBeatmap ReadRiq()
        {
            if (treeDir == string.Empty || treeDir == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");

            string jsonPath = Path.Combine(treeDir, "remix.json");
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
        public static IEnumerator LoadSong(bool stream = true, bool disposeHandler = true)
        {
            string url = "file://" + Path.Combine(treeDir, "song.bin");
            streamedAudioClip = null;
            if (!File.Exists(Path.Combine(treeDir, "song.bin"))) throw new System.IO.FileNotFoundException($"Chart song file does not exist at path {Path.Combine(treeDir, "song.bin")}");

            FileInfo inf = new FileInfo(Path.Combine(treeDir, "song.bin"));
            if (inf.Length == 0) throw new System.IO.FileNotFoundException($"Chart song file does not exist at path {Path.Combine(treeDir, "song.bin")}");

            AudioType audioType = AudioFormats.GetAudioType(Path.Combine(treeDir, "song.bin"), out _);
            if (audioType == AudioType.UNKNOWN) throw new System.IO.InvalidDataException($"file at path {Path.Combine(treeDir, "song.bin")} is of unknown type");

            streamedAudioRequest = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            ((DownloadHandlerAudioClip)streamedAudioRequest.downloadHandler).compressed = false;
            ((DownloadHandlerAudioClip)streamedAudioRequest.downloadHandler).streamAudio = stream;
            streamedAudioRequest.SendWebRequest();
            while (!(streamedAudioRequest.result == UnityWebRequest.Result.ConnectionError) && streamedAudioRequest.downloadedBytes < 4096)
            {
                yield return null;
            }

            if (streamedAudioRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log($"error loading song: {streamedAudioRequest.error}");
                yield break;
            }

            Debug.Log($"Jukebox loaded song {url} ({streamedAudioRequest.downloadedBytes} bytes)");
            streamedAudioClip = ((DownloadHandlerAudioClip)streamedAudioRequest.downloadHandler).audioClip;
            yield return null;

            if (disposeHandler)
            {
                streamedAudioRequest.Dispose();
                streamedAudioRequest = null;
            }
        }

        /// <summary>
        /// Disposes the UnityWebRequest used to load the song file
        /// </summary>
        public static void DisposeAudioWebRequest()
        {
            if (streamedAudioRequest != null)
            {
                streamedAudioRequest.Dispose();
                streamedAudioRequest = null;
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

            string url = "file://" + Path.Combine(treeDir, "song.bin");
            AudioClip audio = null;
            if (!File.Exists(Path.Combine(treeDir, "song.bin"))) throw new System.IO.FileNotFoundException("path", $"Chart song file does not exist at path {Path.Combine(treeDir, "song.bin")}");

            AudioType audioType = AudioFormats.GetAudioType(Path.Combine(treeDir, "song.bin"), out _);
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
        /// gets the file path of a resource in the temporary cache
        /// the first match will be returned
        /// search can be further narrowed by specifying a subdirectory
        /// the end app is expected to do its own processing on the returned path
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static string GetResourcePath(string resourceName, string subDir = "")
        {
            if (resourceName == string.Empty || resourceName == null) throw new System.ArgumentNullException("path", "resource name cannot be null or empty");
            if (!Directory.Exists(Path.Combine(resDir, subDir))) throw new System.IO.DirectoryNotFoundException($"RIQ resource directory does not exist at path {Path.Combine(resDir, subDir)}");
            // find files with the same name
            foreach (string file in Directory.GetFiles(Path.Combine(resDir, subDir), resourceName + ".*", SearchOption.AllDirectories))
            {
                return file;
            }
            return null;
        }

        /// <summary>
        /// writes a beatmap to the "remix.json" file in the temporary cache
        /// </summary>
        /// <param name="beatmap">RiqBeatmap to serialize</param>
        public static void WriteRiq(RiqBeatmap beatmap)
        {
            if (IsCacheLocked()) throw new System.IO.IOException($"RIQ cache is locked, cannot write RIQ file");
            if (!Directory.Exists(treeDir))
                Directory.CreateDirectory(treeDir);
            string jsonPath = Path.Combine(treeDir, "remix.json");
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

            if (!Directory.Exists(treeDir))
                Directory.CreateDirectory(treeDir);

            string songDest = Path.Combine(treeDir, "song.bin");
            File.Copy(songPath, songDest, true);
        }

        /// <summary>
        /// adds / replaces a resource to the riq
        /// copies the resource to the temporary cache and will be included to the .riq file when packed
        /// </summary>
        /// <param name="resourcePath">path of the original resource</param>
        /// <param name="resourceName">new name of the resource</param>
        /// <param name="subDir">subdirectory in the resources</param>
        /// <param name="ignoreExtension">should the extension of the resource be ignored when replacing resources?</param>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public static void AddResource(string resourcePath, string resourceName, string subDir = "", bool ignoreExtension = true)
        {
            if (IsCacheLocked()) throw new System.IO.IOException($"RIQ cache is locked, cannot write resource file");
            if (resourcePath == string.Empty || resourcePath == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");
            if (resourceName == string.Empty || resourcePath == null) throw new System.ArgumentNullException("name", "name of resource cannot be null or empty");
            if (!File.Exists(resourcePath)) throw new System.IO.FileNotFoundException("path", $"Resource file does not exist at path {resourcePath}");

            if (!Directory.Exists(resDir))
                Directory.CreateDirectory(resDir);

            if (subDir != string.Empty && subDir != null)
            {
                if (!Directory.Exists(Path.Combine(resDir, subDir)))
                    Directory.CreateDirectory(Path.Combine(resDir, subDir));
            }

            string extension = Path.GetExtension(resourcePath);
            if (ignoreExtension)
            {
                // find any file at the target path with the same name
                foreach (string file in Directory.GetFiles(Path.Combine(resDir, subDir), resourceName + ".*", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(file);
                }
            }
            string destPath = Path.Combine(resDir, subDir, resourceName + extension);
            File.Copy(resourcePath, destPath, true);
        }

        /// <summary>
        /// removes a resource from the riq
        /// all resources with the same name will be removed
        /// </summary>
        /// <param name="resourceName">name of the resource(s) to be removed</param>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void RemoveResource(string resourceName, string subDir = "")
        {
            if (IsCacheLocked()) throw new System.IO.IOException($"RIQ cache is locked, cannot write resource file");
            if (resourceName == string.Empty || resourceName == null) throw new System.ArgumentNullException("path", "path cannot be null or empty");
            if (!Directory.Exists(resDir)) throw new System.IO.DirectoryNotFoundException($"RIQ resource directory does not exist at path {resDir}");
            // find files with the same name
            foreach (string file in Directory.GetFiles(Path.Combine(resDir, subDir), resourceName + ".*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
        }

        /// <summary>
        /// packs the contents of the temporary cache into a .riq file
        /// </summary>
        /// <param name="destPath">where the new .riq will be saved to</param>
        public static void PackRiq(string destPath, bool backup = true)
        {
            if (treeDir == string.Empty || treeDir == null) throw new System.ArgumentNullException("path", "temporary directory cannot be null or empty");
            if (destPath == string.Empty || destPath == null) throw new System.ArgumentNullException("path", "destination path cannot be null or empty");

            string jsonPath = Path.Combine(treeDir, "remix.json");
            if (!File.Exists(jsonPath)) throw new System.IO.FileNotFoundException("path", $"riq chart file does not exist at path {jsonPath}, was an RIQ file properly created?");

            if (File.Exists(destPath))
            {
                if (backup)
                    File.Copy(destPath, destPath + ".bak", true);

                File.Delete(destPath);
            }
            ZipFile.CreateFromDirectory(treeDir, destPath, System.IO.Compression.CompressionLevel.Optimal, false);
        }

        /// <summary>
        /// makes a backup of the current .riq file in the temporary cache
        /// and puts it in persistent data
        /// </summary>
        public async static Task BackupRiq()
        {
            string bakDir = Path.Combine(Application.persistentDataPath, "RIQBackup");
            if (!Directory.Exists(bakDir))
                Directory.CreateDirectory(bakDir);
            await Task.Run(() => PackRiq(Path.Combine(bakDir, "backup.riq"), true));
        }

        /// <summary>
        /// clears the temporary cache if it exists
        /// </summary>        
        public static void ClearCache()
        {
            if (!IsCacheLocked())
            {
                if (Directory.Exists(treeDir))
                {
                    Directory.Delete(treeDir, true);
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
            if (!File.Exists(Path.Combine(tmpDir, "lock"))) return false;
            string lockID = File.ReadAllText(Path.Combine(tmpDir, "lock"));
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

            if (Directory.Exists(tmpDir) && !File.Exists(Path.Combine(tmpDir, "lock")))
            {
                File.WriteAllText(Path.Combine(tmpDir, "lock"), CacheID.ToString());
            }
        }

        /// <summary>
        /// unlocks the riq cache
        /// only needs to be called on app exit
        /// </summary>
        public static void UnlockCache()
        {
            if (!Directory.Exists(tmpDir)) return;
            if (!File.Exists(Path.Combine(tmpDir, "lock"))) return;
            string lockID = File.ReadAllText(Path.Combine(tmpDir, "lock"));
            if (lockID == CacheID.ToString())
            {
                File.Delete(Path.Combine(tmpDir, "lock"));
            }
        }
    }
#endif
}
