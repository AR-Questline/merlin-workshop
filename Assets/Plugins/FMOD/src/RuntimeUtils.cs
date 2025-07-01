using System;

namespace FMOD
{
    [Serializable]
    public struct GUID : IEquatable<GUID>
    {
        public int Data1;
        public int Data2;
        public int Data3;
        public int Data4;

        public bool IsNull => Data1 == 0 && Data2 == 0 && Data3 == 0 && Data4 == 0;

        public bool Equals(GUID other)
        {
            return Data1 == other.Data1 && Data2 == other.Data2 && Data3 == other.Data3 && Data4 == other.Data4;
        }

        public override bool Equals(object obj)
        {
            return obj is GUID other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Data1;
                hashCode = (hashCode * 397) ^ Data2;
                hashCode = (hashCode * 397) ^ Data3;
                hashCode = (hashCode * 397) ^ Data4;
                return hashCode;
            }
        }

        public static bool operator ==(GUID left, GUID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GUID left, GUID right)
        {
            return !left.Equals(right);
        }

        public static GUID Parse(string guid) => default;
    }
    
    public enum EmitterGameEvent : int
    {
        None,
        ObjectStart,
        ObjectDestroy,
        TriggerEnter,
        TriggerExit,
        TriggerEnter2D,
        TriggerExit2D,
        CollisionEnter,
        CollisionExit,
        CollisionEnter2D,
        CollisionExit2D,
        ObjectEnable,
        ObjectDisable,
        ObjectMouseEnter,
        ObjectMouseExit,
        ObjectMouseDown,
        ObjectMouseUp,
        UIMouseEnter,
        UIMouseExit,
        UIMouseDown,
        UIMouseUp,
    }
}