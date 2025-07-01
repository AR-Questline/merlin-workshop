using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Customs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Spawns enemies that need to be killed in time duration for rewards.")]
    [DisallowMultipleComponent]
    public class StagfathersTrialAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] LocString trialTitle;
        [SerializeField] public float trialDuration = 30f;
        [SerializeField] public ARTimeSpan retryAfterFailCooldown;
        
        [FoldoutGroup("Prey"), SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference trialPreySpec;
        [FoldoutGroup("Prey"), SerializeField] public Transform[] spawnPositions = Array.Empty<Transform>();
        
        [FoldoutGroup("Story"), SerializeField] public StoryBookmark startBookmark;
        [FoldoutGroup("Story"), SerializeField] public StoryBookmark failBookmark;
        [FoldoutGroup("Story"), SerializeField] public StoryBookmark completeBookmark;
        [FoldoutGroup("Story"), SerializeField] public StoryBookmark rewardBookmark;
        
        [FoldoutGroup("VFX"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference startEffectVFX;
        [FoldoutGroup("VFX"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference spawnPreyEffectVFX;
        [FoldoutGroup("VFX"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference rewardEffectVFX;

        public string TrialTitle => trialTitle.Translate();
        public LocationTemplate TrialPrey => trialPreySpec.Get<LocationTemplate>();


        public Element SpawnElement() => new StagfathersTrial();

        public bool IsMine(Element element) => element is StagfathersTrial;
    }
}