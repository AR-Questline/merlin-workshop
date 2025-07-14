using System;
using System.Runtime.InteropServices;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility {
    [StructLayout(LayoutKind.Explicit), Serializable]
    public partial struct SerializableGuid : IComparable<SerializableGuid>, IComparable<Guid>, IEquatable<SerializableGuid>, IEquatable<Guid>, IFormattable {
        public ushort TypeForSerialization => SavedTypes.SerializableGuid;

        public static readonly SerializableGuid Empty = new();
        
        [FieldOffset(0), Saved] 
        public Guid Guid;

        [FoldoutGroup("@GUIDString", GroupID = "Parts")]
        [FieldOffset(00), SerializeField, HorizontalGroup("Parts/hor"), HideLabel] int _guidPart1;
        [FieldOffset(04), SerializeField, HorizontalGroup("Parts/hor"), HideLabel] int _guidPart2;
        [FieldOffset(08), SerializeField, HorizontalGroup("Parts/hor"), HideLabel] int _guidPart3;
        [FieldOffset(12), SerializeField, HorizontalGroup("Parts/hor"), HideLabel] int _guidPart4;

        // ReSharper disable once UnusedMember.Local
        readonly string GUIDString => Guid.ToString();

        public SerializableGuid(Guid guid) {
            _guidPart1 = 0;
            _guidPart2 = 0;
            _guidPart3 = 0;
            _guidPart4 = 0;
            Guid = guid;
        }
        
        public SerializableGuid(int part1, int part2, int part3, int part4) {
            Guid = Guid.Empty;
            _guidPart1 = part1;
            _guidPart2 = part2;
            _guidPart3 = part3;
            _guidPart4 = part4;
        }

        public SerializableGuid(string guidString) : this(Guid.Parse(guidString)) { }

        public static SerializableGuid NewGuid() {
            return new SerializableGuid(Guid.NewGuid());
        }

        public static implicit operator Guid(SerializableGuid uGuid) {
            return uGuid.Guid;
        }

        public readonly int CompareTo(SerializableGuid other) {
            return Guid.CompareTo(other.Guid);
        }

        public readonly int CompareTo(Guid other) {
            return Guid.CompareTo(other);
        }

        public readonly int CompareTo(object obj) {
            return obj switch {
                SerializableGuid serializableGuid => Guid.CompareTo(serializableGuid.Guid),
                Guid guid => Guid.CompareTo(guid),
                _ => -1
            };
        }

        public readonly bool Equals(SerializableGuid other) {
            return Guid == other;
        }

        public readonly bool Equals(Guid other) {
            return Guid == other;
        }

        public readonly override bool Equals(object obj) {
            return obj switch {
                SerializableGuid serializableGuid => Guid == serializableGuid.Guid,
                Guid guid => Guid == guid,
                _ => false
            };
        }

        public readonly override int GetHashCode() {
            return Guid.GetHashCode();
        }

        public static bool operator ==(SerializableGuid a, SerializableGuid b) {
            return a.Equals(b);
        }

        public static bool operator !=(SerializableGuid a, SerializableGuid b) {
            return !a.Equals(b);
        }

        public readonly override string ToString() {
            return Guid.ToString();
        }

        public readonly string ToString(string format) {
            return Guid.ToString(format);
        }

        public readonly string ToString(string format, IFormatProvider formatProvider) {
            return Guid.ToString(format, formatProvider);
        }
    }
}