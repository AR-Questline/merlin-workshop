using Awaken.TG.Main.Heroes.Items.LootTables;

namespace Awaken.TG.Main.Heroes.Items {
    [System.Serializable]
    public class ItemQuantityWithDropChance {
        [UnityEngine.Scripting.Preserve] public ItemSpawningData itemQuantityPair;
        [UnityEngine.Scripting.Preserve] public float dropChance;
    }
}