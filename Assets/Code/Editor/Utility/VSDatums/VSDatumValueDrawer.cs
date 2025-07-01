using System;
using Awaken.TG.Editor.Utility.VSDatums.TypeInstances;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Utility.VSDatums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums {
    public static class VSDatumValueDrawer {
        public static void Draw(in Rect rect, SerializedProperty property, in VSDatumType type, ref VSDatumValue value, out bool changed) {
            GetInstance(type).Draw(rect, property, ref value, out changed);
        }
        
        public static void DrawInLayout(SerializedProperty prop, in VSDatumType type, ref VSDatumValue value, out bool changed) {
            GetInstance(type).DrawInLayout(prop, ref value, out changed);
        }
        
        static VSDatumTypeInstanceDrawer GetInstance(in VSDatumType type) {
            return type.general switch {
                VSDatumGeneralType.Bool => VSDatumBoolDrawer.Instance,
                VSDatumGeneralType.Int => VSDatumIntDrawer.Instance,
                VSDatumGeneralType.RichEnum => (VSDatumRichEnumType) type.specific switch {
                    VSDatumRichEnumType.StatusType => VSDatumRichEnumDrawer<StatusType>.Instance,
                    VSDatumRichEnumType.RPGStats => VSDatumRichEnumDrawer<HeroRPGStatType>.Instance,
                    _ => throw new ArgumentOutOfRangeException()
                },
                VSDatumGeneralType.Enum => (VSDatumEnumType) type.specific switch {
                    VSDatumEnumType.DamageType => VSDatumEnumDrawer<DamageType>.Instance,
                    VSDatumEnumType.DamageSubtype => VSDatumEnumDrawer<DamageSubType>.Instance,
                    VSDatumEnumType.TweakPriority => VSDatumEnumDrawer<TweakPriority>.Instance,
                    _ => throw new ArgumentOutOfRangeException()
                },
                VSDatumGeneralType.String => VSDatumStringDrawer.Instance,
                VSDatumGeneralType.Asset => (VSDatumAssetType) type.specific switch {
                    VSDatumAssetType.GameObject => VSDatumAssetDrawer<GameObject>.Instance,
                    _ => throw new ArgumentOutOfRangeException(),
                },
                VSDatumGeneralType.Flag => VSDatumTagsDrawer.FlagsDrawer,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}