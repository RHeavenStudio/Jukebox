using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

using Jukebox;
using Newtonsoft.Json;

namespace Jukebox.Tests
{
    public class TestsManager : MonoBehaviour
    {
        [SerializeField] TMP_InputField pathInput;
        [SerializeField] TMP_Text statusTxt;

        RiqBeatmap beatmap;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        public void OnImportPressed()
        {
            string path = pathInput.text;
            try
            {
                string tmpDir = RIQReader.ExtractRiq(path);
                beatmap = RIQReader.ReadRiq();

                // foreach (RiqEntity entity in beatmap.data.entities)
                // {
                //     Debug.Log(JsonConvert.SerializeObject(entity, Formatting.Indented));
                // }
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error importing RIQ: {e.Message}";
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
    }
}