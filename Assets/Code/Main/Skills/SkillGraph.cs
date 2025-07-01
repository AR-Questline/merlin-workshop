using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.EditorOnly;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills {
    public class SkillGraph : Template {
        [SerializeField, Tags(TagsCategory.Skill)]
        string[] tags = new string[0];

        [RichEnumExtends(typeof(Keyword)), SerializeField]
        List<RichEnumReference> keywords = new();

        [ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true, AddressableGroup.Skills)] [SerializeField]
        ShareableSpriteReference icon;

        [SerializeField, ScriptGraphReference] SerializableGuid graphGuid;

        [SerializeField] List<SkillVariable> skillVariables;
        [SerializeField] List<SkillRichEnum> skillEnums;
        [SerializeField] List<SkillAssetReference> skillAssetReferences;
        [SerializeField] List<SkillTemplate> skillTemplates;
        [SerializeField] List<SkillDatum> skillDatums;

        public string[] Tags => tags;
        public Guid VisualScriptGuid => graphGuid.Guid;
        public IEnumerable<Keyword> Keywords => keywords.Select(k => k.EnumAs<Keyword>());
        public ShareableSpriteReference Icon => icon;

        public IEnumerable<SkillVariable> SkillVariables => skillVariables;
        public IEnumerable<SkillRichEnum> SkillEnums => skillEnums;
        public IEnumerable<SkillAssetReference> SkillAssetReferences => skillAssetReferences;
        public IEnumerable<SkillTemplate> SkillTemplates => skillTemplates;
        public IEnumerable<SkillDatum> SkillDatums => skillDatums;

        public IEnumerable<string> AllVariableNames => SkillVariables.Select(v => v.name)
            .Concat(SkillEnums.Select(e => e.name))
            .Concat(SkillAssetReferences.Select(a => a.name))
            .Concat(SkillTemplates.Select(t => t.name))
            .Concat(SkillDatums.Select(s => s.name));

#if UNITY_EDITOR
        public ref SerializableGuid EditorSerializableGuid => ref graphGuid;
        public ScriptGraphAsset EditorAsset {
            get {
                if (graphGuid == Guid.Empty) {
                    return null;
                }
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(graphGuid.Guid.ToString("N"));
                return UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(path);
            }
        }
        
        public void AddTags(IEnumerable<string> tags) {
            this.tags = this.tags.Concat(tags).Distinct().ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    public interface IWithName {
        public string Name { get; }
    }

    [Serializable]
    public partial class SkillVariable : IWithName {
        public ushort TypeForSerialization => SavedTypes.SkillVariable;

        [Saved] public string name;
        [Saved] public float value;

        string IWithName.Name => name;

        public SkillVariable Copy() => new() {
            name = name,
            value = value
        };

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public SkillVariable() { }

        public SkillVariable(string name, float value) {
            this.name = name;
            this.value = value;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(name), name);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(value), value);
            jsonWriter.WriteEndObject();
        }

        bool Equals(SkillVariable other) {
            return name == other.name && value.Equals(other.value);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || (obj is SkillVariable other && Equals(other));
        }

        public override int GetHashCode() {
            unchecked {
                return ((name != null ? name.GetHashCode() : 0) * 397) ^ value.GetHashCode();
            }
        }
    }

    [Serializable]
    public sealed partial class SkillRichEnum : IWithName {
        public ushort TypeForSerialization => SavedTypes.SkillRichEnum;

        [Saved] public string name;

        [RichEnumSearchBox] [RichEnumExtends(typeof(StatType), showOthers: true)]
        public RichEnumReference enumReference;

        [Saved]
        StatType StatType {
            get => enumReference?.EnumAs<StatType>();
            set => enumReference = new RichEnumReference(value);
        }

        string IWithName.Name => name;
        public StatType Value => StatType;

        public SkillRichEnum Copy() => new() {
            name = name,
            enumReference = new RichEnumReference(enumReference.Enum)
        };

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public SkillRichEnum() { }

        public SkillRichEnum(string name, RichEnumReference reference) {
            this.name = name;
            this.enumReference = reference;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(name), name);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(StatType), StatType);
            jsonWriter.WriteEndObject();
        }

        bool Equals(SkillRichEnum other) {
            return name == other.name && Equals(enumReference, other.enumReference);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || (obj is SkillRichEnum other && Equals(other));
        }

        public override int GetHashCode() {
            unchecked {
                return ((name != null ? name.GetHashCode() : 0) * 397) ^ (enumReference != null ? enumReference.GetHashCode() : 0);
            }
        }
    }

    [Serializable]
    public sealed partial class SkillAssetReference : IWithName {
        public ushort TypeForSerialization => SavedTypes.SkillAssetReference;

        [Saved] public string name;

        [Saved, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.Weapons)]
        public ShareableARAssetReference assetReference;

        public ShareableARAssetReference ShareableARAssetReference => assetReference;
        public ARAssetReference ARAssetReference => assetReference.Get();
        string IWithName.Name => name;

        public SkillAssetReference Copy() => new() {
            name = name,
            assetReference = new ShareableARAssetReference(ARAssetReference)
        };

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public SkillAssetReference() { }

        public SkillAssetReference(string name, ShareableARAssetReference reference) {
            this.name = name;
            this.assetReference = reference;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(name), name);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(assetReference), assetReference);
            jsonWriter.WriteEndObject();
        }

        bool Equals(SkillAssetReference other) {
            return name == other.name && Equals(assetReference, other.assetReference);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || (obj is SkillAssetReference other && Equals(other));
        }

        public override int GetHashCode() {
            unchecked {
                return ((name != null ? name.GetHashCode() : 0) * 397) ^ (assetReference != null ? assetReference.GetHashCode() : 0);
            }
        }
    }

    [Serializable]
    public sealed partial class SkillTemplate : IWithName {
        public ushort TypeForSerialization => SavedTypes.SkillTemplate;

        [Saved] public string name;

        [Saved, TemplateType(typeof(ITemplate))]
        public TemplateReference templateReference;

        string IWithName.Name => name;

        public SkillTemplate Copy() => new() {
            name = name,
            templateReference = new TemplateReference(templateReference.GUID)
        };

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public SkillTemplate() { }

        public SkillTemplate(string name, TemplateReference reference) {
            this.name = name;
            this.templateReference = reference;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(name), name);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(templateReference), templateReference);
            jsonWriter.WriteEndObject();
        }

        bool Equals(SkillTemplate other) {
            return name == other.name && Equals(templateReference, other.templateReference);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || (obj is SkillTemplate other && Equals(other));
        }

        public override int GetHashCode() {
            unchecked {
                return ((name != null ? name.GetHashCode() : 0) * 397) ^ (templateReference != null ? templateReference.GetHashCode() : 0);
            }
        }
    }

    [Serializable]
    public partial struct SkillDatum {
        public ushort TypeForSerialization => SavedTypes.SkillDatum;

        [Saved] public string name;
        [Saved] public VSDatumType type;
        [Saved] public VSDatumValue value;

        public SkillDatum(string name, in VSDatumType type, in VSDatumValue value) {
            this.name = name;
            this.type = type;
            this.value = value;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(name), name);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(type), type);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(value), value, type);
            jsonWriter.WriteEndObject();
        }

        public SkillDatum Copy() {
            return new SkillDatum {
                name = name,
                type = type,
                value = value.Copy(type),
            };
        }
    }
}