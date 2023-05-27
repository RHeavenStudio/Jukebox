using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Jukebox;
using Newtonsoft.Json;

namespace Jukebox.Tests
{
    public class TestsManager : MonoBehaviour
    {
        [SerializeField] TMP_InputField pathInput;
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
            string path = pathInput.text;
            try
            {
                string tmpDir = RiqFileHandler.ExtractRiq(path);
                beatmap = RiqFileHandler.ReadRiq();

                StartCoroutine(LoadMusic());
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error importing RIQ: {e.Message}";
                return;
            }
            statusTxt.text = "Imported RIQ successfully!";
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