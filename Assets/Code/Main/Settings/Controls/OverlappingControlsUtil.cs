using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Settings.Controls {
    public static class OverlappingControlsUtil {
        static readonly List<KeyBindings[]> AllowedTogetherSets = new() {
            new KeyBindings[] { KeyBindings.Gameplay.Horizontal, KeyBindings.Gameplay.MountHorizontal, KeyBindings.Minigames.LockOpenAxis },
            new KeyBindings[] { KeyBindings.Gameplay.Vertical, KeyBindings.Gameplay.MountVertical, KeyBindings.Minigames.LockOpenAxis },
            new KeyBindings[] { KeyBindings.Gameplay.Interact, KeyBindings.Gameplay.Dismount, KeyBindings.UI.Items.TakeItem, KeyBindings.UI.Generic.Accept },
            new KeyBindings[] { KeyBindings.UI.Items.TransferItems, KeyBindings.UI.Items.SortItems, KeyBindings.UI.Generic.SecondaryAction }
        };

        static readonly List<KeyBindings[]> MergedTogether = new() {
            new KeyBindings[] { KeyBindings.Gameplay.Horizontal, KeyBindings.Gameplay.MountHorizontal, KeyBindings.Minigames.LockOpenAxis },
            new KeyBindings[] { KeyBindings.Gameplay.Vertical, KeyBindings.Gameplay.MountVertical },
            new KeyBindings[] { KeyBindings.Gameplay.Interact, KeyBindings.Gameplay.Dismount, KeyBindings.UI.Items.TakeItem },
            new KeyBindings[] { KeyBindings.Gameplay.Attack, KeyBindings.Gameplay.AttackHeavy}
        };

        public static bool AllowedTogether(VKeyBinding a, VKeyBinding b) {
            if (a.Pole != b.Pole) return false;
            return AllowedTogetherSets.Any(array => ArrayContains(array, a.ActionName) && ArrayContains(array, b.ActionName));
        }

        public static IEnumerable<string> GetMergeGroup(string actionName) {
            KeyBindings[] group = MergedTogether.FirstOrDefault(array => ArrayContains(array, actionName));
            if (group == null) {
                yield return actionName;
            } else {
                foreach (var action in group) {
                    yield return action;
                }
            }
        }

        public static bool IsElementOfMergeGroup(string actionName) {
            KeyBindings[] group = MergedTogether.FirstOrDefault(array => ArrayContains(array, actionName));
            return group != null && group[0] != actionName;
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsMerged(string actionName, out KeyBindings usedBinding) {
            usedBinding = MergedTogether.FirstOrDefault(array => ArrayContains(array, actionName))?[0];
            return usedBinding != null;
        }

        static bool ArrayContains(KeyBindings[] array, string action) {
            return array.Any(binding => binding == action);
        }
    }
}