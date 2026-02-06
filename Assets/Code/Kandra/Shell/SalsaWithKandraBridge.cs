using System;
using UnityEngine;

namespace Awaken.Kandra {
    public class SalsaWithKandraBridge : MonoBehaviour {
        private SkinnedMeshRenderer bridgeRenderer;
        private KandraRenderer kandraRenderer;
        private BlendshapesRedirect[] blendshapesRedirects;

        public struct BlendshapesRedirect {
            public int sourceIndex;
            public int kandraIndex;

            public override string ToString() {
                throw new NotImplementedException();
            }
        }

        public readonly struct EditorAccess {
            public static ref SkinnedMeshRenderer BridgeRenderer(SalsaWithKandraBridge bridge) =>
                ref bridge.bridgeRenderer;

            public static ref KandraRenderer KandraRenderer(SalsaWithKandraBridge bridge) => ref bridge.kandraRenderer;

            public static ref BlendshapesRedirect[] BlendshapesRedirects(SalsaWithKandraBridge bridge) =>
                ref bridge.blendshapesRedirects;
        }
    }
}