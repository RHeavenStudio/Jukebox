using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using SFB;

#if JUKEBOX_V1
using Jukebox.Legacy;
#endif

namespace Jukebox.Tests
{
    public class TestsManager : MonoBehaviour
    {
        [SerializeField] TMP_Text statusTxt;

        [SerializeField] Slider musicSlider;
        [SerializeField] TMP_Text songProgressFormatted;
        [SerializeField] TMP_Text songProgressSeconds;
        [SerializeField] Button playBtn;
        [SerializeField] Button pauseBtn;
        [SerializeField] Button stopBtn;
        [SerializeField] AudioSource audioSource;

        [SerializeField] RawImage waveformImg;

        OldRiqBeatmap beatmap;

        RiqMetadata metadata2;
        RiqBeatmap beatmap2;

        float audioLength;
        double scheduledTime;
        float currentChunkTime;

        // Start is called before the first frame update
        private void Start()
        {
            musicSlider.maxValue = 1f;
            // RiqBeatmap.OnUpdateEntity += UpdateEntityTest;
            RiqFileHandler.LockCache();

            OldRiqFileHandler.LockCache();
        }

        private void OnApplicationQuit() {
            RiqFileHandler.UnlockCache();
            RiqFileHandler.ClearCache();

            OldRiqFileHandler.UnlockCache();
            OldRiqFileHandler.ClearCache();
        }

        private void Update() {
            if (audioSource.isPlaying)
            {
                double time = AudioSettings.dspTime - scheduledTime;
                musicSlider.value = (float)(time / audioLength);
                TimeSpan t = TimeSpan.FromSeconds(time);
                songProgressFormatted.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
                songProgressSeconds.text = $"{time:0.000} / {audioLength:0.000}";
            }
        }

        // public RiqEntity UpdateEntityTest(string datamodel, RiqEntity entity)
        // {
        //     Debug.Log($"UpdateEntityTest 1: {datamodel}");
        //     // user code would check for datamodel, and local version
        //     // here we use equals for version, but can feasibly be "less than"
        //     // different versions can use branching code to handle multiple cases
        //     if (datamodel == "karateman/hit" && entity.version == 0)
        //     {
        //         Debug.Log($"running entity update on {datamodel} at {entity.beat}");
        //         try
        //         {
        //             entity["type"] = 3;
        //             Debug.Log($"entity \"type\" is now {entity["type"]}");
        //         }
        //         catch (System.Exception e)
        //         {
        //             Debug.Log($"Error updating entity: {e.Message}");
        //         }
        //         return entity;
        //     }
        //     // return null if the entity should be untouched
        //     Debug.Log("skipping entity update as it is not karateman/hit");
        //     return null;
        // }

        IEnumerator LoadMusic()
        {
            IEnumerator load = OldRiqFileHandler.LoadSong();
            while (true)
            {
                object current = load.Current;
                try
                {
                    if (load.MoveNext() == false)
                    {
                        break;
                    }
                    current = load.Current;
                }
                catch (System.IO.FileNotFoundException f)
                {
                    Debug.LogWarning($"chart has no music: {f.Message} {f.StackTrace}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load music: {e.Message}");
                    yield break;
                }
                yield return current;
            }
            Debug.Log("Finished loading music");
            audioSource.clip = OldRiqFileHandler.StreamedAudioClip;
            audioLength = audioSource.clip.length;
            musicSlider.value = 0;
            songProgressSeconds.text = $"0.000 / {audioLength:0.000}";
            currentChunkTime = 0f;
            // DrawWaveformChunk(currentChunkTime);
        }

        IEnumerator LoadMusic2(int idx)
        {
            IEnumerator load = RiqFileHandler.ReadAudio(idx);
            while (true)
            {
                object current = load.Current;
                try
                {
                    if (load.MoveNext() == false)
                    {
                        break;
                    }
                    current = load.Current;
                }
                catch (System.IO.FileNotFoundException f)
                {
                    Debug.LogWarning($"chart has no music: {f.Message} {f.StackTrace}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load music: {e.Message}");
                    yield break;
                }
                yield return current;
            }
            Debug.Log("Finished loading music");
            audioSource.clip = RiqFileHandler.GetLoadedSong();
            audioLength = audioSource.clip.length;
            musicSlider.value = 0;
            songProgressSeconds.text = $"0.000 / {audioLength:0.000}";
            currentChunkTime = 0f;
            // DrawWaveformChunk(currentChunkTime);
        }

        IEnumerator LoadChartAndMusic(int idx)
        {
            yield return RiqFileHandler.ReadChartAndAudio(idx);

            beatmap2 = RiqFileHandler.GetLoadedChart();
            audioSource.clip = RiqFileHandler.GetLoadedSong();

            audioLength = audioSource.clip.length;
            musicSlider.value = 0;
            songProgressSeconds.text = $"0.000 / {audioLength:0.000}";
            currentChunkTime = 0f;
        }

        // private void DrawWaveformChunk(float startTime)
        // {
        //     Vector2 imgSize = waveformImg.GetPixelAdjustedRect().size;
        //     StartCoroutine(PaintWaveformSpectrum(startTime, 1f, Mathf.RoundToInt(imgSize.x), Mathf.RoundToInt(imgSize.y), Color.yellow));
        // }

            // https://answers.unity.com/questions/1603418/how-to-create-waveform-texture-from-audioclip.html
            // and
            // https://answers.unity.com/questions/699595/how-to-generate-waveform-from-audioclip.html
            // with modifications to only render chunks of audio
            // public IEnumerator PaintWaveformSpectrum(float startTime, float length, int width, int height, Color col) {
            //     AudioClip audio = RiqFileHandler.StreamedAudioClip;
            //     if (audio == null) yield break;

            //     int sampleRate = audio.frequency;
            //     int channels = audio.channels;
            //     int numSamples = Mathf.RoundToInt(length * sampleRate);

            //     Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            //     yield return RiqFileHandler.GetSongSamples(Mathf.RoundToInt(startTime * sampleRate), numSamples);
            //     float[] samples = RiqFileHandler.LastSongChunk;
            //     if (samples == null) yield break;

            //     float[] waveform = new float[width];
            //     float packSize = ((float)samples.Length / (float)width);
            //     int waveIdx = 0;
            //     for (float i = 0; Mathf.RoundToInt(i) < samples.Length && waveIdx < waveform.Length; i += packSize)
            //     {
            //         waveform[waveIdx] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
            //         waveIdx++;
            //     }

            //     for (int x = 0; x < width; x++) {
            //         for (int y = 0; y < height; y++) {
            //             tex.SetPixel(x, y, Color.black);
            //         }
            //     }

            //     waveformImg.texture = tex;
            //     for (int x = 0; x < waveform.Length; x++) {
            //         for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++) {
            //             tex.SetPixel(x, ( height / 2 ) + y, col);
            //             tex.SetPixel(x, ( height / 2 ) - y, col);
            //         }
            //         // tex.Apply();
            //         // yield return null;
            //     }
            //     tex.Apply();
            // }

        public void OnImportPressed()
        {
            var extensions = new[] {
                new ExtensionFilter("RIQ-compatible", "riq"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            try
            {
                if (paths.Length == 0) return;
                string tmpDir = RiqFileHandler.Extract(paths[0]);
                int version = RiqFileHandler.CheckVersion();

                if (version < 2)
                {
#if JUKEBOX_V1
                    // try to convert
                    RiqFileHandler.UpgradeOldStructure();
#else
                    statusTxt.text = "RIQ version is not supported.";
                    return;
#endif
                }

                metadata2 = RiqFileHandler.ReadMetadata();
                StartCoroutine(LoadChartAndMusic(0));

                statusTxt.text = "Imported RIQ successfully!";
                return;
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error importing RIQ: {e.Message}";
                Debug.LogException(e);
                return;
            }

        }

        public void OnCreate2Pressed()
        {
            if (beatmap2 == null)
            {
                metadata2 = new RiqMetadata();
                RiqFileHandler.WriteMetadata(metadata2);

                beatmap2 = new RiqBeatmap();
                RiqFileHandler.WriteChart(0, beatmap2);
            }
        }

        public void OnCreatePressed()
        {
            if (beatmap == null)
            {
                beatmap = new OldRiqBeatmap();
                beatmap.data.properties = 
                    new Dictionary<string, object>() {
                        // mapper set properties? (future: use this to flash the button)
                        {"propertiesmodified", false},

                        ////// CATEGORY 1: SONG INFO
                        // general chart info
                        {"remixtitle", "New Remix"},        // chart name
                        {"remixauthor", "Your Name"},       // charter's name
                        {"remixdesc", "Remix Description"}, // chart description
                        {"remixlevel", 1},                  // chart difficulty (maybe offer a suggestion but still have the mapper determine it)
                        {"remixtempo", 120f},               // avg. chart tempo
                        {"remixtags", ""},                  // chart tags
                        {"icontype", 0},                    // chart icon (presets, custom - future)
                        {"iconurl", ""},                    // custom icon location (future)
                        {"challengetype", 0},               // perfect challenge type

                        // chart song info
                        {"idolgenre", "Song Genre"},        // song genre
                        {"idolsong", "Song Name"},          // song name
                        {"idolcredit", "Artist"},           // song artist

                        ////// CATEGORY 2: PROLOGUE AND EPILOGUE
                        // chart prologue
                        {"prologuetype", 0},                // prologue card animation (future)
                        {"prologuecaption", "Remix"},       // prologue card sub-title (future)

                        // chart results screen messages
                        {"resultcaption", "Rhythm League Notes"},                       // result screen header
                        {"resultcommon_hi", "Good rhythm."},                            // generic "Superb" message (one-liner, or second line for single-type)
                        {"resultcommon_ok", "Eh. Passable."},                           // generic "OK" message (one-liner, or second line for single-type)
                        {"resultcommon_ng", "Try harder next time."},                   // generic "Try Again" message (one-liner, or second line for single-type)

                            // the following are shown / hidden in-editor depending on the tags of the games used
                        {"resultnormal_hi", "You show strong fundamentals."},           // "Superb" message for normal games (two-liner)
                        {"resultnormal_ng", "Work on your fundamentals."},              // "Try Again" message for normal games (two-liner)

                        {"resultkeep_hi", "You kept the beat well."},                   // "Superb" message for keep-the-beat games (two-liner)
                        {"resultkeep_ng", "You had trouble keeping the beat."},         // "Try Again" message for keep-the-beat games (two-liner)

                        {"resultaim_hi", "You had great aim."},                         // "Superb" message for aim games (two-liner)
                        {"resultaim_ng", "Your aim was a little shaky."},               // "Try Again" message for aim games (two-liner)

                        {"resultrepeat_hi", "You followed the example well."},          // "Superb" message for call-and-response games (two-liner)
                        {"resultrepeat_ng", "Next time, follow the example better."},   // "Try Again" message for call-and-response games (two-liner)
                };
                OldRiqFileHandler.WriteRiq(beatmap);
            }
        }

        public void OnMusicSelect2Pressed()
        {
            if (beatmap2 == null)
            {
                OnCreate2Pressed();
            }

            var extensions = new [] {
                new ExtensionFilter("Audio File", "ogg", "wav", "mp3", "aiff", "aifc"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            try
            {
                if (paths.Length == 0) return;
                RiqFileHandler.WriteAudio(beatmap2, paths[0]);
                StartCoroutine(LoadMusic2(0));
                return;
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error selecting music file: {e.Message}";
                return;
            }
        }

        public void OnMusicSelectPressed()
        {
            if (beatmap == null)
            {
                beatmap = new OldRiqBeatmap();
                OldRiqFileHandler.WriteRiq(beatmap);
            }
            var extensions = new [] {
                new ExtensionFilter("Audio File", "ogg", "wav", "mp3", "aiff", "aifc"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            try
            {
                if (paths.Length == 0) return;
                OldRiqFileHandler.WriteSong(paths[0]);
                StartCoroutine(LoadMusic());
                return;
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error selecting music file: {e.Message}";
                return;
            }
        }

        public void OnPackPressed()
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Save packed RIQ", "", "remix", "riq");
            try
            {
                OldRiqFileHandler.PackRiq(path);
                statusTxt.text = "Packed RIQ successfully!";
                return;
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error packing RIQ: {e.Message}";
                return;
            }
        }

        public void OnPack2Pressed()
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Save packed RIQ v2", "", "remix", "riq");
            try
            {
                RiqFileHandler.WriteMetadata(metadata2);
                RiqFileHandler.WriteChart(0, beatmap2);
                RiqFileHandler.Pack(path);
                statusTxt.text = "Packed RIQ successfully!";
                return;
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error packing RIQ: {e.Message}";
                return;
            }
        }

        public void OnSliderValueChanged()
        {
            if (!audioSource.isPlaying)
                audioSource.time = musicSlider.value * audioLength;
        }

        public void OnPlay2Pressed()
        {
            statusTxt.text = "Now Playing";
            scheduledTime = AudioSettings.dspTime - (musicSlider.value * audioLength) - beatmap2.Offset;
            audioSource.time = Mathf.Max((musicSlider.value * audioLength) + (float)beatmap2.Offset, 0f);
            audioSource.PlayScheduled(scheduledTime);
        }

        public void OnPlayPressed()
        {
            Debug.Log(beatmap.data.offset);
            statusTxt.text = "Now Playing";
            scheduledTime = AudioSettings.dspTime - (musicSlider.value * audioLength) - beatmap.data.offset;
            audioSource.time = Mathf.Max((musicSlider.value * audioLength) + (float)beatmap.data.offset, 0f);
            audioSource.PlayScheduled(scheduledTime);
        }

        public void OnPausePressed()
        {
            statusTxt.text = "Audio Paused";
            audioSource.Pause();
        }

        public void OnStopPressed()
        {
            statusTxt.text = "Audio Stopped";
            audioSource.Stop();
            musicSlider.value = 0;
            songProgressFormatted.text = $"00:00";
            songProgressSeconds.text = $"0.000 / {audioLength:0.000}";
        }

        public void OnPreviousChunkPressed()
        {
            if (currentChunkTime - 1f < 0f) return;
            currentChunkTime -= 1f;
            // DrawWaveformChunk(currentChunkTime);
        }

        public void OnNextChunkPressed()
        {
            if (currentChunkTime + 1f > audioLength) return;
            currentChunkTime += 1f;
            // DrawWaveformChunk(currentChunkTime);
        }

        public void OpenCacheFolder()
        {
            // open the Application.temporaryCachePath folder in explorer
            System.Diagnostics.Process.Start(Application.temporaryCachePath);
        }
    }
}