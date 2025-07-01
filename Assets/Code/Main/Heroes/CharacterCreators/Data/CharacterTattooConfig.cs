using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [CreateAssetMenu(menuName = "NpcData/BodyFeatures/Tattoo")]
    public class CharacterTattooConfig : ScriptableObject {
        [SerializeField] TattooType type = TattooType.None;
        [SerializeField, TextureAssetReference] ARAssetReference tattoo;
        [SerializeField, TextureAssetReference] ARAssetReference tattooNormal;
        [SerializeField] bool isFaceTattoo;
        
        public TattooType Type => type;
        public ARAssetReference Tattoo => tattoo;
        public ARAssetReference TattooNormal => tattooNormal;
        public bool IsFaceTattoo => isFaceTattoo;
    } 
    
    [Serializable]
    public struct CharacterBodyTattoo {
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel]
        ShareableSpriteReference iconMale;
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel]
        ShareableSpriteReference iconFemale;
        public CharacterTattooConfig data;
        
        public ShareableSpriteReference Icon(Gender gender) => gender == Gender.Female ? iconFemale : iconMale;
    }
    
    [Serializable]
    public struct CharacterFaceTattoo {
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel]
        ShareableSpriteReference iconMale;
        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel]
        ShareableSpriteReference iconFemale;
        public CharacterTattooConfig data;
        
        public ShareableSpriteReference Icon(Gender gender) => gender == Gender.Female ? iconFemale : iconMale;
    }
}