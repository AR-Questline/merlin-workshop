using UnityEngine;

namespace Awaken.TG.Assets
{
    public interface IIconized {
        public ShareableSpriteReference GetIconReference();
        public void SetIconReference(ShareableSpriteReference iconReference);
        public GameObject InstantiateProp(Transform parent);
    }
}
