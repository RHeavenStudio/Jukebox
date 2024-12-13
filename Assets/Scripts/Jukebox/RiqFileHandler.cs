using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

#if JUKEBOX_V1
using Jukebox.Legacy;
#endif

namespace Jukebox
{
    public static class RiqFileHandler
    {
        const int VERSION = 2;

        static Guid? CacheID = null;
        static readonly string tempDir = Path.Combine(Application.temporaryCachePath, "RIQCache");
        static readonly string treeDir = Path.Combine(tempDir, "Current");
        static readonly string resourcesDir = Path.Combine(treeDir, "Resources");
        static readonly string audioDir = Path.Combine(treeDir, "Music");
        static readonly string chartDir = Path.Combine(treeDir, "Charts");
        static readonly string metadataDir = Path.Combine(treeDir, ".meta");


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
        /// <exception cref="ArgumentNullException">Path is null or empty</exception>
        /// <exception cref="FileNotFoundException">RIQ file does not exist at provided path</exception>
        /// <exception cref="IOException">RIQ cache is locked</exception>
        public static string Extract(string path)
        {
            if (path == string.Empty || path == null) throw new ArgumentNullException("path", "path cannot be null or empty");
            if (!File.Exists(path)) throw new FileNotFoundException("path", $"RIQ file does not exist at path {path}");
            if (IsCacheLocked()) throw new IOException($"RIQ cache is locked, cannot extract RIQ file at path {path}");

            try
            {
                ClearCache();
                ZipFile.ExtractToDirectory(path, treeDir, true);
                currentMetadata = null;
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
        /// <exception cref="Exception">Error reading the metadata.</exception>
        public static int CheckVersion()
        {
            if (!File.Exists(metadataDir))
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
        /// <exception cref="Exception">Error reading the metadata.</exception>
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

        /// <summary>
        /// Reads a chart from the cache.
        /// Use <see cref="Extract"/> first to extract the contents of a RIQ file. 
        /// </summary>
        /// <param name="index">Chart index to read</param>
        /// <returns>A <see cref="RiqBeatmap"/> object</returns>
        /// <exception cref="FileNotFoundException">Chart file not found</exception>
        public static RiqBeatmap ReadChart(int index)
        {
            string chartName = $"chart{index}";
            string chartPath = Path.Combine(chartDir, chartName + ".json");
            if (!File.Exists(chartPath))
            {
                throw new FileNotFoundException($"Chart {index} not found in RIQ file");
            }

            string chartJson = File.ReadAllText(chartPath);
            Debug.Log($"Jukebox loaded chart {chartPath} ({chartJson.Length} bytes)");

            return JsonConvert.DeserializeObject<RiqBeatmap>(chartJson);
        }

        /// <summary>
        /// Coroutine
        /// Uses <see cref="UnityWebRequestMultimedia"/> to load an audio clip from the cache.
        /// Use <see cref="Extract"/> first to extract the contents of a RIQ file. 
        /// </summary>
        /// <param name="index">Chart index to get the audio clip for</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">Audio file not found</exception>
        /// <exception cref="InvalidDataException">Unknown audio format</exception>
        /// <exception cref="Exception">Error loading audio</exception>
        public static IEnumerator ReadAudio(int index)
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

        /// <summary>
        /// Gets the audio clip that was loaded by <see cref="ReadAudio"/>.
        /// </summary>
        /// <returns>Loaded audio clip</returns>
        public static AudioClip GetLoadedSong()
        {
            return streamedAudioClip;
        }
        #endregion

        #region Write
        /// <summary>
        /// Writes chart metadata <see cref="RiqMetadata"/> to the cache.
        /// </summary>
        /// <param name="metadata"><see cref="RiqMetadata"/> to write</param>
        /// <exception cref="ArgumentNullException">Provided metadata is null</exception>
        /// <exception cref="IOException">RIQ cache is locked</exception>
        public static void WriteMetadata(RiqMetadata metadata)
        {
            if (metadata is null) throw new ArgumentNullException("metadata", "metadata cannot be null");
            if (IsCacheLocked()) throw new IOException("RIQ cache is locked, cannot write metadata");

            if (!Directory.Exists(treeDir))
                Directory.CreateDirectory(treeDir);

            string meta = JsonConvert.SerializeObject(metadata);
            File.WriteAllText(metadataDir, meta);
        }

        /// <summary>
        /// Writes a chart <see cref="RiqBeatmap"/> to the cache.
        /// </summary>
        /// <param name="index">Index of the chart to write for</param>
        /// <param name="chart"><see cref="RiqBeatmap"/> to write</param>
        /// <exception cref="ArgumentNullException">Provided chart is null</exception>
        /// <exception cref="IOException">RIQ cache is locked</exception>
        /// <exception cref="InvalidOperationException">Chart index is invalid</exception>
        public static void WriteChart(int index, RiqBeatmap chart)
        {
            if (chart is null) throw new ArgumentNullException("chart", "chart cannot be null");
            if (IsCacheLocked()) throw new IOException("RIQ cache is locked, cannot write chart");
            if (index < 0) throw new InvalidOperationException("chart index must be greater than 0");

            if (!Directory.Exists(chartDir))
                Directory.CreateDirectory(chartDir);

            string chartName = $"chart{index}";
            string chartPath = Path.Combine(chartDir, chartName + ".json");
            string chartJson = chart.Serialize();
            File.WriteAllText(chartPath, chartJson);
        }

        /// <summary>
        /// Copies an audio file to the cache.
        /// </summary>
        /// <param name="index">Index of the chart to copy an audio file for</param>
        /// <param name="audioPath">Path to the audio file</param>
        /// <exception cref="ArgumentNullException">path to the audio file is null or empty</exception>
        /// <exception cref="FileNotFoundException">audio file does not exist at provided path</exception>
        /// <exception cref="IOException">RIQ cache is locked</exception>
        /// <exception cref="InvalidOperationException">chart index is invalid</exception>
        /// <exception cref="System.IO.InvalidDataException">audio file is of unknown type</exception>
        public static void WriteAudio(int index, string audioPath)
        {
            if (audioPath == string.Empty || audioPath == null) throw new ArgumentNullException("audioPath", "audioPath cannot be null or empty");
            if (!File.Exists(audioPath)) throw new FileNotFoundException("audioPath", $"audio file does not exist at path {audioPath}");
            if (IsCacheLocked()) throw new IOException("RIQ cache is locked, cannot write audio");
            if (index < 0) throw new InvalidOperationException("audio index must be greater than 0");
            // check if songPath is a valid audio file
            if (AudioFormats.GetAudioType(audioPath, out _) == AudioType.UNKNOWN)
            {
                // if no user processing is defined on unknown file type, or user processing returns unknown filetype, throw exception
                throw new System.IO.InvalidDataException($"file at path {audioPath} is of unknown type");
            }

            if (!Directory.Exists(audioDir))
                Directory.CreateDirectory(audioDir);

            DeleteOldAudio(index);

            string songPath = Path.Combine(audioDir, $"song{index}{Path.GetExtension(audioPath)}");
            File.Copy(audioPath, songPath, true);
        }

        static void DeleteOldAudio(int index)
        {
            string songName = $"song{index}";
            string[] files = Directory.GetFiles(audioDir, songName + ".*");
            if (files.Length != 0)
            {
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// Packs the contents of the temporary cache into a .riq file.
        /// </summary>
        /// <param name="destPath">Path to save the .riq file</param>
        /// <param name="backup">If destination file exists, make a backup</param>
        public static void Pack(string destPath, bool backup = true)
        {
#if JUKEBOX_V1
            // if not already done, delete old json and song files, they should be in their new locations by now
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
        /// <returns>is the cache locked? always false if the cache doesn't exist</returns>
        public static bool IsCacheLocked()
        {
            if (!Directory.Exists(tempDir)) return false;
            if (!File.Exists(Path.Combine(tempDir, "lock"))) return false;
            string lockID = File.ReadAllText(Path.Combine(tempDir, "lock"));
            return lockID != CacheID.ToString();
        }

        /// <summary>
        /// locks the riq cache
        /// only needs to be called once on startup
        /// do not call if locking is not necessary
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
        /// only needs to be called once on shutdown
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

        #region Old File Conversion
#if JUKEBOX_V1
        /// <summary>
        /// Upgrades the cache structure of a v1 RIQ to the new structure
        /// Assumes a v1 RIQ was extracted to the temporary cache.
        /// </summary>
        public static void UpgradeOldStructure()
        {
            if (!Directory.Exists(treeDir)) return;
            if (CheckVersion() >= 2) return;

            string oldBeatmapPath = Path.Combine(treeDir, "remix.json");
            string oldSongPath = Path.Combine(treeDir, "song.bin");

            if (!File.Exists(oldBeatmapPath)) throw new FileNotFoundException("old style remix.json not found in RIQ file (may not be a v1 riq?)");

            OldRiqBeatmap oldBeatmap = OldRiqFileHandler.ReadRiq();

            RiqMetadata metadata = new(VERSION, oldBeatmap.data.riqOrigin);
            foreach (KeyValuePair<string, object> kvp in oldBeatmap.data.properties)
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(kvp.Key);
                metadata.CreateEntry(key.StringValue, kvp.Value);
            }

            RiqBeatmap beatmap = new(VERSION);
            beatmap.WithOffset(oldBeatmap.data.offset);
            foreach (OldRiqEntity oldEntity in oldBeatmap.data.entities)
            {
                RiqEntity entity = ConvertOldEntity(oldEntity);
                beatmap.AddEntity(entity);
            }
            foreach (OldRiqEntity oldTempoChange in oldBeatmap.data.tempoChanges)
            {
                RiqEntity entity = ConvertOldEntity(oldTempoChange);
                beatmap.AddEntity(entity);
            }
            foreach (OldRiqEntity oldVolumeChange in oldBeatmap.data.volumeChanges)
            {
                RiqEntity entity = ConvertOldEntity(oldVolumeChange);
                beatmap.AddEntity(entity);
            }
            foreach (OldRiqEntity oldSectionMarker in oldBeatmap.data.beatmapSections)
            {
                RiqEntity entity = ConvertOldEntity(oldSectionMarker);
                beatmap.AddEntity(entity);
            }

            WriteMetadata(metadata);
            WriteChart(0, beatmap);
            if (File.Exists(oldSongPath))
            {
                // this keeps the .bin extension but the audio reader will still try to determine type based on content
                WriteAudio(0, oldSongPath);
                File.Delete(oldSongPath);
            }

            File.Delete(oldBeatmapPath);
        }

        static RiqEntity ConvertOldEntity(OldRiqEntity oldEntity)
        {
            RiqEntity entity = new(oldEntity.data.type, oldEntity.datamodel, oldEntity.version);

            //manually add beat and length
            entity.CreateProperty("beat", oldEntity.beat);
            entity.CreateProperty("length", oldEntity.length);

            foreach (KeyValuePair<string, object> kvp in oldEntity.data.dynamicData)
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(kvp.Key);
                entity.Keys.Add(key);
                entity.DynamicData.Add(key.Hash, kvp.Value);
            }

            return entity;
        }
#endif
        #endregion
    }
}
