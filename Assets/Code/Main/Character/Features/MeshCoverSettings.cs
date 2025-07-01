using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    /// <summary>
    /// Used to mark that a piece of clothing hides body features of a specified type.
    /// </summary>
    [DisallowMultipleComponent]
    public class MeshCoverSettings : MonoBehaviour {
        [InfoBox("A cover is something that, when equipped to a character, will hide anything that is not a cover in the same area.\nA hat is a cover. Hairstyle is not. When you equip a hat you hide the hair. \n<b>Both require this script</b>")]
        [SerializeField] bool isCover = false;
        [Title("@" + nameof(CoverTypeLabelText)), LabelText("Cover Area")]
        [SerializeField] CoverType type;

        string CoverTypeLabelText => isCover ? "Will cover these areas" : "Will be hidden by these areas";

        public void CopyFrom(MeshCoverSettings other) {
            type = other.type;
            isCover = other.isCover;
        }

        public CoverType Type {
            get => type;
            set => type = value;
        }

        public bool IsCover => isCover && type != CoverType.None;
        public bool IsBeard => !isCover && type.HasFlagFast(CoverType.Beard);
    }
}