using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Jukebox
{
    /*
        New file structure:
        / Current
            / Resources
                // Holds implementation-specific resources that may-or-may-not be read by the user
                // user is expected to use this manually
                / Images
                / Sounds
                / Assetbundles
                . . .
            / Audio
                // Holds per-chart BGM streams
                song0.wav
                song1.mp3
                song2.ogg
                . . .
            / Charts
                // holds RIQ chart information
                chart0.json
                chart1.json
                chart2.json
            .meta
                // json format metadata
    */

    public static class RiqFileHandler2
    {
        const int VERSION = 2;

        static Guid? CacheID = null;
        static string tempDir = Path.Combine(Application.temporaryCachePath, "RIQCache");
        static string treeDir = Path.Combine(tempDir, "Current");
        static string resourcesDir = Path.Combine(treeDir, "Resources");
        static string audioDir = Path.Combine(treeDir, "Audio");
        static string chartDir = Path.Combine(treeDir, "Charts");
        static string metadataDir = Path.Combine(treeDir, ".meta");


        static AudioClip streamedAudioClip;
        static UnityWebRequest streamedAudioRequest;

        public static string CachePath => treeDir;

        static RiqMetadata currentMetadata;

        #region Read
        /// <summary>
        /// Extracts the contents of a RIQ file into a temporary directory.
        /// For Unity Engine games, this uses the normal temporaryCachePath.
        /// </summary>
        /// <param name="path">path to the .riq file</param>
        /// <returns>path to the extracted riq contents</returns>
        public static string Extract(string path)
        {
            if (path == string.Empty || path == null) throw new ArgumentNullException("path", "path cannot be null or empty");
            if (!File.Exists(path)) throw new FileNotFoundException("path", $"RIQ file does not exist at path {path}");
            if (IsCacheLocked()) throw new IOException($"RIQ cache is locked, cannot extract RIQ file at path {path}");

            try
            {
                ClearCache();
                ZipFile.ExtractToDirectory(path, treeDir, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error extracting RIQ file: {e.Message}");
                throw e;
            }

            return treeDir;
        }

        [Obsolete("Use Extract instead.")]
        public static string ExtractRiq(string path)
        {
            return Extract(path);
        }

        /// <summary>
        /// Checks the version of the RIQ file.
        /// </summary>
        /// <returns>Version of the RIQ file.</returns>
        /// <exception cref="InvalidOperationException">RIQ file is invalid or is an unsupported legacy format.</exception>
        public static int CheckVersion()
        {
            if (!Directory.Exists(metadataDir))
            {
#if JUKEBOX_V1
                string oldJsonPath = Path.Combine(treeDir, "remix.json");
                if (File.Exists(oldJsonPath))
                {
                    // represents both v1 riq and older .tengoku / .rhmania files
                    // latter case is handled by the v1 importer
                    return 1;
                }
                else
                {
                    throw new InvalidOperationException("RIQ file is not valid.");
                }
#else
                throw new InvalidOperationException("RIQ file is not valid.");
#endif
            }

            try
            {
                if (currentMetadata is null)
                {
                    ReadMetadata();
                }
                return currentMetadata.Version;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading RIQ metadata: {e.Message}");
                throw e;
            }
        }

        /// <summary>
        /// Parses the RIQ file's metadata.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">RIQ file is invalid.</exception>
        public static RiqMetadata ReadMetadata()
        {
            if (!File.Exists(metadataDir))
            {
                throw new InvalidOperationException("RIQ file is not valid. Missing metadata. (Could be an older version?)");
            }

            string meta = File.ReadAllText(metadataDir);
            try
            {
                currentMetadata = JsonConvert.DeserializeObject<RiqMetadata>(meta);
                return currentMetadata;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading RIQ metadata: {e.Message}");
                throw e;
            }
        }

        static RiqBeatmap2 LoadChart(int index)
        {
            string chartName = $"chart{index}";
            string chartPath = Path.Combine(chartDir, chartName + ".json");
            if (!File.Exists(chartPath))
            {
                throw new FileNotFoundException($"Chart {index} not found in RIQ file");
            }

            string chartJson = File.ReadAllText(chartPath);
            Debug.Log($"Jukebox loaded chart {chartPath} ({chartJson.Length} bytes)");

            return JsonConvert.DeserializeObject<RiqBeatmap2>(chartJson);
        }

        static IEnumerator LoadSong(int index)
        {
            string songName = $"song{index}";
            streamedAudioClip = null;
            string[] files = Directory.GetFiles(audioDir, songName + ".*");
            if (files.Length == 0)
            {
                throw new FileNotFoundException($"Song {index} not found in RIQ file");
            }

            AudioType audioType = AudioFormats.GetAudioType(files[0], out _);
            if (audioType == AudioType.UNKNOWN)
            {
                throw new InvalidDataException($"Unknown audio format for song {index} (at {files[0]})");
            }

            string uri = "file://" + files[0];
            streamedAudioRequest = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            ((DownloadHandlerAudioClip)streamedAudioRequest.downloadHandler).compressed = false;
            ((DownloadHandlerAudioClip)streamedAudioRequest.downloadHandler).streamAudio = true;

            streamedAudioRequest.SendWebRequest();
            while (!(streamedAudioRequest.result == UnityWebRequest.Result.ConnectionError) && streamedAudioRequest.downloadedBytes < 4096)
            {
                yield return null;
            }

            if (streamedAudioRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                throw new Exception($"Error loading song {uri}: {streamedAudioRequest.error}");
            }

            Debug.Log($"Jukebox loaded song {uri} ({streamedAudioRequest.downloadedBytes} bytes)");
            streamedAudioClip = ((DownloadHandlerAudioClip)streamedAudioRequest.downloadHandler).audioClip;

            streamedAudioRequest.Dispose();
            streamedAudioRequest = null;
        }
        #endregion

        #region Write
        public static void Pack(string destPath, bool backup = true)
        {
#if JUKEBOX_V1
            // delete old json and song files, they should be in their new locations by now
            string oldJsonPath = Path.Combine(treeDir, "remix.json");
            if (File.Exists(oldJsonPath))
            {
                File.Delete(oldJsonPath);
            }
            string oldSongPath = Path.Combine(treeDir, "song.bin");
            if (File.Exists(oldSongPath))
            {
                File.Delete(oldSongPath);
            }
#endif

            if (File.Exists(destPath))
            {
                if (backup)
                    File.Copy(destPath, destPath + ".bak", true);

                File.Delete(destPath);
            }
            ZipFile.CreateFromDirectory(treeDir, destPath, System.IO.Compression.CompressionLevel.Optimal, false);
        }

        [Obsolete("Use Pack instead.")]
        public static void PackRiq(string destPath, bool backup = true)
        {
            Pack(destPath, backup);
        }

        /// <summary>
        /// makes a backup of the current .riq file in the temporary cache
        /// and puts it in persistent data
        /// </summary>
        public async static Task BackupRiq()
        {
            string backupDir = Path.Combine(Application.persistentDataPath, "RIQBackup");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);
            await Task.Run(() => Pack(Path.Combine(backupDir, "backup.riq"), true));
        }
        #endregion

        #region Cache
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
            if (!Directory.Exists(tempDir)) return false;
            if (!File.Exists(Path.Combine(tempDir, "lock"))) return false;
            string lockID = File.ReadAllText(Path.Combine(tempDir, "lock"));
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

            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            CacheID = Guid.NewGuid();

            if (Directory.Exists(tempDir) && !File.Exists(Path.Combine(tempDir, "lock")))
            {
                File.WriteAllText(Path.Combine(tempDir, "lock"), CacheID.ToString());
            }
        }

        /// <summary>
        /// unlocks the riq cache
        /// only needs to be called on app exit
        /// </summary>
        public static void UnlockCache()
        {
            if (!Directory.Exists(tempDir)) return;
            if (!File.Exists(Path.Combine(tempDir, "lock"))) return;
            string lockID = File.ReadAllText(Path.Combine(tempDir, "lock"));
            if (lockID == CacheID.ToString())
            {
                File.Delete(Path.Combine(tempDir, "lock"));
            }
        }
        #endregion
    }
}
