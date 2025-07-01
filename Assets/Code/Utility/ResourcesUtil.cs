using Awaken.Utility.Enums;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Utility {
    /// <summary>
    /// Use this paths when loading objects from resources.
    /// Makes it easier to validate if paths are correct.
    /// </summary>
    public class ResourcePath : RichEnum {
        public string Path { get; }
        public string Prefix { get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly ResourcePath 
            ConsumableRogue = new ResourcePath(nameof(ConsumableRogue), "Data/Items/Items_Rogue/Consumables", "ItemConsumable_Rogue_"),
            ConsumableItem = new ResourcePath(nameof(ConsumableItem), "Data/Items/Consumables", "Item"),
            GearItem = new ResourcePath(nameof(GearItem), "Data/Items/Gears", "Item"),
            RuntimeStory = new ResourcePath(nameof(RuntimeStory), "Data/Stories/Runtime", ""),
            CommonStatuses = new ResourcePath(nameof(CommonStatuses), "Data/Statuses/CommonStatuses", "Status"),
            FightStatuses = new ResourcePath(nameof(FightStatuses), "Data/Statuses/FightStatuses", "Status"),
            SpecialStatuses = new ResourcePath(nameof(SpecialStatuses), "Data/Statuses/SpecialStatuses", "Status");

        protected ResourcePath(string enumName, string path, string prefix) : base(enumName) {
            Path = path;
            Prefix = prefix;
        }
    }

    [UnityEngine.Scripting.Preserve]
    public static class ResourcesUtil {
        public static T Load<T>(ResourcePath resourcePath, string name) where T : Object {
            return Resources.Load<T>(ConstructPath(resourcePath, name));
        }

        public static string ConstructPath(ResourcePath resourcePath, string name) {
            return $"{resourcePath.Path}/{resourcePath.Prefix}{name}";
        }
    }
}