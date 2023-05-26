using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

using Jukebox;

namespace Jukebox.Tests
{
    public class TestsManager : MonoBehaviour
    {
        [SerializeField] TMP_InputField pathInput;
        [SerializeField] TMP_Text statusTxt;

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
            }
            catch (System.Exception e)
            {
                statusTxt.text = $"Error importing RIQ: {e.Message}";
            }
        }
    }
}