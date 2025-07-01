using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public abstract class NpcAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, TemplateType(typeof(NpcTemplate))] TemplateReference npcTemplate;
        [SerializeField, FoldoutGroup("Story"), ShowIf(nameof(ShowActorProperty)), PropertyOrder(-2)] public ActorRef actor;
        [SerializeField, FoldoutGroup("Story"), TemplateType(typeof(StoryGraph))] TemplateReference storyOnDeath;
        [SerializeField, FoldoutGroup("Visuals"), BoxGroup("Visuals/Mesh"), PrefabAssetReference(AddressableGroup.NPCs)]
        ARAssetReference visualPrefab;
        [SerializeField, FoldoutGroup("Visuals"), BoxGroup("Visuals/Mesh"), PrefabAssetReference(AddressableGroup.NPCs)]
        ShareableARAssetReference simplifiedDeadBodyPrefab;

        [SerializeField, BoxGroup("Visuals/VFX")]
        bool shouldSpawnDeathVfx = true;
        [SerializeField, BoxGroup("Visuals/VFX"), PrefabAssetReference]
        ShareableARAssetReference hitVFXReference;
        [SerializeField, BoxGroup("Visuals/VFX"), PrefabAssetReference]
        ShareableARAssetReference criticalHitVFXReference;
        [SerializeField, BoxGroup("Visuals/VFX"), PrefabAssetReference]
        ShareableARAssetReference backStabHitVFXReference;
        [SerializeField, BoxGroup("Visuals/VFX"), PrefabAssetReference, ShowIf(nameof(shouldSpawnDeathVfx))]
        ShareableARAssetReference deathVFXReference;
        
        [SerializeField, FoldoutGroup("Visuals"), UIAssetReference]
        SpriteReference npcIcon;
        [SerializeField, FoldoutGroup("Visuals"), UIAssetReference]
        ShareableSpriteReference renderIcon;

        /// <summary>
        /// Toggling this will make the NPC appear in the "New" section of the NPC spawner
        /// </summary>
        [field: Tooltip("Toggling this will make the NPC appear in the \"New\" section of the NPC spawner")]
        [field: SerializeField] public bool IsNew { get; private set; }
        public abstract bool IsUnique { get; }
        public NpcTemplate NpcTemplate => npcTemplate.Get<NpcTemplate>();
        public TemplateReference StoryOnDeath => storyOnDeath;
        public ARAssetReference VisualPrefab => visualPrefab;
        public ShareableARAssetReference SimplifiedDeadBodyPrefab => simplifiedDeadBodyPrefab;
        public ShareableARAssetReference HitVFXReference => hitVFXReference;
        public ShareableARAssetReference CriticalHitVFXReference => criticalHitVFXReference;
        public ShareableARAssetReference BackStabHitVFXReference => backStabHitVFXReference;
        public ShareableARAssetReference DeathVFXReference => deathVFXReference;
        public bool ShouldSpawnDeathVfx => shouldSpawnDeathVfx;
        public SpriteReference NpcIcon => npcIcon;
        public ShareableSpriteReference RenderIcon => renderIcon;
        protected virtual bool ShowActorProperty => true; 
        
        public virtual ActorRef GetActor() => actor;
        
        public Element SpawnElement() => new NpcElement();
        public bool IsMine(Element element) => element is NpcElement;
        
        public void Setup(NpcTemplate template, ARAssetReference prefab) {
            npcTemplate = new TemplateReference(template);
            visualPrefab = prefab;
        }
        
        
#if UNITY_EDITOR
        public ARAssetReference EDITOR_visualPrefab {
            get => visualPrefab;
            set => visualPrefab = value;
        }
        public TemplateReference EDITOR_npcTemplate {
            get => npcTemplate;
            set => npcTemplate = value;
        }

        /// <summary>
        /// Sets render icon for the NPC and marks asset as dirty
        /// </summary>
        public void EDITOR_SetRenderIcon(ShareableSpriteReference icon) {
            renderIcon = icon;
            UnityEditor.EditorUtility.SetDirty(this);
        } 

        public virtual ActorRef Editor_GetActorForCache() => GetActor();
        
        /// <summary> calls MergedNpcBaker.Merge </summary>
        [HorizontalGroup("Visuals/Mesh/Buttons"), Button(Name = "Merge")]
        void EDITOR_MergeMeshes() {
            UnityEditor.Selection.activeGameObject = gameObject;
            UnityEditor.EditorApplication.ExecuteMenuItem("TG/Assets/Merged Npc/Merge");
        }
        
        /// <summary> calls MergedNpcBaker.Unmerge </summary>
        [HorizontalGroup("Visuals/Mesh/Buttons"), Button(Name = "Unmerge")]
        void EDITOR_UnmergedMesh() {
            UnityEditor.Selection.activeGameObject = gameObject;
            UnityEditor.EditorApplication.ExecuteMenuItem("TG/Assets/Merged Npc/Unmerge");
        }
#endif
    }
}