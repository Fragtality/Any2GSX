using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Any2GSX.CommBus
{
    public enum ClientDataId
    {
        REQUEST = 401,
    }

    public struct ModuleMessage
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)ModuleCommBus.AREA_SIZE)]
        public String Data;

        public static implicit operator string(ModuleMessage msg)
        {
            return msg.ToString();
        }

        public override readonly string ToString()
        {
            return Data ?? "";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is string value)
                return Data?.Equals(value) == true;
            else
                return false;
        }

        public static bool operator ==(ModuleMessage left, ModuleMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ModuleMessage left, ModuleMessage right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(ModuleMessage left, string right)
        {
            return left.Data?.Equals(right) == true;
        }

        public static bool operator !=(ModuleMessage left, string right)
        {
            return left.Data?.Equals(right) == false;
        }

        public override readonly int GetHashCode()
        {
            return Data?.GetHashCode() ?? 0;
        }
    }
}
