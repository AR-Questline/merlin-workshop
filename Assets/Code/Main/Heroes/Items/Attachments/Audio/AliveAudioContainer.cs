using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using FMODUnity;
using Newtonsoft.Json;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    [Serializable]
    public partial class AliveAudioContainer {
        public ushort TypeForSerialization => SavedTypes.AliveAudioContainer;

        [Saved] public CharacterType audioType;

        [Saved]
        public EventReference idle, hurt, die, attack, specialAttack, specialBegin, specialRelease;
        [Saved] [ShowIf(nameof(IsHumanoid))] public EventReference fall;
        [Saved] [ShowIf(nameof(IsHumanoid))] public EventReference dash;
        [Saved] [ShowIf(nameof(IsAnimal))] public EventReference footStep;
        [Saved] [ShowIf(nameof(IsAnimal))] public EventReference roar;

        bool IsHumanoid => audioType.HasFlagFast(CharacterType.Humanoid);
        bool IsAnimal => audioType.HasFlagFast(CharacterType.Animal);

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();

            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(audioType), audioType);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(idle), idle);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(hurt), hurt);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(die), die);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(attack), attack);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(specialAttack), specialAttack);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(specialBegin), specialBegin);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(specialRelease), specialRelease);
            if (IsHumanoid) {
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(fall), fall);
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(dash), dash);
            }
            if (IsAnimal) {
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(footStep), footStep);
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(roar), roar);
            }

            jsonWriter.WriteEndObject();
        }
        
        [Flags]
        public enum CharacterType {
            Humanoid = 1 << 1,
            Animal = 1 << 2,
            [UnityEngine.Scripting.Preserve] All = int.MaxValue
        }
    }
}