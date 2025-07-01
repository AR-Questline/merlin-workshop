// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.RichEnums {

    /// <summary>
    /// Reference to rich enum object <see cref="System.Type"/> with support for Unity serialization.
    /// </summary>
    [Serializable]
    public sealed partial class RichEnumReference : ISerializationCallbackReceiver {
        public ushort TypeForSerialization => SavedTypes.RichEnumReference;

        // === Fields

        [SerializeField, Saved] string _enumRef;
        [Saved] RichEnum _enum;

        // === Properties

        /// <summary>
        /// Gets or sets rich enum object
        /// </summary>
        public RichEnum Enum {
            get => _enum;
            set {
                _enum = value;
                _enumRef = GetEnumRef(value);
            }
        }

        public string EnumRef => _enumRef;

        public void SetIfEmpty<T>(T enumValue) where T : RichEnum {
            if (EnumAs<T>() == null) {
                Enum = enumValue;
            }
        }

        // === Static converters

        public static string GetEnumRef(RichEnum richEnum) {
            return richEnum != null ? richEnum.Serialize() : "";
        }

        public static RichEnum GetEnum(string enumRef) {
            return RichEnum.Deserialize(enumRef);
        }

        // === Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RichEnumReference"/> class.
        /// </summary>
        public RichEnumReference() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RichEnumReference"/> class.
        /// </summary>
        public RichEnumReference(string enumRef) {
            Enum = !string.IsNullOrEmpty(enumRef) ? GetEnum(enumRef) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RichEnumReference"/> class.
        /// </summary>
        public RichEnumReference(RichEnum richEnum) {
            Enum = richEnum;
        }

        // === ISerializationCallbackReceiver Members

        public void OnAfterDeserialize() {
            if (!string.IsNullOrEmpty(_enumRef)) {
                _enum = GetEnum(_enumRef);

                if (_enum == null)
                    Log.Important?.Warning($"'{_enumRef}' was referenced but rich enum instance was not found.");
            } else {
                _enum = null;
            }
        }


        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        // === Conversions

        public T EnumAs<T>() where T : RichEnum => Enum as T;

        public static implicit operator string(RichEnumReference enumReference) {
            return enumReference._enumRef;
        }

        public static implicit operator RichEnum(RichEnumReference enumReference) {
            return enumReference.Enum;
        }

        public static implicit operator RichEnumReference(RichEnum richEnum) {
            return new RichEnumReference(richEnum);
        }

        public static bool operator ==(RichEnumReference richEnumRef, RichEnum richEnum) {
            return richEnumRef?.Enum == richEnum;
        }

        public static bool operator !=(RichEnumReference richEnumRef, RichEnum richEnum) {
            return !(richEnumRef == richEnum);
        }
        
        // === Equality overrides
        public bool Equals(RichEnumReference other) {
            return _enumRef == other._enumRef && Equals(_enum, other._enum);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || obj is RichEnumReference other && Equals(other);
        }

        public override int GetHashCode() {
            return _enumRef != null ? _enumRef.GetHashCode() : 0;
        }

        public override string ToString() {
            return Enum != null ? Enum.Serialize() : "(None)";
        }
        public static SerializationAccessor Serialization(RichEnumReference instance) => new(instance);

        public struct SerializationAccessor {
            RichEnumReference _instance;

            public SerializationAccessor(RichEnumReference instance) {
                _instance = instance;
            }

            public ref string EnumRef => ref _instance._enumRef;
        }
    }
}