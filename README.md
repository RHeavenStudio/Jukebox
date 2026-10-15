# Jukebox
Parser and Serializer for the RIQ format for rhythm game charts.

### Installation
Unity Package Manager: add https://github.com/RHeavenStudio/Jukebox.git?path=/Assets/Scripts/Jukebox as a git URL.

### Example Usage
Below are code snippets modified from the test program

Setting up an empty cache for a new RIQ (or resetting the cache for a clean state):
```cs
// should execute sometime at the start of your program
private void Start()
{
    RiqFileHandler.LockCache();
    RiqFileHandler.ClearCache();
}

// should execute at the end of your program's lifetime
private void OnApplicationQuit() 
{
    RiqFileHandler.UnlockCache();
    RiqFileHandler.ClearCache();
}

private void CreateNewRIQ()
{
    // put an empty chart into the cache
    // specifying an origin helps authoring tools identify your program
    RiqMetadata metadata = new(origin: "Jukebox");
    RiqFileHandler.WriteMetadata(metadata);

    RiqBeatmap beatmap = new();
    // an RIQ file can contain multiple charts
    // the first chart is index 0
    RiqFileHandler.WriteChart(0, beatmap);
}
```

Load an RIQ file from the local filesystem:
```cs
// loaded RIQs get stored into a "cache" for future operations
RiqFileHandler.Extract("file/to/riq.riq");
// all further operations assume a chart has already been loaded into the cache
```

Check and convert old file versions (requires the `JUKEBOX_V1` scripting define if converting a "V1" RIQ)
```cs
int version = RiqFileHandler.CheckVersion();

if (version < 2)
{
#if JUKEBOX_V1
    // try to convert
    RiqFileHandler.UpgradeOldStructure();
#else
    statusTxt.text = "RIQ version is not supported.";
#endif
}
```

Load a Chart and its accompanying BGM from the RIQ:
```cs
IEnumerator LoadChartAndMusic(int idx)
{
    // an RIQ file can contain multiple charts, the index specifies which is loaded
    // the first chart is index 0
    yield return RiqFileHandler.ReadChartAndAudio(chartIndex);

    beatmap = RiqFileHandler.GetLoadedChart();
    // by default BGM is streamed from the cache
    audioSource.clip = RiqFileHandler.GetLoadedSong();
}

LoadChartAndMusic(0);
```

Loading new BGM for a chart from the local filesystem:
```cs
// uses the RiqBeatmap object from above
RiqFileHandler.WriteAudio(beatmap, "path/to/song.ogg");

// reload the music
yield return RiqFileHandler.ReadAudio(beatmap.SongName);
audioSource.clip = RiqFileHandler.GetLoadedSong();

//the offset of the song in the chart can be changed if needed
beatmap.WithOffset(-0.01);
```

Read from, create, and modify Entities in a chart:
```cs
// uses the RiqBeatmap object from above
// the datamodel identifies the entity to an action in your game such as spawning a note or playing a sound
// the type gives a broader classification to entities 
// (most currently existing RIQs specify Tempo Changes, Volume Changes, and Section Markers as well as generic Entities)
beatmap.AddEntity("example/datamodel", "riq__Entity", out RiqEntity newEntity1);

// creating entities also can be chained
beatmap.AddEntity("example/datamodel", "riq__Entity", out RiqEntity newEntity2)
    .AddEntity("example/datamodel", "riq__Entity", out RiqEntity newEntity3)
    .AddEntity("example/datamodel", "riq__Entity", out RiqEntity newEntity4);

// finds all entities of a specified type in the chart
List<RiqEntity> tempoChanges = FindAllEntitiesByType("riq__TempoChange");

// finds all entities with a specific datamodel
// uses a RiqHashedKey representing the datamodel to search for
RiqHashedKey datamodelToSearch = RiqHashedKey.CreateFrom("example/datamodel");
List<RiqEntity> examples = FindAllEntitiesByDatamodel(datamodelToSearch);

// sets a property on an entity
newEntity1.AddProperty("newProperty", 1.0f, out RiqHashedKey newPropertyKey);

// creating properties can be chained
newEntity1.AddProperty("beat", 0f, out RiqHashedKey beatPropertyKey)
.AddProperty("stringProperty", "some string value", out RiqHashedKey stringPropertyKey)
.AddProperty("vector2Property", new Vector2(0,0), out RiqHashedKey vector2PropertyKey);

// reading properties uses generics to specify type
if (newEntity1.TryGetProperty<float>(newPropertyKey, out float propertyValue))
{
    Debug.Log(propertyValue);
}

// ideally RIQ Entities should be processed before playback start into a form better suited for runtime.
```

Read from and create new entries in an RIQ's metadata:
```cs
RiqMetadata metadata = RiqFileHandler.ReadMetadata();

// keys are hashed for faster access
RiqHashedKey hashedKey = metadata.AddEntry("meta1", "someStringData1");

// writing entries can also be chained
metadata.AddEntry("meta2", "someStringData2")
    .AddEntry("meta3", "someStringData3")
    .AddEntry("meta4", "someStringData4");

// reading entries uses generics to specify type
if (metadata.TryGetEntry<string>(hashedKey, out string entryValue))
{
    Debug.Log(entryValue);
}
```

Packing the RIQ cache into an RIQ file on the local filesystem:
```cs
// uses the RiqMetadata and RiqBeatmap objects from above
try
{
    // makes sure all previous modifications are written to the cache
    RiqFileHandler.WriteMetadata(metadata);
    // an RIQ file can contain multiple charts
    // do this for every chart in your RIQ at this stage
    RiqFileHandler.WriteChart(0, beatmap);
    RiqFileHandler.Pack("path/to/riq.riq");
    return;
}
catch (System.Exception e)
{
    statusTxt.text = $"Error packing RIQ: {e.Message}";
    return;
}
```
