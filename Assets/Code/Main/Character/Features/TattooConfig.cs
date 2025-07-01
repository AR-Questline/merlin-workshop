using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    [Serializable]
    public partial struct TattooConfig {
        public ushort TypeForSerialization => SavedTypes.TattooConfig;

        [Saved] public TattooType type;
        
        [Space]
        [Saved] public Color colorA;
        [Saved] public Color colorB;
        [Saved] public Color colorC;
        [Saved] public Color colorD;
        
        [Space]
        [Saved, Range(0, 10)] public float emissiveA;
        [Saved, Range(0, 10)] public float emissiveB;
        [Saved, Range(0, 10)] public float emissiveC;
        [Saved, Range(0, 10)] public float emissiveD;

        [SerializeField, TextureAssetReference, Saved] ARAssetReference face;
        [SerializeField, TextureAssetReference, Saved] ARAssetReference faceNormal;
        [SerializeField, TextureAssetReference, Saved] ARAssetReference torso;
        [SerializeField, TextureAssetReference, Saved] ARAssetReference torsoNormal;

        public ARAssetReference Face => face;
        public ARAssetReference Torso => torso;
        public ARAssetReference FaceNormal => faceNormal;
        public ARAssetReference TorsoNormal => torsoNormal;

        public TattooConfig(CharacterTattooConfig config, Color color) {
            type = config.Type;

            if (config.IsFaceTattoo) {
                face = config.Tattoo.DeepCopy();
                faceNormal = config.TattooNormal.DeepCopy();
                torso = null;
                torsoNormal = null;
            } else {
                torso = config.Tattoo.DeepCopy();
                torsoNormal = config.TattooNormal.DeepCopy();
                face = null;
                faceNormal = null;
            }

            colorA = ARColor.Transparent;
            colorB = ARColor.Transparent;
            colorC = color;
            colorD = ARColor.Transparent;

            emissiveA = 0;
            emissiveB = 0;
            emissiveC = 0;
            emissiveD = 0;
        }
        
        public TattooConfig Copy() {
            return new TattooConfig {
                type = type,
                face = face?.DeepCopy(),
                torso = torso?.DeepCopy(),
                faceNormal = faceNormal?.DeepCopy(),
                torsoNormal = torsoNormal?.DeepCopy(),
                colorA = colorA,
                colorB = colorB,
                colorC = colorC,
                colorD = colorD,
                emissiveA = emissiveA,
                emissiveB = emissiveB,
                emissiveC = emissiveC,
                emissiveD = emissiveD
            };
        }
    }
}