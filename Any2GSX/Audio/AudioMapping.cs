﻿using System;
using System.Text.Json.Serialization;

namespace Any2GSX.Audio
{
    public class AudioMapping : IComparable<AudioMapping>
    {
        public virtual string Channel { get; set; }
        public virtual string Device { get; set; }
        public virtual string Binary { get; set; }
        public virtual bool UseLatch { get; set; }

        public AudioMapping() { }

        public AudioMapping(string channel, string device, string binary, bool useLatch = true)
        {
            Channel = channel;
            Device = device;
            Binary = binary;
            UseLatch = useLatch;
        }

        [JsonIgnore]
        public virtual string DeviceName { get => string.IsNullOrWhiteSpace(Device) ? "All" : Device; set { Device = string.IsNullOrWhiteSpace(value) || value == "All" ? "" : value; } }

        public int CompareTo(AudioMapping? other)
        {
            return Channel.CompareTo(other.Channel);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not AudioMapping mapping)
                return false;
            else
                return Channel == mapping.Channel && Device.Equals(mapping.Device) && Binary.Equals(mapping.Binary) && UseLatch == mapping.UseLatch;
        }

        public override int GetHashCode()
        {
            return Channel.GetHashCode() ^ Device.GetHashCode() ^ Binary.GetHashCode() ^ UseLatch.GetHashCode();
        }

        public override string ToString()
        {
            return $"Channel: {Channel} - Binary '{Binary}' @ Device '{DeviceName}' (UseLatch: {UseLatch})";
        }
    }
}
