using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using UnityEngine;

using FullSerializer;

namespace Jukebox.Legacy
{
    [Serializable]
    [fsObject(VersionString = "0", PreviousModels = new Type[] { typeof(Beatmap) })]
    public class DynamicBeatmap
    {
        public DynamicBeatmap(Beatmap beatmap)
        {
            bpm = beatmap.bpm;
            musicVolume = beatmap.musicVolume;
            firstBeatOffset = beatmap.firstBeatOffset;

            foreach (Beatmap.Entity entity in beatmap.entities)
            {
                DynamicEntity e = new DynamicEntity();
                e.beat = entity.beat;
                e.track = entity.track;
                e.length = entity.length;
                e.swing = entity.swing;
                e.datamodel = entity.datamodel;

                e.DynamicData = new Dictionary<string, dynamic>();
                e.DynamicData.Add("valA", entity.valA);
                e.DynamicData.Add("valB", entity.valB);
                e.DynamicData.Add("valC", entity.valC);
                e.DynamicData.Add("toggle", entity.toggle);
                e.DynamicData.Add("type", entity.type);
                e.DynamicData.Add("type2", entity.type2);
                e.DynamicData.Add("type3", entity.type3);
                e.DynamicData.Add("type4", entity.type4);
                e.DynamicData.Add("type5", entity.type5);
                e.DynamicData.Add("type6", entity.type6);
                e.DynamicData.Add("ease", entity.ease);
                e.DynamicData.Add("colorA", entity.colorA);
                e.DynamicData.Add("colorB", entity.colorB);
                e.DynamicData.Add("colorC", entity.colorC);
                e.DynamicData.Add("colorD", entity.colorD);
                e.DynamicData.Add("colorE", entity.colorE);
                e.DynamicData.Add("colorF", entity.colorF);
                e.DynamicData.Add("text1", entity.text1);
                e.DynamicData.Add("text2", entity.text2);
                e.DynamicData.Add("text3", entity.text3);

                entities.Add(e);
            }

            foreach (Beatmap.TempoChange tempo in beatmap.tempoChanges)
            {
                TempoChange t = new TempoChange();
                t.beat = tempo.beat;
                t.length = tempo.length;
                t.tempo = tempo.tempo;

                tempoChanges.Add(t);
            }

            foreach (Beatmap.VolumeChange volume in beatmap.volumeChanges)
            {
                VolumeChange v = new VolumeChange();
                v.beat = volume.beat;
                v.length = volume.length;
                v.volume = volume.volume;

                volumeChanges.Add(v);
            }
        }


        public static int CurrentRiqVersion = 0;
        public float bpm;

        [DefaultValue(100)] public int musicVolume; // In percent (1-100)
        
        public Dictionary<string, object> properties = 
            new Dictionary<string, object>() {
                // software version (MajorMinorPatch, revision)
                {"productversion", 000},
                {"productsubversion", 0},
                // file format version
                {"riqversion", CurrentRiqVersion},
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

        public List<DynamicEntity> entities = new List<DynamicEntity>();
        public List<TempoChange> tempoChanges = new List<TempoChange>();
        public List<VolumeChange> volumeChanges = new List<VolumeChange>();
        public List<ChartSection> beatmapSections = new List<ChartSection>();
        public float firstBeatOffset;

        [Serializable]
        public class DynamicEntity
        {
            public float beat;
            public int track;
            public float length;
            public float swing;
            public Dictionary<string, dynamic> DynamicData = new Dictionary<string, dynamic>();

            public string datamodel;
        }

        [Serializable]
        public class TempoChange
        {
            public float beat;
            public float length;
            public float tempo;
        }

        [Serializable]
        public class VolumeChange
        {
            public float beat;
            public float length;
            public float volume;
        }

        [Serializable]
        public class ChartSection
        {
            public float beat;
            public bool startPerfect;
            public string sectionName;
            public bool isCheckpoint;   // really don't think we need this but who knows
        }
    }
}