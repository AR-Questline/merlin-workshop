using System;
using Awaken.TG.Main.Localization;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    [Serializable]
    public struct ItemLevelData {
        [SerializeField] public int itemLevel;
        [SerializeField, LocStringCategory(Category.Item)] public LocString itemNameAffix;
    }
}