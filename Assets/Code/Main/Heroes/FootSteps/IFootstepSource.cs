using Awaken.TG.Main.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.FootSteps {
    public interface IFootstepSource {
        public void GetSampleData(RaycastHit hit, out Texture2D[] splatmaps, out int[] fmodIndices, out Vector2 uv);
    }
}