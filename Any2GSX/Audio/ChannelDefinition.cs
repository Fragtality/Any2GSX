using System;

namespace Any2GSX.Audio
{
    public class ChannelDefinition : IComparable<ChannelDefinition>, IEquatable<ChannelDefinition>
    {
        public virtual string Name { get; set; }
        public virtual string VolumeVariable { get; set; } = null;
        public virtual string VolumeUnit { get; set; } = "Number";
        public virtual string VolumeStartupCode { get; set; } = null;
        public virtual double MinValue { get; set; } = 0;
        public virtual double MaxValue { get; set; } = 1;
        public virtual string MuteVariable { get; set; } = null;
        public virtual string MuteUnit { get; set; } = "Number";
        public virtual string MuteStartupCode { get; set; } = null;
        public virtual double MutedValue { get; set; } = 0;
        public virtual double UnmutedValue { get; set; } = 1;

        public override string ToString()
        {
            return Name ?? "";
        }

        public virtual string GetInfoString()
        {
            string info = $"{Name,-16}\t";
            if (!string.IsNullOrWhiteSpace(VolumeVariable))
                info = $"{info} [Volume]";
            if (!string.IsNullOrWhiteSpace(MuteVariable))
                info = $"{info} [Mute]";

            return info;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not ChannelDefinition definition)
                return false;
            else
                return Name == definition.Name;
        }

        public int CompareTo(ChannelDefinition? definition)
        {
            if (definition == null)
                return 1;
            else
                return Name.CompareTo(definition.Name);
        }

        public bool Equals(ChannelDefinition? definition)
        {
            if (definition == null)
                return false;
            else
                return Name == definition.Name;
        }
    }
}
