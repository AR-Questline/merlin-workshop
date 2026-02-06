using Awaken.Utility;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LocationWithNpcInteractionElement : Element<Location>, IRefreshedByAttachment<LocationWithNpcInteractionAttachment>, IHeroActionBlocker {
        public override ushort TypeForSerialization => SavedModels.LocationWithNpcInteractionElement;

        const float RealTimeInteractionDelay = 5f; //seconds
        
        LocationWithNpcInteractionAttachment _spec;
        CancellationTokenSource _cancellationTokenSource;
        
        public void InitFromAttachment(LocationWithNpcInteractionAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Location.Events.Interacted, OnHeroActionStarted, this);
            ParentModel.ListenTo(Location.Events.InteractionFinished, OnHeroActionFinished, this);
            foreach (var interaction in _spec.Interactions) {
                if (interaction == null) {
                    Log.Important?.Error($"{nameof(LocationWithNpcInteractionElement)} has empty interaction on the list: {LogUtils.GetDebugName(this)}");
                }
            }
        }

        void OnHeroActionStarted(LocationInteractionData data) {
            if (_spec == null) {
                Log.Important?.Error($"{nameof(LocationWithNpcInteractionElement)} has null _spec on {(HasBeenDiscarded ? "discarded" : LogUtils.GetDebugName(this))}");
                _cancellationTokenSource?.Cancel();
                return;
            }
            foreach (var interaction in _spec.Interactions) {
                if (interaction == null) {
                    continue;
                }
                interaction.Book(Hero.Current);
            }
            _cancellationTokenSource?.Cancel();
        }
        
        void OnHeroActionFinished(LocationInteractionData data) {
            _cancellationTokenSource = new CancellationTokenSource();
            RealTimeWait().Forget();
        }

        async UniTaskVoid RealTimeWait() {
            if (await AsyncUtil.DelayTime(this, RealTimeInteractionDelay, source: _cancellationTokenSource)) {
                if (_spec == null) {
                    return;
                }
                foreach (var interaction in _spec.Interactions) {
                    if (interaction == null) {
                        continue;
                    }
                    interaction.Unbook(Hero.Current);
                }
            }
        }

        public bool IsBlocked(Hero hero, IInteractableWithHero interactable) {
            return _spec.Interactions.Any(i => i != null && !i.AvailableFor(Hero.Current));
        }
    }
}
