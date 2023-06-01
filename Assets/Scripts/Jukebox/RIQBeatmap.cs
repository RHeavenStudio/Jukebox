using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;
using Jukebox.Legacy;

namespace Jukebox
{
    public class RiqBeatmap
    {
        public delegate RiqEntity? EntityUpdateHandler(string datamodel, RiqEntity entity);

        /// <summary>
        /// Use this event to check for and handle entity updates
        /// return null to leave the entity untouched
        /// </summary>
        public static event EntityUpdateHandler OnUpdateEntity;

        public RiqBeatmapData data;

        public List<RiqEntity> Entities => data.entities;
        public List<RiqEntity> TempoChanges => data.tempoChanges;
        public List<RiqEntity> VolumeChanges => data.volumeChanges;
        public List<RiqEntity> SectionMarkers => data.beatmapSections;

        public object this[string propertyName]
        {
            get
            {
                return data.properties[propertyName];
            }
            set
            {
                if (data.properties.ContainsKey(propertyName))
                    data.properties[propertyName] = value;
                else
                {
                    data.properties.Add(propertyName, value);
                }
            }
        }

        public RiqBeatmap()
        {
            data = new RiqBeatmapData();
            data.riqVersion = "1";
            data.riqOrigin = "HeavenStudio";
            data.properties = new();
            data.entities = new();
            data.tempoChanges = new();
            data.volumeChanges = new();
            data.beatmapSections = new();
        }

        public RiqBeatmap(string json)
        {
            if (json == string.Empty || json == null) throw new ArgumentNullException("json", "json cannot be null or empty");

            // scan the json to check if we need to do conversion
            // if the json is missing the "riqVersion" property, it's a v0 riq
            // if the json is missing the "properties" property, it's an even older legacy type (tengoku, rhmania)
            // otherwise, it's a v1 riq

            // first check for v0 riqs
            var riq0Def = new { riqVersion = "" };
            var riqVersion = JsonConvert.DeserializeAnonymousType(json, riq0Def, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
            });
            if (riqVersion.riqVersion == "" || riqVersion.riqVersion == null)
            {
                Debug.Log("Detected \"v0\" riq (DynamicBeatmap)");
                DynamicBeatmap riq0 = JsonConvert.DeserializeObject<DynamicBeatmap>(json, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.None,
                });
                data = ConvertFromDynamicBeatmap(riq0);
                RunUpdateHandlers(true);
                return;
            }

            // then check for legacy types
            var tengokuDef = new { properties = new Dictionary<string, object>() };
            var properties = JsonConvert.DeserializeAnonymousType(json, tengokuDef, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
            });
            if (properties.properties == null)
            {
                Debug.Log("Detected legacy type (tengoku, rhmania)");
                Beatmap tengoku = JsonConvert.DeserializeObject<Beatmap>(json);
                DynamicBeatmap riq0 = DynamicBeatmap.ConvertFromTengoku(tengoku);
                data = ConvertFromDynamicBeatmap(riq0);
                RunUpdateHandlers(true);
                return;
            }

            // v1 riq detected
            Debug.Log("Detected \"v1\" riq (RiqBeatmapData)");
            data = JsonConvert.DeserializeObject<RiqBeatmapData>(json, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
            });
            RunUpdateHandlers();
        }

        void RunUpdateHandlers(bool forceUpdate = false)
        {
            // lets user code update entities
            // return null if no changes should be done
            Debug.Log("Running entity update handlers");
            for (int i = 0; i < data.entities.Count; i++)
            {
                RiqEntity? temp = OnUpdateEntity?.Invoke(data.entities[i].datamodel, data.entities[i]);
                if (temp != null)
                {
                    data.entities[i] = (RiqEntity)temp;
                    forceUpdate = true;
                }
            }

            for (int i = 0; i < data.tempoChanges.Count; i++)
            {
                RiqEntity? temp = OnUpdateEntity?.Invoke(data.tempoChanges[i].datamodel, data.tempoChanges[i]);
                if (temp != null)
                {
                    data.tempoChanges[i] = (RiqEntity)temp;
                    forceUpdate = true;
                }
            }

            for (int i = 0; i < data.volumeChanges.Count; i++)
            {
                RiqEntity? temp = OnUpdateEntity?.Invoke(data.volumeChanges[i].datamodel, data.volumeChanges[i]);
                if (temp != null)
                {
                    data.tempoChanges[i] = (RiqEntity)temp;
                    forceUpdate = true;
                }
            }

            for (int i = 0; i < data.beatmapSections.Count; i++)
            {
                RiqEntity? temp = OnUpdateEntity?.Invoke(data.beatmapSections[i].datamodel, data.beatmapSections[i]);
                if (temp != null)
                {
                    data.tempoChanges[i] = (RiqEntity)temp;
                    forceUpdate = true;
                }
            }

            if (forceUpdate)
                RiqFileHandler.WriteRiq(this);
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings() 
            { 
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
            });
        }

        public RiqEntity AddNewEntity(string datamodel, double beat, float length)
        {
            RiqEntity e = new RiqEntity()
            {
                type = "riq__Entity",
                datamodel = datamodel,
                version = 0,
                beat = beat,
                length = length,
                dynamicData = new(),
            };

            e.CreateProperty("track", 0);
            data.entities.Add(e);
            return e;
        }

        public RiqEntity AddNewTempoChange(double beat, float tempo)
        {
            RiqEntity e = new RiqEntity()
            {
                type = "riq__TempoChange",
                datamodel = "global/tempo change",
                version = 0,
                beat = beat,
                length = 0f,
                dynamicData = new(),
            };

            e.CreateProperty("tempo", tempo);
            e.CreateProperty("swing", 0f);

            data.tempoChanges.Add(e);
            return e;
        }

        public RiqEntity AddNewVolumeChange(double beat, float volume)
        {
            RiqEntity e = new RiqEntity()
            {
                type = "riq__VolumeChange",
                datamodel = "global/volume change",
                version = 0,
                beat = beat,
                length = 0f,
                dynamicData = new(),
            };

            e.CreateProperty("volume", volume);
            e.CreateProperty("fade", EasingFunction.Ease.Instant);

            data.volumeChanges.Add(e);
            return e;
        }

        public RiqEntity AddNewSectionMarker(double beat, string sectionName)
        {
            RiqEntity e = new RiqEntity()
            {
                type = "riq__SectionMarker",
                datamodel = "global/section marker",
                version = 0,
                beat = beat,
                length = 0,
                dynamicData = new(),
            };

            e.CreateProperty("sectionName", sectionName);
            e.CreateProperty("isCheckpoint", false);
            e.CreateProperty("startPerfect", false);
            e.CreateProperty("breakSection", false);
            e.CreateProperty("sectionWeight", 1f);
            e.CreateProperty("extendsPrevious", false);

            data.beatmapSections.Add(e);
            return e;
        }

        static RiqBeatmapData ConvertFromDynamicBeatmap(DynamicBeatmap riq)
        {
            Debug.Log("Updating \"v0\" riq (DynamicBeatmap) to \"v1\" riq (RiqBeatmapData)");
            RiqBeatmapData data = new RiqBeatmapData();
            data.riqVersion = "1";
            data.riqOrigin = "HeavenStudio";
            data.offset = riq.firstBeatOffset;
            data.properties = riq.properties;

            data.entities = new List<RiqEntity>();
            foreach (DynamicBeatmap.DynamicEntity entity in riq.entities)
            {
                RiqEntity e = new RiqEntity();
                e.type = "riq__Entity";
                e.datamodel = entity.datamodel;
                e.version = 0;
                e.beat = entity.beat;
                e.length = entity.length;
                e.dynamicData = new(entity.DynamicData);

                e.CreateProperty("track", entity.track);

                // TODO: updating entities using user-defined methods

                data.entities.Add(e);
            }

            data.tempoChanges = new List<RiqEntity>();
            
            // create an initial tempo change
            RiqEntity initialTempo = new RiqEntity();
            initialTempo.type = "riq__TempoChange";
            initialTempo.datamodel = "global/tempo change";
            initialTempo.version = 0;
            initialTempo.beat = 0d;
            initialTempo.length = 0f;

            initialTempo.CreateProperty("tempo", riq.bpm);
            initialTempo.CreateProperty("swing", 0f);

            data.tempoChanges.Add(initialTempo);

            foreach (DynamicBeatmap.TempoChange tempo in riq.tempoChanges)
            {
                RiqEntity e = new RiqEntity();
                e.type = "riq__TempoChange";
                e.datamodel = "global/tempo change";
                e.version = 0;
                e.beat = tempo.beat;
                e.length = tempo.length;

                e.CreateProperty("tempo", tempo.tempo);
                e.CreateProperty("swing", 0f);

                data.tempoChanges.Add(e);
            }

            data.volumeChanges = new List<RiqEntity>();
            // create an initial volume change
            RiqEntity initialVolume = new RiqEntity();
            initialVolume.datamodel = "global/volume change";
            initialVolume.type = "riq__VolumeChange";
            initialVolume.version = 0;
            initialVolume.beat = 0;
            initialVolume.length = 0;

            initialVolume.CreateProperty("volume", riq.musicVolume);
            initialVolume.CreateProperty("fade", EasingFunction.Ease.Instant);

            data.volumeChanges.Add(initialVolume);

            foreach (DynamicBeatmap.VolumeChange volume in riq.volumeChanges)
            {
                RiqEntity e = new RiqEntity();
                e.datamodel = "global/volume change";
                e.type = "riq__VolumeChange";
                e.version = 0;
                e.beat = volume.beat;
                e.length = volume.length;

                e.CreateProperty("volume", volume.volume);
                e.CreateProperty("fade", EasingFunction.Ease.Instant);

                data.volumeChanges.Add(e);
            }

            data.beatmapSections = new List<RiqEntity>();
            foreach (DynamicBeatmap.ChartSection section in riq.beatmapSections)
            {
                RiqEntity e = new RiqEntity();
                e.datamodel = "global/section marker";
                e.type = "riq__SectionMarker";
                e.version = 0;
                e.beat = section.beat;
                e.length = 0;

                e.CreateProperty("sectionName", section.sectionName);
                e.CreateProperty("isCheckpoint", section.isCheckpoint);
                e.CreateProperty("startPerfect", section.startPerfect);
                e.CreateProperty("breakSection", false);
                e.CreateProperty("sectionWeight", 1f);
                e.CreateProperty("extendsPrevious", false);

                data.beatmapSections.Add(e);
            }

            Debug.Log("Updating \"v0\" riq (DynamicBeatmap) to \"v1\" riq (RiqBeatmapData) - done");
            return data;
        }
    }

    [Serializable]
    public struct RiqBeatmapData
    {
        [DefaultValue("1")] public string riqVersion;
        public string riqOrigin;
        public double offset;

        public Dictionary<string, object> properties;
        public List<RiqEntity> entities;
        public List<RiqEntity> tempoChanges;
        public List<RiqEntity> volumeChanges;
        public List<RiqEntity> beatmapSections;
    }
}