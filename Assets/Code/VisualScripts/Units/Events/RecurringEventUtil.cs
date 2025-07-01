using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Events {
    public static class RecurringEventUtil {
        public static void Trigger(IMachine target, string name) {
            VGUtils.SendCustomEvent(target, name);
        }

        public static string ID(IMachine target, string name) {
            return $"{((MonoBehaviour) target).GetInstanceID()}_{name}";
        }
    }
}