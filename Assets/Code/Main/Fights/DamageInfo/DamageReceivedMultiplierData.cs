using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public class RuntimeDamageReceivedMultiplierData : DamageReceivedMultiplierDataBase {
        [UnityEngine.Scripting.Preserve]
        public RuntimeDamageReceivedMultiplierData(DamageSubType subtype = DamageSubType.Default) : base(subtype) { }
        [UnityEngine.Scripting.Preserve]
        public RuntimeDamageReceivedMultiplierData(IEnumerable<DamageReceivedMultiplierDataPart> defaultSubtypes) : base(defaultSubtypes) { }
        public RuntimeDamageReceivedMultiplierData(UnsafePinnableList<DamageReceivedMultiplierDataPart> defaultSubtypes) : base(defaultSubtypes) { }
    }
    
    public class DamageReceivedMultiplierData : DamageReceivedMultiplierDataBase {
        [UnityEngine.Scripting.Preserve]
        public DamageReceivedMultiplierData(DamageSubType subtype = DamageSubType.Default) : base(subtype) { }
        public DamageReceivedMultiplierData(IEnumerable<DamageReceivedMultiplierDataPart> defaultSubtypes) : base(defaultSubtypes) { }
        [UnityEngine.Scripting.Preserve]
        public DamageReceivedMultiplierData(UnsafePinnableList<DamageReceivedMultiplierDataPart> defaultSubtypes) : base(defaultSubtypes) { }
        
        public RuntimeDamageReceivedMultiplierData GetRuntimeData() {
            return new RuntimeDamageReceivedMultiplierData(Parts);
        }
    }
    
    public abstract class DamageReceivedMultiplierDataBase {
        public UnsafePinnableList<DamageReceivedMultiplierDataPart> Parts { get; private set; }

        public DamageReceivedMultiplierDataBase(DamageSubType subtype = DamageSubType.Default) {
            this.Parts = new UnsafePinnableList<DamageReceivedMultiplierDataPart>();
            this.Parts.Add(new DamageReceivedMultiplierDataPart(subtype, 100));
        }
        
        public DamageReceivedMultiplierDataBase(IEnumerable<DamageReceivedMultiplierDataPart> defaultSubtypes) {
            this.Parts = new UnsafePinnableList<DamageReceivedMultiplierDataPart>();
            foreach (var subType in defaultSubtypes) {
                this.Parts.Add(subType);
            }
        }
        
        public DamageReceivedMultiplierDataBase(UnsafePinnableList<DamageReceivedMultiplierDataPart> defaultSubtypes) {
            this.Parts = new UnsafePinnableList<DamageReceivedMultiplierDataPart>();
            foreach (var subType in defaultSubtypes) {
                this.Parts.Add(subType);
            }
        }
        
        public float GetMultiplierForSubtype(DamageSubType subTypeToCheck) {
            float multiplier = 1f;
            
            foreach (ref var dataPart in Parts) {
                var subType = dataPart.SubType;
                if (subType == subTypeToCheck) {
                    multiplier *= dataPart.Multiplier;
                    continue;
                }
                switch (subType) {
                    case DamageSubType.GenericPhysical: {
                        if (subTypeToCheck.IsPhysical()) {
                            multiplier *= dataPart.Multiplier;
                        }
                        continue;
                    }
                    case DamageSubType.GenericMagical: {
                        if (subTypeToCheck.IsMagical()) {
                            multiplier *= dataPart.Multiplier;
                        }
                        continue;
                    }
                }
            }

            LogDamageReceivedMultiplierInfo(subTypeToCheck, multiplier);
            return multiplier;
        }

        /// <summary>
        /// Adds new subtype, using this will result in exceeding 100% percent.
        /// If subtype already exists, it will just multiply it's value.
        /// </summary>
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void AddSubType(DamageSubType subType, float multiplier) {
            var subTypeDataIndex = Parts.FindIndex(s => s.SubType == subType);
            if (subTypeDataIndex >= 0) {
                ref var tempSubtype = ref Parts[subTypeDataIndex];
                tempSubtype.MultiplyMultiplier(multiplier);
                return;
            }
            Parts.Add(new DamageReceivedMultiplierDataPart(subType, multiplier));
        }
        
        void LogDamageReceivedMultiplierInfo(DamageSubType subType, float multiplier) {
            if (RawDamageData.showCalculationLogs) {
                Log.Important?.Info($"Damage Received Multiplier:\nDamage Sub Type: {subType}, Multiplier: {multiplier * 100}%\nAll Multipliers:\n - {string.Join("\n - ", Parts.ToArray().Select(s => $"{s.SubType}: {s.Multiplier*100}%"))}");
            }
        }
    }

    public struct DamageReceivedMultiplierDataPart {
        public DamageSubType SubType { get; }
        public float Multiplier { get; private set; }

        public DamageReceivedMultiplierDataPart(DamageSubType subType, float multiplier) {
            this.SubType = subType;
            this.Multiplier = multiplier;
        }

        [UnityEngine.Scripting.Preserve]
        public void AddMultiplier(float multiplier) {
            this.Multiplier += multiplier;
        }

        public void MultiplyMultiplier(float multiplier) {
            this.Multiplier *= multiplier;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void SetMultiplier(float multiplier) {
            this.Multiplier = multiplier;
        }
    }

    [Serializable]
    public struct DamageReceivedMultiplierDataConfig {
        public DamageSubType subType;
        [Range(0, 1000)]
        public int takenDamagePercentageMultiplier;

        public static DamageReceivedMultiplierDataConfig Default => new DamageReceivedMultiplierDataConfig() {
            subType = DamageSubType.GenericPhysical,
            takenDamagePercentageMultiplier = 100
        };
        
        public static DamageReceivedMultiplierDataPart Construct(DamageReceivedMultiplierDataConfig config) {
            return new DamageReceivedMultiplierDataPart(config.subType, (float) config.takenDamagePercentageMultiplier / 100);
        }
    }

    public static class DamageReceivedMultiplierDataUtils {
        public static RuntimeDamageReceivedMultiplierData GetRuntimeDamageReceivedMultiplierData(this IAlive alive) {
            return alive.AliveStats.DamageReceivedMultiplierData?.GetRuntimeData();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static float GetDamageReceivedMultiplierForSubtype(this IAlive alive, DamageSubType subTypeToCheck) {
            return alive.AliveStats.DamageReceivedMultiplierData?.GetMultiplierForSubtype(subTypeToCheck) ?? 1f;
        }
    }
}
