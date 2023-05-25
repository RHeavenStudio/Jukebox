using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FullSerializer;
using Jukebox.Legacy;

namespace Jukebox
{
    public class RIQBeatmap : MonoBehaviour
    {
        public RiqBeatmapData data;
    }

    [Serializable]
    [fsObject(VersionString = "1", PreviousModels = new Type[] { typeof(DynamicBeatmap) })]
    public struct RiqBeatmapData
    {
        public string riqVersion;
        public string riqOrigin;
        public double offset;

        public Dictionary<string, object> properties;
        public List<RiqEntity> entities;
        public List<RiqEntity> tempoChanges;
        public List<RiqEntity> volumeChanges;
        public List<RiqEntity> beatmapSections;

        public RiqBeatmapData(DynamicBeatmap riq)
        {
            riqVersion = "1";
            riqOrigin = "HeavenStudio";
            offset = riq.firstBeatOffset;
            properties = riq.properties;

            entities = new List<RiqEntity>();
            foreach (DynamicBeatmap.DynamicEntity entity in riq.entities)
            {
                RiqEntity e = new RiqEntity();
                e.data.type = "riq__Entity";
                e.data.datamodel = entity.datamodel;
                e.data.version = 0;
                e.data.beat = entity.beat;
                e.data.length = entity.length;
                e.data.dynamicData = entity.DynamicData;

                e.CreateProperty("track", entity.track);
            }

            tempoChanges = new List<RiqEntity>();
            foreach (DynamicBeatmap.TempoChange tempo in riq.tempoChanges)
            {
                RiqEntity e = new RiqEntity();
                e.data.type = "riq__TempoChange";
                e.data.datamodel = "global/tempo change";
                e.data.version = 0;
                e.data.beat = tempo.beat;
                e.data.length = tempo.length;

                e.CreateProperty("tempo", tempo.tempo);
                e.CreateProperty("swing", 0f);
            }

            volumeChanges = new List<RiqEntity>();
            foreach (DynamicBeatmap.VolumeChange volume in riq.volumeChanges)
            {
                RiqEntity e = new RiqEntity();
                e.data.datamodel = "global/volume change";
                e.data.type = "riq__VolumeChange";
                e.data.version = 0;
                e.data.beat = volume.beat;
                e.data.length = volume.length;

                e.CreateProperty("volume", volume.volume);
            }

            beatmapSections = new List<RiqEntity>();
            foreach (DynamicBeatmap.ChartSection section in riq.beatmapSections)
            {
                RiqEntity e = new RiqEntity();
                e.data.datamodel = "global/section marker";
                e.data.type = "riq__SectionMarker";
                e.data.version = 0;
                e.data.beat = section.beat;
                e.data.length = 0;

                e.CreateProperty("sectionName", section.sectionName);
                e.CreateProperty("isCheckpoint", section.isCheckpoint);
                e.CreateProperty("startPerfect", section.startPerfect);
            }
        }
    }
}