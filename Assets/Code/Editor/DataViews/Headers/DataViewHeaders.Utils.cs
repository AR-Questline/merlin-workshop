using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Editor.DataViews.Data;
using Awaken.TG.Editor.DataViews.Types;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Headers {
    public static partial class DataViewHeaders {
        public const int SmallCellWidth = 80;
        public const int MediumCellWidth = 120;
        public const int BigCellWidth = 240;
        public const int LargeCellWidth = 360;

        static T GetShareableAsset<T>(ShareableARAssetReference reference) where T : class {
            return new ShareableARAssetReference.EDITOR_Accessor(reference).ARReference.EditorLoad<T>();
        }

        static DataViewType GetDataViewTypeOf(Type componentType, string fieldName) {
            var names = fieldName.Split('.');

            var typeIterator = componentType;
            MemberInfo member = null;
            Type type = null;
            for (int i = 0; i < names.Length; i++) {
                type = GetTypeOf(typeIterator, names[i], out member);
                typeIterator = type;
                if (typeIterator == null) {
                    break;
                }
            }
            if (type == null) {
                Log.Important?.Error($"Cannot find type of field {fieldName} in {componentType}");
                return null;
            }

            if (type == typeof(bool)) {
                return DataViewTypeBool.Instance;
            }

            if (type == typeof(int)) {
                return DataViewTypeInt.Instance;
            }

            if (type == typeof(float)) {
                return DataViewTypeFloat.Instance;
            }

            if (type == typeof(string)) {
                return DataViewTypeString.Instance;
            }

            if (type.IsEnum) {
                var genericType = typeof(DataViewTypeEnum<>).MakeGenericType(type);
                var instanceField = genericType.GetField("Instance");
                var instance = instanceField.GetValue(null);
                return instance as DataViewType;
            }

            if (type == typeof(RichEnumReference)) {
                var attribute = member.GetCustomAttribute<RichEnumExtendsAttribute>();
                if (attribute is not { BaseTypes: { Length: 1 } }) {
                    Log.Important?.Error($"Cannot find type of RichEnum {fieldName} in {componentType}");
                    return null;
                }

                var richEnumType = attribute.BaseTypes[0];
                var genericType = typeof(DataViewTypeRichEnum<>).MakeGenericType(richEnumType);
                var instanceField = genericType.GetField("Instance");
                var instance = instanceField.GetValue(null);
                return instance as DataViewType;
            }

            if (typeof(Object).IsAssignableFrom(type)) {
                var genericType = typeof(DataViewTypeObject<>).MakeGenericType(type);
                var instanceField = genericType.GetField("Instance");
                var instance = instanceField.GetValue(null);
                return instance as DataViewType;
            }

            Log.Important?.Error($"Cannot determine type of field {fieldName} in {componentType}");
            return null;

            static Type GetTypeOf(Type componentType, string fieldName, out MemberInfo member) {
                const BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var field = componentType.GetField(fieldName, BindingFlags);
                if (field != null) {
                    member = field;
                    return field.FieldType;
                }

                var property = componentType.GetProperty(fieldName, BindingFlags);
                if (property != null) {
                    member = property;
                    return property.PropertyType;
                }

                member = null;
                return null;
            }
        }

        static DataViewArchetype.HeaderProvider TagsHeaders<TComponent>(Func<IEnumerable<TComponent>> components, GetTags<TComponent> tags) where TComponent : Component {
            return new DataViewArchetype.HeaderProvider(() => components()
                .SelectMany(template => tags(template))
                .Distinct()
                .Select(tag => {
                    return Computable<TComponent>($"tags/{tag}",
                        component => TagUtils.HasRequiredTag(tags(component), tag),
                        (component, has) => {
                            if (has) {
                                ArrayUtils.Add(ref tags(component), tag);
                            } else {
                                ArrayUtils.Remove(ref tags(component), tag);
                            }
                        },
                        SmallCellWidth
                    );
                })
            );
        }

        static DataViewHeader TierProperty<TComponent>(GetTags<TComponent> tags, TierHelper.TierTags tierTags) where TComponent : Component {
            return ComputableEnum<TComponent, TierHelper.Tier>("Tier",
                component => TierHelper.GetTier(tags(component), tierTags),
                (component, tier) => TierHelper.SetTier(ref tags(component), tier, tierTags),
                SmallCellWidth
            );
        }

        static DataViewHeader ItemSkillVariable(string name) {
            return Computable<ItemEffectsSpec>(name, spec => GetVariable(spec, name), (spec, value) => SetVariable(spec, name, value), SmallCellWidth);
        }
        
        static DataViewHeader ItemProjectileSkillVariable(string name) {
            return Computable<ItemProjectileAttachment>(name, spec => GetVariable(spec, name), (spec, value) => SetVariable(spec, name, value), SmallCellWidth);
        }

        static float GetVariable(ItemEffectsSpec spec, string variableName) {
            return (spec == null) ? 0 : GetVariable(spec.Skills, variableName);
        }
        
        static float GetVariable(ItemProjectileAttachment spec, string variableName) {
            return (spec == null) ? 0 : GetVariable(spec.Skills, variableName);
        }

        static float GetVariable(IEnumerable<SkillReference> skills, string variableName) {
            foreach(var skill in skills) {
                foreach (var variable in skill.variables) {
                    if (variable.name == variableName) {
                        return variable.value;
                    }
                }
            }
            return 0;
        }

        static void SetVariable(ItemEffectsSpec spec, string variableName, float value) {
            if (spec == null) {
                return;
            }
            SetVariable(spec.Skills, variableName, value);
        }
        
        static void SetVariable(ItemProjectileAttachment spec, string variableName, float value) {
            if (spec == null) {
                return;
            }
            SetVariable(spec.Skills, variableName, value);
        }
        
        static void SetVariable(IEnumerable<SkillReference> skills, string variableName, float value) {
            foreach(var skill in skills) {
                foreach (var variable in skill.variables) {
                    if (variable.name == variableName) {
                        variable.value = value;
                        return;
                    }
                }
            }
        }

        static DataViewHeader ItemSkillEnum(string name) {
            return ComputableRichEnum<ItemEffectsSpec, StatType>(name, spec => Get(spec, name), (spec, value) => Set(spec, name, value), SmallCellWidth);
            
            StatType Get(ItemEffectsSpec spec, string variableName) {
                if (spec == null) {
                    return null;
                }
                foreach(var skill in spec.Skills) {
                    foreach (var variable in skill.enums) {
                        if (variable.name == variableName) {
                            return variable.enumReference.EnumAs<StatType>();
                        }
                    }
                }
                return null;
            }

            void Set(ItemEffectsSpec spec, string variableName, StatType value) {
                if (spec == null) {
                    return;
                }
                foreach(var skill in spec.Skills) {
                    foreach (var variable in skill.enums) {
                        if (variable.name == variableName) {
                            variable.enumReference = new RichEnumReference(value);
                            return;
                        }
                    }
                }
            }
        }

        static GameObject RetrievePrefabFromItemEquipSpec(ItemEquipSpec itemEquipSpec, Gender gender) {
            return itemEquipSpec.RetrieveMobItemsInstance().FirstOrDefault(i => i.Gender == gender || i.Gender == Gender.None).itemPrefab?.EditorLoad<GameObject>();
        }
        
        static Mesh RetrieveMeshFromItemEquipSpec(ItemEquipSpec itemEquipSpec, Gender gender) {
            var prefab = RetrievePrefabFromItemEquipSpec(itemEquipSpec, gender);
            var kandra = prefab?.GetComponentInChildren<KandraRenderer>()?.rendererData.EDITOR_sourceMesh;
            if (kandra != null) {
                return kandra;
            }
            var skinnedMesh = prefab?.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMesh;
            if (skinnedMesh != null) {
                return skinnedMesh;
            }
            var drake = prefab?.GetComponentInChildren<DrakeLodGroup>()?.Renderers?.FirstOrDefault()?.EDITOR_GetMesh();
            if (drake != null) {
                return drake;
            }
            var mesh = prefab?.GetComponentInChildren<MeshFilter>()?.sharedMesh;
            if (mesh != null) {
                return mesh;
            }
            return null;
        }
        
        delegate ref string[] GetTags<in T>(T component);
    }
}