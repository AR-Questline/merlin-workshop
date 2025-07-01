using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Cameras;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    [UsesPrefab("CharacterCreator/VCharacterCreator")]
    public class VCharacterCreator : VTabParent<CharacterCreator> {
        [SerializeField] Transform promptHost;
        [Space(5f)] [SerializeField] UIFullscreenRenderer bgRender;
        [SerializeField] TextMeshProUGUI title;

        [SerializeField, BoxGroup("Alive Audio")]
        public AliveAudioContainerWrapper maleAudioContainer;

        [SerializeField, BoxGroup("Alive Audio")]
        public AliveAudioContainerWrapper femaleAudioContainer;

        public UIFullscreenRenderer HeroRender => bgRender;
        public Transform PromptsHost => promptHost;

        public AliveAudioContainer AudioContainer(Gender gender) => gender switch {
            Gender.Male => maleAudioContainer.Data,
            Gender.Female => femaleAudioContainer.Data,
            _ => World.Services.Get<CommonReferences>().AudioConfig.DefaultAliveAudioContainer,
        };

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            bgRender.ChangeCamera(Target.Element<HeroRenderer>().Camera);
            title.text = LocTerms.CharacterCreatorTitle.Translate();
            World.Services.Get<TransitionService>().ToCamera(1).Forget();
        }

        protected override IBackgroundTask OnDiscard() {
            bgRender.Release();
            return base.OnDiscard();
        }
    }
}