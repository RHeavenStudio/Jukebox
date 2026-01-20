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
        const int VERSION = 201;

        static Guid? CacheID = null;
        static readonly string tempDir = Path.Combine(Application.temporaryCachePath, "RIQCache");
        static readonly string treeDir = Path.Combine(tempDir, "Current");
        static readonly string resourcesDir = Path.Combine(treeDir, "Resources");
        static readonly string audioDir = Path.Combine(treeDir, "Music");
        static readonly string chartDir = Path.Combine(treeDir, "Charts");
        static readonly string metadataDir = Path.Combine(treeDir, ".meta");


        static RiqBeatmap lastReadBeatmap;
        static AudioClip currentAudioClip;
        static UnityWebRequest audioReadRequest;

        public static string CachePath => treeDir;
        public static string AudioPath => audioDir;
        public static string ChartPath => chartDir;
        public static string MetadataPath => metadataDir;
        public static string ResourcesPath => resourcesDir;

        static RiqMetadata currentMetadata;

        #region File Handler Hints
        public static AudioClipLoadType AudioLoadTypeHint = AudioClipLoadType.Streaming;
        #endregion

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
        /// Reads a chart from the cache, and the chart's corresponding song.
        /// Use <see cref="Extract"/> first to extract the contents of a RIQ file.
        /// Use <see cref="GetLoadedChart"/> to get the loaded beatmap.
        /// Use <see cref="GetLoadedSong"/> to get the loaded song.
        /// </summary>
        /// <param name="index">Chart index to read</param>
        public static IEnumerator ReadChartAndAudio(int index)
        {
            yield return ReadChart_Coroutine(index);

            IEnumerator readAudio = ReadAudio(lastReadBeatmap.SongName);
            while (true)
            {
                object current = readAudio.Current;
                try
                {
                    if (readAudio.MoveNext() == false)
                    {
                        break;
                    }
                    current = readAudio.Current;
                }
                catch (FileNotFoundException f)
                {
                    Debug.LogWarning($"Chart has no music: {f.Message} {f.StackTrace}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load music: {e.Message}");
                    yield break;
                }
                yield return current;
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

            lastReadBeatmap = JsonConvert.DeserializeObject<RiqBeatmap>(chartJson);
            return lastReadBeatmap;
        }

        /// <summary>
        /// Coroutine
        /// Reads a chart from the cache.
        /// Use <see cref="Extract"/> first to extract the contents of a RIQ file. 
        /// Use <see cref="GetLoadedChart"/> to get the loaded beatmap.
        /// </summary>
        /// <param name="index">Chart index to read</param>
        /// <returns>A <see cref="RiqBeatmap"/> object</returns>
        /// <exception cref="FileNotFoundException">Chart file not found</exception>
        public static IEnumerator ReadChart_Coroutine(int index)
        {
            string chartName = $"chart{index}";
            string chartPath = Path.Combine(chartDir, chartName + ".json");
            if (!File.Exists(chartPath))
            {
                throw new FileNotFoundException($"Chart {index} not found in RIQ file");
            }

            Task<string> readTask = File.ReadAllTextAsync(chartPath);
            WaitUntil waitUntil = new(() => readTask.IsCompleted);
            yield return waitUntil;
            if (readTask.Status == TaskStatus.Faulted)
            {
                throw readTask.Exception;
            }
            string chartJson = readTask.Result;
            Debug.Log($"Jukebox loaded chart {chartPath} ({chartJson.Length} bytes)");
            lastReadBeatmap = JsonConvert.DeserializeObject<RiqBeatmap>(chartJson);
        }

        /// <summary>
        /// Coroutine
        /// Uses <see cref="UnityWebRequestMultimedia"/> to load an audio clip from the cache.
        /// Use <see cref="Extract"/> first to extract the contents of a RIQ file. 
        /// Use <see cref="GetLoadedSong"/> to get the loaded song.
        /// </summary>
        /// <param name="index">Chart index to get the audio clip for</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">Audio file not found</exception>
        /// <exception cref="InvalidDataException">Unknown audio format</exception>
        /// <exception cref="Exception">Error loading audio</exception>
        [Obsolete("Use ReadAudio(string songPath) instead for more control regarding sharing song files between charts.")]
        public static IEnumerator ReadAudio(int index)
        {
            yield return ReadAudio($"song{index}");
        }

        /// <summary>
        /// Coroutine
        /// Uses <see cref="UnityWebRequestMultimedia"/> to load an audio clip from the cache.
        /// Use <see cref="Extract"/> first to extract the contents of a RIQ file. 
        /// Use <see cref="GetLoadedSong"/> to get the loaded song.
        /// </summary>
        /// <param name="songName">Name of the song file</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">Audio file not found</exception>
        /// <exception cref="InvalidDataException">Unknown audio format</exception>
        /// <exception cref="Exception">Error loading audio</exception>
        public static IEnumerator ReadAudio(string songName)
        {
            currentAudioClip = null;
            if (string.IsNullOrEmpty(songName) || songName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new FileNotFoundException($"{songName} is an invalid song path");
            }

            string[] files = Directory.GetFiles(audioDir, songName + ".*");
            if (files.Length == 0)
            {
                throw new FileNotFoundException($"Song {songName} not found in RIQ file");
            }
            else if (files.Length > 1)
            {
                Debug.LogWarning($"Multiple songs with name {songName} found, first found entry will be used.");
            }

            AudioType audioType = AudioFormats.GetAudioType(files[0], out _);
            if (audioType == AudioType.UNKNOWN)
            {
                throw new InvalidDataException($"Unknown audio format for song {songName} (at {files[0]})");
            }

            string uri = "file://" + files[0];
            audioReadRequest = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            bool useStreamingPath = AudioLoadTypeHint == AudioClipLoadType.Streaming;
            switch (AudioLoadTypeHint)
            {
                case AudioClipLoadType.DecompressOnLoad:
                    ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).compressed = false;
                    ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).streamAudio = false;
                    break;
                case AudioClipLoadType.CompressedInMemory:
                    ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).compressed = false;
                    ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).streamAudio = false;
                    break;
                default:
                    ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).compressed = false;
                    ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).streamAudio = true;
                    break;
            }

            if (useStreamingPath)
            {
                // don't download entire audio file when streaming
                audioReadRequest.SendWebRequest();
                while (!(audioReadRequest.result == UnityWebRequest.Result.ConnectionError) && audioReadRequest.downloadedBytes < 4096)
                {
                    yield return null;
                }
            }
            else
            {
                yield return audioReadRequest.SendWebRequest();
            }
            if (audioReadRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                throw new Exception($"Error loading song {uri}: {audioReadRequest.error}");
            }


            if (useStreamingPath)
            {
                Debug.Log($"Jukebox streaming song {uri} ({audioReadRequest.downloadedBytes} bytes)");
                currentAudioClip = ((DownloadHandlerAudioClip)audioReadRequest.downloadHandler).audioClip;
            }
            else
            {
                Debug.Log($"Jukebox downloaded song {uri} ({audioReadRequest.downloadedBytes} bytes)");
                currentAudioClip = DownloadHandlerAudioClip.GetContent(audioReadRequest);
            }

            audioReadRequest.Dispose();
            audioReadRequest = null;
        }

        /// <summary>
        /// Gets the beatmap that was loaded by <see cref="ReadChartAndAudio"/>.
        /// </summary>
        /// <returns>Loaded beatmap</returns>
        public static RiqBeatmap GetLoadedChart()
        {
            return lastReadBeatmap;
        }

        /// <summary>
        /// Gets the audio clip that was loaded by <see cref="ReadAudio"/> or <see cref="ReadChartAndAudio"/>.
        /// </summary>
        /// <returns>Loaded audio clip</returns>
        public static AudioClip GetLoadedSong()
        {
            return currentAudioClip;
        }

        /// <summary>
        /// Creates a file path to a chart's song.
        /// </summary>
        /// <param name="chart">The chart to get the audio path for</param>
        /// <returns>Possible path to the audio file for the chart. Returns the first found file if multiple are found.</returns>
        /// <exception cref="ArgumentNullException">Provided chart is null</exception>
        /// <exception cref="FileNotFoundException">No audio file found for the chart</exception>
        public static string GetAudioPathForChart(RiqBeatmap chart)
        {
            if (chart is null) throw new ArgumentNullException("chart", "chart cannot be null");
            string songName = chart.SongName;
            if (string.IsNullOrEmpty(songName)) throw new FileNotFoundException("chart.SongName", "chart.SongName cannot be null or empty");

            string[] files = Directory.GetFiles(audioDir, songName + ".*");
            if (files.Length == 0)
            {
                throw new FileNotFoundException($"Song {songName} not found in RIQ file");
            }
            else if (files.Length > 1)
            {
                Debug.LogWarning($"Multiple songs with name {songName} found, first found entry will be used.");
            }

            return files[0];
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
        /// <param name="chart">Chart to copy an audio file for</param>
        /// <param name="audioPath">Path to the audio file</param>
        /// <exception cref="ArgumentNullException">path to the audio file is null or empty</exception>
        /// <exception cref="FileNotFoundException">audio file does not exist at provided path</exception>
        /// <exception cref="IOException">RIQ cache is locked</exception>
        /// <exception cref="System.IO.InvalidDataException">audio file is of unknown type</exception>
        public static string WriteAudio(RiqBeatmap chart, string audioPath, bool setSongName = true)
        {
            if (audioPath == string.Empty || audioPath == null) throw new ArgumentNullException("audioPath", "audioPath cannot be null or empty");
            if (!File.Exists(audioPath)) throw new FileNotFoundException("audioPath", $"audio file does not exist at path {audioPath}");
            if (IsCacheLocked()) throw new IOException("RIQ cache is locked, cannot write audio");
            // check if songPath is a valid audio file
            if (AudioFormats.GetAudioType(audioPath, out _) == AudioType.UNKNOWN)
            {
                // if no user processing is defined on unknown file type, or user processing returns unknown filetype, throw exception
                throw new System.IO.InvalidDataException($"file at path {audioPath} is of unknown type");
            }

            if (!Directory.Exists(audioDir))
                Directory.CreateDirectory(audioDir);

            DeleteOldAudio(chart);
            string songName = Path.GetFileNameWithoutExtension(audioPath);
            if (setSongName)
            {
                chart.WithSongName(songName);
            }

            string songPath = Path.Combine(audioDir, $"{songName}{Path.GetExtension(audioPath)}");
            File.Copy(audioPath, songPath, true);
            return songName;
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
        [Obsolete("Use WriteAudio and pass an RiqBeatmap instead of a song index")]
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

        static void DeleteOldAudio(RiqBeatmap chart)
        {
            string songName = chart.SongName;
            if (string.IsNullOrEmpty(songName)) return;

            string[] files = Directory.GetFiles(audioDir, songName + ".*");
            if (files.Length != 0)
            {
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
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
                Debug.Log($"Converting metadata key {kvp.Key}, value {kvp.Value}");
                metadata.CreateEntry(kvp.Key, kvp.Value);
                Debug.Log($"Added entry {metadata[kvp.Key]} as {kvp.Key}");
            }

            RiqBeatmap beatmap = new(VERSION);
            beatmap.WithOffset(oldBeatmap.data.offset).WithSongName("song");
            foreach (OldRiqEntity oldEntity in oldBeatmap.data.entities)
            {
                RiqEntity entity = ConvertOldEntity(oldEntity);
                beatmap.CreateEntity(entity);
            }
            foreach (OldRiqEntity oldTempoChange in oldBeatmap.data.tempoChanges)
            {
                RiqEntity entity = ConvertOldEntity(oldTempoChange);
                beatmap.CreateEntity(entity);
            }
            foreach (OldRiqEntity oldVolumeChange in oldBeatmap.data.volumeChanges)
            {
                RiqEntity entity = ConvertOldEntity(oldVolumeChange);
                beatmap.CreateEntity(entity);
            }
            foreach (OldRiqEntity oldSectionMarker in oldBeatmap.data.beatmapSections)
            {
                RiqEntity entity = ConvertOldEntity(oldSectionMarker);
                beatmap.CreateEntity(entity);
            }

            WriteMetadata(metadata);
            WriteChart(0, beatmap);
            if (File.Exists(oldSongPath))
            {
                // this keeps the .bin extension but the audio reader will still try to determine type based on content
                WriteAudio(beatmap, oldSongPath);
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
