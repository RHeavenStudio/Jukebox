#if JUKEBOX_V0
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

using Newtonsoft.Json;

namespace Jukebox.Legacy
{
    [Serializable]
    public class Beatmap
    {
        public float bpm;

        [DefaultValue(100)] public int musicVolume; // In percent (1-100)
        
        public List<Entity> entities = new List<Entity>();
        public List<TempoChange> tempoChanges = new List<TempoChange>();
        public List<VolumeChange> volumeChanges = new List<VolumeChange>();
        public float firstBeatOffset;

        [Serializable]
        public class Entity : ICloneable
        {
            public float beat;
            public int track;

            // consideration: use arrays instead of hardcoding fixed parameter names
            // note from zeo: yeah definately use arrays
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public float length;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public float valA;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public float valB;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public float valC;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public bool toggle;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int type;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int type2;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int type3;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int type4;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int type5;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int type6;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public EasingFunction.Ease ease;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public Color colorA;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public Color colorB;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public Color colorC;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public Color colorD;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public Color colorE;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public Color colorF;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public string text1;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public string text2;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public string text3;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public float swing;
            public string datamodel;

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        [Serializable]
        public class TempoChange : ICloneable
        {
            public float beat;
            public float length;
            public float tempo;

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        [Serializable]
        public class VolumeChange : ICloneable
        {
            public float beat;
            public float length;
            public float volume;

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }
    }
}
#endif