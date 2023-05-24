using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jukebox
{
    public class RIQBeatmap : MonoBehaviour
    {
        public RiqBeatmapData data;
    }

    [Serializable]
    public struct RiqBeatmapData
    {
        public int riqVersion;
        public string riqOrigin;

        public Dictionary<string, object> properties;
        public List<RiqEntity> entities;
    }
}