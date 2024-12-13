using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jukebox
{
    [JsonConverter(typeof(RiqBeatmapConverter))]
    public class RiqBeatmap
    {
        readonly List<RiqEntity> entities = new();
        public List<RiqEntity> Entities { get => entities; }

        public int Version { get => version; }
        public double Offset { get => offset; }

        double offset;
        int version;

        public RiqBeatmap(int version = 2)
        {
            this.version = version;
        }

        /// <summary>
        /// Find all entities of a specific type.
        /// </summary>
        /// <param name="type">Type of entity to search for</param>
        /// <returns>List of <see cref="RiqEntity"/> objects with matching type</returns>
        public List<RiqEntity> FindEntitiesByType(string type)
        {
            return entities.FindAll(e => e.Type == type);
        }

        /// <summary>
        /// Create a new <see cref="RiqEntity"/> object and add it to the beatmap.
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <returns>The added entity</returns>
        public RiqEntity CreateEntity(RiqEntity entity)
        {
            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Create a new <see cref="RiqEntity"/> object and add it to the beatmap.
        /// </summary>
        /// <param name="datamodel">Datamodel of the new entity</param>
        /// <param name="type">Type of the new entity</param>
        /// <returns>The added entity</returns>
        public RiqEntity CreateEntity(string datamodel, string type = "riq__Entity")
        {
            RiqEntity entity = new(type, datamodel, version);
            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Add a new <see cref="RiqEntity"/> object to the beatmap.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <returns>This <see cref="RiqBeatmap"/> object</returns>
        public RiqBeatmap AddEntity(RiqEntity entity)
        {
            CreateEntity(entity);
            return this;
        }

        /// <summary>
        /// Add a new <see cref="RiqEntity"/> object to the beatmap.
        /// </summary>
        /// <param name="datamodel">Datamodel of the new entity</param>
        /// <param name="type">Type of the new entity</param>
        /// <param name="addedEntity">The added entity</param>
        /// <returns>This <see cref="RiqBeatmap"/> object</returns>
        public RiqBeatmap AddEntity(string datamodel, string type, out RiqEntity addedEntity)
        {
            addedEntity = CreateEntity(datamodel, type);
            return this;
        }

        /// <summary>
        /// Set the offset of the beatmap.
        /// </summary>
        /// <param name="offset">Offset in seconds</param>
        /// <returns>This <see cref="RiqBeatmap"/> object</returns>
        public RiqBeatmap WithOffset(double offset)
        {
            this.offset = offset;
            return this;
        }

        /// <summary>
        /// Serialize this <see cref="RiqBeatmap"/> object to a JSON string.
        /// </summary>
        /// <returns>JSON string</returns>
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
        public List<RiqEntity> TempoChanges { get => FindEntitiesByType("riq__TempoChange"); }
        [System.Obsolete("Use FindEntitiesByType instead.")]
        public List<RiqEntity> VolumeChanges { get => FindEntitiesByType("riq__VolumeChange"); }
        [System.Obsolete("Use FindEntitiesByType instead.")]
        public List<RiqEntity> SectionMarkers { get => FindEntitiesByType("riq__SectionMarker"); }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewEntity(string datamodel, double beat, float length)
        {
            RiqEntity e = CreateEntity(datamodel);

            e.CreateProperty("beat", beat);
            e.CreateProperty("length", length);
            e.CreateProperty("track", 0);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewEntity(string datamodel, double beat, float length, Dictionary<string, object> dynamicData)
        {
            RiqEntity e = AddNewEntity(datamodel, beat, length);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewTempoChange(double beat, float tempo)
        {
            RiqEntity e = CreateEntity("global/tempo change", "riq__TempoChange");

            e.CreateProperty("beat", beat);
            e.CreateProperty("tempo", tempo);
            e.CreateProperty("swing", 0f);
            e.CreateProperty("swingDivision", 1f);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewTempoChange(double beat, float tempo, Dictionary<string, object> dynamicData)
        {
            RiqEntity e = AddNewTempoChange(beat, tempo);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewVolumeChange(double beat, float volume)
        {
            RiqEntity e = CreateEntity("global/volume change", "riq__VolumeChange");

            e.CreateProperty("beat", beat);
            e.CreateProperty("volume", volume);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewVolumeChange(double beat, float volume, Dictionary<string, object> dynamicData)
        {
            RiqEntity e = AddNewVolumeChange(beat, volume);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewSectionMarker(double beat, string markerName)
        {
            RiqEntity e = CreateEntity("global/section marker", "riq__SectionMarker");

            e.CreateProperty("beat", beat);
            e.CreateProperty("sectionName", markerName);

            entities.Add(e);
            return e;
        }

        [System.Obsolete("Use AddEntity instead and manually specify the type.")]
        public RiqEntity AddNewSectionMarker(double beat, string markerName, Dictionary<string, object> dynamicData)
        {
            RiqEntity e = AddNewSectionMarker(beat, markerName);

            foreach (KeyValuePair<string, object> kvp in dynamicData)
            {
                e.CreateProperty(kvp.Key, kvp.Value);
            }

            return e;
        }
#endregion
    }
}