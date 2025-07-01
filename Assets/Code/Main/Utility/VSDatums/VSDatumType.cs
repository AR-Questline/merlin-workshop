using Awaken.Utility;
using System;
using System.Runtime.InteropServices;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.VSDatums.TypeInstances;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VSDatums {
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public partial struct VSDatumType {
        public ushort TypeForSerialization => SavedTypes.VSDatumType;

        [FieldOffset(0), NonSerialized] public readonly VSDatumGeneralType general;
        [FieldOffset(1), NonSerialized] public readonly byte specific;
        
        [FieldOffset(0), SerializeField, Saved] ushort id;

        public VSDatumType(VSDatumGeneralType general, byte specific) : this() {
            this.general = general;
            this.specific = specific;
        }

        public readonly bool Equals(in VSDatumType other) {
            return id == other.id;
        }

        public new readonly Type GetType() => GetInstance().GetDatumType();
        public readonly object GetValue(in VSDatumValue value) => GetInstance().GetBoxedDatumValue(value);
        
        public readonly VSDatumTypeInstance GetInstance() {
            return general switch {
                VSDatumGeneralType.Bool => VSDatumTypeInstanceBool.Instance,
                VSDatumGeneralType.Int => VSDatumTypeInstanceInt.Instance,
                VSDatumGeneralType.RichEnum => (VSDatumRichEnumType) specific switch {
                    VSDatumRichEnumType.StatusType => VSDatumTypeInstanceRichEnum<StatusType>.Instance,
                    VSDatumRichEnumType.RPGStats => VSDatumTypeInstanceRichEnum<HeroRPGStatType>.Instance,
                    _ => throw new ArgumentOutOfRangeException()
                },
                VSDatumGeneralType.Enum => (VSDatumEnumType) specific switch {
                    VSDatumEnumType.DamageType => VSDatumTypeInstanceEnum<DamageType>.Instance,
                    VSDatumEnumType.DamageSubtype => VSDatumTypeInstanceEnum<DamageSubType>.Instance,
                    VSDatumEnumType.TweakPriority => VSDatumTypeInstanceEnum<TweakPriority>.Instance,
                    _ => throw new ArgumentOutOfRangeException()
                },
                VSDatumGeneralType.String => VSDatumTypeInstanceString.Instance,
                VSDatumGeneralType.Asset => VSDatumTypeInstanceAsset.Instance,
                VSDatumGeneralType.Flag => VSDatumTypeInstanceString.Instance,
                _ => throw new ArgumentOutOfRangeException()
            };
        }


        public readonly void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(id), id);
            jsonWriter.WriteEndObject();
        }
    }

    // if you add any entry to enums below you need to address it in:
    // - VSDatumType.GetInstance
    // - VSDatumValueDrawer.GetInstance
    // - VSDatumTypeDrawer.GetAll
    
    public enum VSDatumGeneralType : byte {
        Bool = 0,
        Int = 1,
        RichEnum = 2,
        Enum = 3,
        String = 4,
        Asset = 5,
        Flag = 6,
    }

    public enum VSDatumEnumType : byte {
        DamageType = 0,
        DamageSubtype = 1,
        TweakPriority = 2
    }

    public enum VSDatumRichEnumType : byte {
        StatusType = 0,
        RPGStats = 1,
    }

    public enum VSDatumAssetType : byte {
        GameObject = 0,
    }
}