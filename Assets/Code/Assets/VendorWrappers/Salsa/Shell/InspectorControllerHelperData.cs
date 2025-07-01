using System.Collections.Generic;
using Awaken.VendorWrappers.Salsa;
using UnityEngine;

namespace CrazyMinnow.SALSA
{
    public class InspectorControllerHelperData
    {
        public Transform bone;
        public TformBase baseTform;
        public TformBase startTform;
        public TformBase endTform;
        public bool fracPos = true;
        public bool fracRot = true;
        public bool fracScl = true;
        public bool inspIsSetStart;
        public bool inspIsSetEnd;
        public UmaUepProxy umaUepProxy;
        public float uepAmount;
        public SkinnedMeshRenderer smr;
        public int blendIndex;
        public float minShape;
        public float maxShape = 1f;
        public EyeGizmo eyeGizmo;
        public GameObject eventSender;
        public Animator animator;
        public bool isTriggerParameterBiDirectional;
        public string eventIdentityName;
        public Switcher.OnState onState = Switcher.OnState.OnUntilOff;
        public bool display2dImage;
        public bool isRestNull;
        public SpriteRenderer spriteRenderer;
        public List<Sprite> sprites;
        public Renderer textureRenderer;
        public int materialIndex;
        public List<Texture> backupTextures;
        public List<Texture> textures;
        public Renderer materialRenderer;
        public List<Material> materials;
    }
}