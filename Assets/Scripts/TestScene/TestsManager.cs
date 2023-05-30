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

        RiqBeatmap beatmap;
        float audioLength;
        double scheduledTime;

        // Start is called before the first frame update
        private void Start()
        {
            musicSlider.maxValue = 1f;
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

        IEnumerator LoadMusic()
        {
            yield return RiqFileHandler.LoadSong();
            audioSource.clip = RiqFileHandler.LoadedAudioClip;
            audioLength = audioSource.clip.length;
            musicSlider.value = 0;
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
                RiqBeatmap newBeatmap = new RiqBeatmap();
                GUIUtility.systemCopyBuffer = newBeatmap.Serialize();
            }
            else
            {
                GUIUtility.systemCopyBuffer = beatmap.Serialize();
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
        }
    }
}