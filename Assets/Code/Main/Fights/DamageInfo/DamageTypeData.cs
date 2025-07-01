using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public partial class RuntimeDamageTypeData : DamageTypeDataBase {
        public override ushort TypeForSerialization => SavedTypes.RuntimeDamageTypeData;

        public RuntimeDamageTypeData(DamageType sourceType, DamageSubType subtype = DamageSubType.Default) : base(sourceType, subtype) { }
        public RuntimeDamageTypeData(DamageType sourceType, IEnumerable<DamageTypeDataPart> defaultParts) : base(sourceType, defaultParts) { }
        public RuntimeDamageTypeData(DamageType sourceType, UnsafePinnableList<DamageTypeDataPart> defaultParts) : base(sourceType, defaultParts) { }
    }
    
    public partial class DamageTypeData : DamageTypeDataBase {
        public override ushort TypeForSerialization => SavedTypes.DamageTypeData;

        public DamageTypeData(DamageType sourceType, DamageSubType subtype = DamageSubType.Default) : base(sourceType, subtype) { }
        public DamageTypeData(DamageType sourceType, IEnumerable<DamageTypeDataPart> defaultParts) : base(sourceType, defaultParts) { }
        [UnityEngine.Scripting.Preserve]
        public DamageTypeData(DamageType sourceType, UnsafePinnableList<DamageTypeDataPart> defaultParts) : base(sourceType, defaultParts) { }
        
        public RuntimeDamageTypeData GetRuntimeData() {
            return new RuntimeDamageTypeData(SourceType, Parts);
        }
    }
    
    public abstract partial class DamageTypeDataBase {
        public abstract ushort TypeForSerialization { get; }
        
        [Saved] public DamageType SourceType { get; private set; }
        [Saved] public UnsafePinnableList<DamageTypeDataPart> Parts { get; private set; }

        float _totalMultiplier;
        
        public DamageTypeDataBase(DamageType sourceType, DamageSubType subtype = DamageSubType.Default) {
            this.SourceType = sourceType;
            bool isDefault = subtype == DamageSubType.Default;
            if (isDefault) {
                subtype = sourceType.DefaultSubtype();
            }
            this.Parts = new UnsafePinnableList<DamageTypeDataPart>();
            this.Parts.Add(new DamageTypeDataPart(subtype, 100, isDefault));
        }
        
        public DamageTypeDataBase(DamageType sourceType, IEnumerable<DamageTypeDataPart> defaultParts) {
            this.SourceType = sourceType;
            this.Parts = new UnsafePinnableList<DamageTypeDataPart>();
            foreach (var part in defaultParts) {
                this.Parts.Add(part);
            }
        }
        
        public DamageTypeDataBase(DamageType sourceType, UnsafePinnableList<DamageTypeDataPart> defaultParts) {
            this.SourceType = sourceType;
            this.Parts = new UnsafePinnableList<DamageTypeDataPart>();
            foreach (var part in defaultParts) {
                this.Parts.Add(part);
            }
        }

        /// <summary>
        /// Gets total damage multiplier taking in account all different damage subtypes.
        /// Includes armor calculations and subtypes percentage.
        /// </summary>
        public float CalculateMultiplier(ref Damage damage, IAlive target) {
            float totalDamageTypeMultiplier = 0f;
            
            // Calculate total damage multiplier by adding all subtypes multipliers
            foreach (ref var dataPart in Parts) {
                float armorMultiplier = 1f;
                
                if (damage.CanBeReducedByArmor) {
                    float armorValue = target.TotalArmor(dataPart.SubType);
                    armorValue -= damage.Parameters.ArmorPenetration; //TODO different Armor and Magic Armor penetration?
                    armorMultiplier = armorValue > 0f ? Damage.GetArmorMitigatedMultiplier(armorValue) : 1f;
                }
                
                float damageReceivedMultiplier = damage.DamageReceivedMultiplierData?.GetMultiplierForSubtype(dataPart.SubType) ?? 1f;
                float totalMultiplier = armorMultiplier * damageReceivedMultiplier * dataPart.PercentageAsFloat;
                // Set temporary damage taken for VS events etc.
                dataPart.SetTotalDamageMultiplier(totalMultiplier);
                dataPart.SetDamageTaken(damage.Amount);
                totalDamageTypeMultiplier += totalMultiplier;
            }

            // Set total damage multiplier "normalized" for final calculations
            foreach (ref var subType in Parts) {
                subType.SetTotalDamageMultiplier(subType.TotalDamageMultiplier / totalDamageTypeMultiplier);
            }
                
            _totalMultiplier = totalDamageTypeMultiplier;
            return _totalMultiplier;
        }

        /// <summary>
        /// Adds new subtype, using this will result in exceeding 100% percent.
        /// If subtype already exists, it will just increase it's percentage.
        /// </summary>
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void AddSubType(DamageSubType subType, int percentage) {
            var subTypeDataIndex = Parts.FindIndex(s => s.SubType == subType);
            if (subTypeDataIndex >= 0) {
                ref var tempSubtype = ref Parts[subTypeDataIndex];
                tempSubtype.IncreasePercentage(percentage);
                return;
            }
            Parts.Add(new DamageTypeDataPart(subType, percentage));
        }
        
        /// <summary>
        /// Distributes final damage taken to all subtybes, needed for VS events etc.
        /// </summary>
        public void FinalizeDamage(float totalDamage) {
            foreach (ref var subType in Parts) {
                subType.SetDamageTaken(totalDamage);
            }
            LogDamageTypeInfo(totalDamage);
        }

        void LogDamageTypeInfo(float damage) {
            if (RawDamageData.showCalculationLogs) {
                Log.Important?.Info($"Damage Type Calculations:\nDamage Taken: {damage}, Multiplier: {_totalMultiplier}\nDamageType: {SourceType}\nAll SubTypes:\n - {string.Join("\n - ", Parts.ToArray().Select(s => $"{s.SubType} ({s.Percentage}%): {s.DamageTaken}"))}");
            }
        }
    }

    public partial struct DamageTypeDataPart {
        public ushort TypeForSerialization => SavedTypes.DamageTypeDataPart;

        [Saved] public DamageSubType SubType { get; private set; }
        [Saved] public int Percentage { get; private set; }
        [Saved] public float DamageTaken { get; private set; }
        [Saved] public float TotalDamageMultiplier { get; private set; }
        [Saved] public bool IsDefault { get; private set; } //Is this subType created from the DamageSubType.Default, if yes it can be used in Arrow and Bow combo and replaced with Bow subTypes.

        public float PercentageAsFloat => Percentage / 100f;

        public DamageTypeDataPart(DamageSubType subType, int percentage, bool isDefault = false) {
            this.SubType = subType;
            this.Percentage = percentage;
            this.IsDefault = isDefault;
            DamageTaken = 0;
            TotalDamageMultiplier = 0;
        }

        public void IncreasePercentage(int percentage) {
            this.Percentage += percentage;
        }

        public void SetDamageTaken(float damageTaken) {
            this.DamageTaken = damageTaken * TotalDamageMultiplier;
        }
        
        public void SetTotalDamageMultiplier(float multiplier) {
            TotalDamageMultiplier = multiplier;
        }
    }

    [Serializable]
    public struct DamageTypeDataConfig {
        public DamageSubType subType;
        [DisableIf(nameof(calculatedPercentage)), Range(0, 100)]
        public int percentage;
        [HideInInspector] public bool calculatedPercentage;

        public static DamageTypeDataPart Construct(DamageTypeDataConfig config, DamageType damageType) {
            DamageSubType damageSubType = config.subType;
            bool isDefault = damageSubType == DamageSubType.Default;
            if (isDefault) {
                damageSubType = damageType.DefaultSubtype();
            }
            return new DamageTypeDataPart(damageSubType, config.percentage, isDefault);
        }
    }

    public static class DamageTypeDataUtils {
        [UnityEngine.Scripting.Preserve]
        public static bool HasSubtype(this DamageTypeDataBase data, DamageSubType subType) {
            foreach (var part in data.Parts) {
                if (subType == part.SubType) {
                    return true;
                };
            }
            return false;
        } 
        
        [UnityEngine.Scripting.Preserve]
        public static bool HasSubtypes(this DamageTypeDataBase data, IEnumerable<DamageSubType> subTypes) {
            foreach (var part in data.Parts) {
                if (subTypes.Contains(part.SubType)) {
                    return true;
                };
            }
            return false;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static DamageTypeDataPart GetSubtype(this DamageTypeDataBase data, DamageSubType subType) {
            foreach (var part in data.Parts) {
                if (subType == part.SubType) {
                    return part;
                };
            }
            return default;
        } 
        
        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<DamageTypeDataPart> GetSubtypes(this DamageTypeDataBase data, IEnumerable<DamageSubType> subTypes) {
            List<DamageTypeDataPart> parts = new();
            foreach (var part in data.Parts) {
                if (subTypes.Contains(part.SubType)) {
                    parts.Add(part);
                };
            }
            return parts;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static float GetSubtypeDamage(this DamageTypeDataBase data, DamageSubType subType) {
            foreach (var part in data.Parts) {
                if (subType == part.SubType) {
                    return part.DamageTaken;
                };
            }
            return 0;
        } 
        
        [UnityEngine.Scripting.Preserve]
        public static float GetSubtypesDamage(this DamageTypeDataBase data, IEnumerable<DamageSubType> subTypes) {
            float damageSum = 0;
            foreach (var part in data.Parts) {
                if (subTypes.Contains(part.SubType)) {
                    damageSum += part.DamageTaken;
                };
            }
            return damageSum;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static float GetSubtypePercentage(this DamageTypeDataBase data, DamageSubType subType) {
            foreach (var part in data.Parts) {
                if (subType == part.SubType) {
                    return part.PercentageAsFloat;
                };
            }
            return 0;
        } 
        
        [UnityEngine.Scripting.Preserve]
        public static float GetSubtypesPercentage(this DamageTypeDataBase data, IEnumerable<DamageSubType> subTypes) {
            float percentageSum = 0;
            foreach (var part in data.Parts) {
                if (subTypes.Contains(part.SubType)) {
                    percentageSum += part.PercentageAsFloat;
                };
            }
            return percentageSum;
        }

        public static RuntimeDamageTypeData CombineWeaponAndAmmoType(DamageTypeDataBase weaponDamage, DamageTypeDataBase ammoDamage) {
            RuntimeDamageTypeData newData = new (weaponDamage.SourceType, Array.Empty<DamageTypeDataPart>());
            foreach (var ammoPart in ammoDamage.Parts) {
                if (ammoPart.IsDefault) {
                    foreach (var weaponPart in weaponDamage.Parts) {
                        newData.AddSubType(weaponPart.SubType,  Mathf.CeilToInt(weaponPart.PercentageAsFloat * ammoPart.PercentageAsFloat * 100));
                    }
                    continue;
                }
                newData.AddSubType(ammoPart.SubType, ammoPart.Percentage);
            }
            return newData;
        }
    }
}
