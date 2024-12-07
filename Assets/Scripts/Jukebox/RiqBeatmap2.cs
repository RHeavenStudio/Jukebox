using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jukebox
{
    [JsonConverter(typeof(RiqBeatmap2Converter))]
    public class RiqBeatmap2
    {
        readonly List<RiqEntity2> entities = new();
        public List<RiqEntity2> Entities { get => entities; }

        public int Version { get => version; }
        public string Origin { get => origin; }

        int version;
        string origin;

        public RiqBeatmap2(int version = 2, string origin = "Jukebox")
        {
            this.version = version;
            this.origin = origin;
        }

        public List<RiqEntity2> FindEntitiesByType(string type)
        {
            return entities.FindAll(e => e.Type == type);
        }

        public RiqEntity2 AddEntity(string datamodel, string type = "riq__Entity")
        {
            RiqEntity2 entity = new(type, datamodel, version);
            entities.Add(entity);
            return entity;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Arrays,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            });
        }

#region Obsolete
        [System.Obsolete("Use FindEntitiesByType instead.")]
        public List<RiqEntity2> TempoChanges { get => FindEntitiesByType("riq__TempoChange"); }
        [System.Obsolete("Use FindEntitiesByType instead.")]
        public List<RiqEntity2> VolumeChanges { get => FindEntitiesByType("riq__VolumeChange"); }
        [System.Obsolete("Use FindEntitiesByType instead.")]
        public List<RiqEntity2> SectionMarkers { get => FindEntitiesByType("riq__SectionMarker"); }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewEntity(string datamodel, double beat, float length)
        {
            RiqEntity2 e = AddEntity(datamodel);

            e.CreateProperty("beat", beat);
            e.CreateProperty("length", length);
            e.CreateProperty("track", 0);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewEntity(string datamodel, double beat, float length, Dictionary<string, object> dynamicData)
        {
            RiqEntity2 e = AddNewEntity(datamodel, beat, length);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewTempoChange(double beat, float tempo)
        {
            RiqEntity2 e = AddEntity("global/tempo change", "riq__TempoChange");

            e.CreateProperty("beat", beat);
            e.CreateProperty("tempo", tempo);
            e.CreateProperty("swing", 0f);
            e.CreateProperty("swingDivision", 1f);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewTempoChange(double beat, float tempo, Dictionary<string, object> dynamicData)
        {
            RiqEntity2 e = AddNewTempoChange(beat, tempo);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewVolumeChange(double beat, float volume)
        {
            RiqEntity2 e = AddEntity("global/volume change", "riq__VolumeChange");

            e.CreateProperty("beat", beat);
            e.CreateProperty("volume", volume);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewVolumeChange(double beat, float volume, Dictionary<string, object> dynamicData)
        {
            RiqEntity2 e = AddNewVolumeChange(beat, volume);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewSectionMarker(double beat, string markerName)
        {
            RiqEntity2 e = AddEntity("global/section marker", "riq__SectionMarker");

            e.CreateProperty("beat", beat);
            e.CreateProperty("sectionName", markerName);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity2 AddNewSectionMarker(double beat, string markerName, Dictionary<string, object> dynamicData)
        {
            RiqEntity2 e = AddNewSectionMarker(beat, markerName);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }
#endregion
    }
}