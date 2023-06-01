using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using SFB;

using Jukebox;
using Newtonsoft.Json;

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

        RiqBeatmap beatmap;
        float audioLength;
        double scheduledTime;
        float currentChunkTime;

        // Start is called before the first frame update
        private void Start()
        {
            musicSlider.maxValue = 1f;
            RiqBeatmap.OnUpdateEntity += UpdateEntityTest;
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

        public RiqEntity? UpdateEntityTest(string datamodel, RiqEntity entity)
        {
            Debug.Log($"UpdateEntityTest 1: {datamodel}");
            // user code would check for datamodel, and local version
            // here we use equals for version, but can feasibly be "less than"
            // different versions can use branching code to handle multiple cases
            if (datamodel == "karateman/hit" && entity.version == 0)
            {
                Debug.Log($"running entity update on {datamodel} at {entity.beat}");
                try
                {
                    entity["type"] = 3;
                    Debug.Log($"entity \"type\" is now {entity["type"]}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"Error updating entity: {e.Message}");
                }
                return entity;
            }
            // return null if the entity should be untouched
            Debug.Log("skipping entity update as it is not karateman/hit");
            return null;
        }

        IEnumerator LoadMusic()
        {
            yield return RiqFileHandler.LoadSong();
            audioSource.clip = RiqFileHandler.StreamedAudioClip;
            audioLength = audioSource.clip.length;
            musicSlider.value = 0;
            songProgressSeconds.text = $"0.000 / {audioLength:0.000}";
            currentChunkTime = 0f;
            DrawWaveformChunk(currentChunkTime);
        }

        private void DrawWaveformChunk(float startTime)
        {
            Vector2 imgSize = waveformImg.GetPixelAdjustedRect().size;
            StartCoroutine(PaintWaveformSpectrum(startTime, 1f, Mathf.RoundToInt(imgSize.x), Mathf.RoundToInt(imgSize.y), Color.yellow));
        }

        // https://answers.unity.com/questions/1603418/how-to-create-waveform-texture-from-audioclip.html
        // and
        // https://answers.unity.com/questions/699595/how-to-generate-waveform-from-audioclip.html
        // with modifications to only render chunks of audio
        public IEnumerator PaintWaveformSpectrum(float startTime, float length, int width, int height, Color col) {
            AudioClip audio = RiqFileHandler.StreamedAudioClip;
            if (audio == null) yield break;

            int sampleRate = audio.frequency;
            int channels = audio.channels;
            int numSamples = Mathf.RoundToInt(length * sampleRate);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            yield return RiqFileHandler.GetSongSamples(Mathf.RoundToInt(startTime * sampleRate), numSamples);
            float[] samples = RiqFileHandler.LastSongChunk;
            if (samples == null) yield break;

            float[] waveform = new float[width];
            float packSize = ((float)samples.Length / (float)width);
            int waveIdx = 0;
            for (float i = 0; Mathf.RoundToInt(i) < samples.Length && waveIdx < waveform.Length; i += packSize)
            {
                waveform[waveIdx] = Mathf.Abs(samples[Mathf.RoundToInt(i)]);
                waveIdx++;
            }
        
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    tex.SetPixel(x, y, Color.black);
                }
            }

            waveformImg.texture = tex;
            for (int x = 0; x < waveform.Length; x++) {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++) {
                    tex.SetPixel(x, ( height / 2 ) + y, col);
                    tex.SetPixel(x, ( height / 2 ) - y, col);
                }
                // tex.Apply();
                // yield return null;
            }
            tex.Apply();
        }

        public void OnImportPressed()
        {
            var extensions = new [] {
                new ExtensionFilter("RIQ-compatible", "riq", "tengoku", "rhmania"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            try
            {
                if (paths.Length == 0) return;
                string tmpDir = RiqFileHandler.ExtractRiq(paths[0]);
                beatmap = RiqFileHandler.ReadRiq();

                StartCoroutine(LoadMusic());
                statusTxt.text = "Imported RIQ successfully!";
                return;
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error importing RIQ: {e.Message}";
                return;
            }
        }

        public void OnCreatePressed()
        {
            if (beatmap == null)
            {
                beatmap = new RiqBeatmap();
                GUIUtility.systemCopyBuffer = beatmap.Serialize();
            }
            else
            {
                GUIUtility.systemCopyBuffer = beatmap.Serialize();
            }
        }

        public void OnMusicSelectPressed()
        {
            if (beatmap == null)
            {
                beatmap = new RiqBeatmap();
                RiqFileHandler.WriteRiq(beatmap);
            }
            var extensions = new [] {
                new ExtensionFilter("Audio File", "ogg", "wav", "mp3", "aiff", "aifc"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            try
            {
                if (paths.Length == 0) return;
                RiqFileHandler.WriteSong(paths[0]);
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
                RiqFileHandler.PackRiq(path);
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
            DrawWaveformChunk(currentChunkTime);
        }

        public void OnNextChunkPressed()
        {
            if (currentChunkTime + 1f > audioLength) return;
            currentChunkTime += 1f;
            DrawWaveformChunk(currentChunkTime);
        }
    }
}