using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Animations;
using Awaken.Utility.Cameras;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    [UsesPrefab("CharacterSheet/VCharacterSheetUI")]
    public class VCharacterSheetUI : VTabParent<CharacterSheetUI>, IAutoFocusBase {
        [field: SerializeField] public Transform PromptsHost { get; private set; }
        
        [field: Title("Slots")]
        [field: SerializeField] public UISpecificStickerUI Stickers { get; private set; }
        [field: SerializeField] public Transform TooltipParent { get; private set; }
        [field: SerializeField] public Transform StaticTooltip { get; private set; }
        [field: SerializeField] public Transform MapHost { get; private set; }
        [field: SerializeField] public Transform RotatorHost { get; private set; }
        
        [field: Title("Audio")]
        [field: SerializeField] ARFmodEventEmitter SnapshotEmitter { get; set; }
        [field: SerializeField] public EventReference OpenSound { get; set; }
        [field: SerializeField] public EventReference ExitSound { get; set; }
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            Stickers.Init();
            PlayAudio();
        }

        protected override IBackgroundTask OnDiscard() {
            StopAudio();
            return base.OnDiscard();
        }

        void PlayAudio() {
            // SnapshotEmitter.Play();
            if (!OpenSound.IsNull) {
                FMODManager.PlayOneShot(OpenSound);
            }
        }
        
        void StopAudio() {
            //SnapshotEmitter.Stop();
            if (!ExitSound.IsNull) {
                FMODManager.PlayOneShot(ExitSound);
            }
        }
    }
}