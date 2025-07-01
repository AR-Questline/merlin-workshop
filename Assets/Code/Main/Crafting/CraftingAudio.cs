using System;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Crafting {
    [Serializable]
    public struct CraftingAudio {
        [SerializeField] EventReference createHoldSound;
        [SerializeField] EventReference craftingResultRegular;
        [SerializeField] EventReference craftingResultGreat;
        [SerializeField] EventReference craftingResultPoor;

        public EventReference GetResultSound(CraftingResultQuality quality) {
            return quality switch {
                CraftingResultQuality.Poor => craftingResultPoor,
                CraftingResultQuality.Great => craftingResultGreat,
                _ => craftingResultRegular
            };
        }
        
        public EventReference CreateHoldSound => createHoldSound;
        public EventReference CraftingResultRegular => craftingResultRegular;
        public EventReference CraftingResultGreat => craftingResultGreat;
        public EventReference CraftingResultPoor => craftingResultPoor;
    }
}