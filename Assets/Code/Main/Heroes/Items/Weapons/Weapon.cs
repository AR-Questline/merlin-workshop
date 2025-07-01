using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    /// <summary>
    /// On weapon data container
    /// </summary>
    public class Weapon : MonoBehaviour {
        [RichEnumExtends(typeof(WeaponType))]
        [SerializeField] RichEnumReference weaponType = null;
        [UnityEngine.Scripting.Preserve] public WeaponType WeaponType => weaponType.EnumAs<WeaponType>();

        [UnityEngine.Scripting.Preserve] public new Collider collider;
        [UnityEngine.Scripting.Preserve] public bool leftHanded = false;
    }
}