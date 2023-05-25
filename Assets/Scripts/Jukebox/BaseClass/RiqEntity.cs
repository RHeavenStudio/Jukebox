using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using FullSerializer;

namespace Jukebox
{
    public class RiqEntity
    {
        public RiqEntityData data;

        public RiqEntity()
        {
            data = new RiqEntityData();
            data.dynamicData = new();
        }

        public RiqEntity(RiqEntity other)
        {
            this.data = other.data;
        }

        public object DeepCopy()
        {
            return new RiqEntity(this);
        }

        public object this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case "beat":
                        return data.beat;
                    case "length":
                        return data.length;
                    case "datamodel":
                        return data.datamodel;
                    default:
                        if (data.dynamicData.ContainsKey(propertyName))
                            return data.dynamicData[propertyName];
                        else
                        {
                            return null;
                        }
                }
            }
            set
            {
                switch (propertyName)
                {
                    case "beat":
                    case "length":
                    case "datamodel":
                        throw new Exception($"Property name {propertyName} is reserved and cannot be set.");
                    default:
                        if (data.dynamicData.ContainsKey(propertyName))
                            data.dynamicData[propertyName] = value;
                        else
                            throw new Exception($"This entity does not have a property named {propertyName}! Attempted to insert value of type {value.GetType()}");
                        break;
                }
            }
        }

        public void CreateProperty(string name, object defaultValue)
        {
            if (!data.dynamicData.ContainsKey(name))
                data.dynamicData.Add(name, defaultValue);
        }
    }
    
    [Serializable]
    public struct RiqEntityData
    {
        public string type;
        public int version;
        public double beat;
        public float length;
        public string datamodel;
        public Dictionary<string, dynamic> dynamicData;
    }
}
