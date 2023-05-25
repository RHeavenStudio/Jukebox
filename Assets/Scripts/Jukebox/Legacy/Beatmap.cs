using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

using FullSerializer;

namespace Jukebox.Legacy
{
    [Serializable]
    [fsObject(VersionString = "-1")]
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
            public float length;
            public float valA;
            public float valB;
            public float valC;
            public bool toggle;
            public int type;
            public int type2;
            public int type3;
            public int type4;
            public int type5;
            public int type6;
            public EasingFunction.Ease ease;
            public Color colorA;
            public Color colorB;
            public Color colorC;
            public Color colorD;
            public Color colorE;
            public Color colorF;
            public string text1;
            public string text2;
            public string text3;
            public float swing;
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