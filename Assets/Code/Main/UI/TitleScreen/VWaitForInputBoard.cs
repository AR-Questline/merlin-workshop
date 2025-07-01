using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    /// <summary>
    /// Useful for displaying some info to the player after loading a scene and before letting him play
    /// </summary>
    public class VWaitForInputBoard : View<WaitForInputBoard>, IPromptHost {
        [BoxGroup("Text"), SerializeField] TextMeshProUGUI infoText;
        [BoxGroup("Text"), SerializeField, LocStringCategory(Category.UI)] LocString infoLocString;

        [BoxGroup("Video"), SerializeField] public LoadingHandle videoHandle;
        [BoxGroup("Video"), SerializeField] Video.FadeOutOptions fadeOutOptions;

        [BoxGroup("References"), SerializeField] Transform promptsHost;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public Transform PromptsHost => promptsHost;

        protected override void OnMount() {
            if (videoHandle.IsSet) {
                ShowVideo();
            } else {
                ShowText();
            }
        }

        void ShowVideo() {
            var video = World.Add(Video.FullScreen(videoHandle, Target, fadeOut: fadeOutOptions));
            video.ListenTo(Model.Events.AfterDiscarded, Target.Discard, Target);
        }

        void ShowText() {
            infoText.text = infoLocString;
            var prompts = new Prompts(this);
            Target.AddElement(prompts);
            prompts.AddPrompt(Prompt.Hold(KeyBindings.UI.Items.SelectItem, LocTerms.Accept.Translate(), HandleHold), Target);
        }

        void HandleHold() {
            Target.Discard();
        }

        protected override IBackgroundTask OnDiscard() {
            return new BackgroundUniTask(AsyncUtil.DelayFrame(this));
        }
    }
}