using Awaken.Utility;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class HeroLocationInvolvement : Element<Location>, IRefreshedByAttachment<HeroInvolvementAttachment> {
        public override ushort TypeForSerialization => SavedModels.HeroLocationInvolvement;

        HeroInvolvementAttachment _spec;
        HeroLocationInteractionInvolvement _heroInvolvement;
        CancellationTokenSource _cancellationTokenSource;

        public void InitFromAttachment(HeroInvolvementAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Location.Events.Interacted, OnInteractionStarted, this);
            ParentModel.ListenTo(Location.Events.InteractionFinished, OnInteractionFinished, this);
        }

        void OnInteractionStarted(LocationInteractionData data) {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _heroInvolvement ??= ParentModel.AddElement(new HeroLocationInteractionInvolvement(ParentModel, _spec.HideWeapons, _spec.HideHands));
        }

        void OnInteractionFinished(LocationInteractionData data) {
            if (_spec.FinishDelay == 0f) {
                _heroInvolvement?.Discard();
                _heroInvolvement = null;
            } else {
                DelayInvolvementDiscard(_spec.FinishDelay).Forget();
            }
        }

        async UniTaskVoid DelayInvolvementDiscard(float time) {
            _cancellationTokenSource = new CancellationTokenSource();
            if (!await AsyncUtil.DelayTime(ParentModel, time, false, _cancellationTokenSource)) {
                return;
            }
            _cancellationTokenSource = null;
            _heroInvolvement?.Discard();
            _heroInvolvement = null;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void ChangeFocusedLocation(Location newFocusedLocation = null) {
            if (_heroInvolvement == null) {
                return;
            }
            _heroInvolvement.ChangeFocusedLocation(newFocusedLocation ?? ParentModel);
        }
    }
}
