using Awaken.TG.Assets;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
     public partial class HallucinationRewardStatusLinkLifetime : Element<Location>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.HallucinationRewardStatusLinkLifetime;

        [Saved] StatusTemplate _statusTemplate;
        [Saved] ShareableARAssetReference _rewardHideVfx;
        WeakModelRef<Status> _status;
        NpcAI _ai; 
        
        public HallucinationRewardStatusLinkLifetime(Status status, StatusTemplate statusTemplate, ShareableARAssetReference rewardHideVfx) {
            _status = status;
            _statusTemplate = statusTemplate;
            _rewardHideVfx = rewardHideVfx;
        }

        protected override void OnFullyInitialized() {
            if (_status is { IsSet: false }) {
                if (Hero.Current.Statuses.TryGetStatus(_statusTemplate, out var status)) {
                    _status = status;
                }
            }
            
            var npc = ParentModel.TryGetElement<NpcElement>();
            if (npc != null) {
                if (!_status.TryGet(out var status)) {
                    npc.Destroy();
                    return;
                }
                status.ListenTo(Model.Events.BeforeDiscarded, OnStatusBeforeDiscarded, this);
                npc.OnCompletelyInitialized(_ => {
                    _ai = npc.NpcAI;
                    _ai.ReceiveHostileAction(Hero.Current, null, DamageType.None);
                    World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
                });
                return;
            }
            ParentModel.Discard();
        }
        
        public void UnityUpdate() {
            if (_ai is { InFlee: false, HasBeenDiscarded: false, NpcElement: { IsAlive: true } }) {
                _ai.ReceiveHostileAction(Hero.Current, null, DamageType.None);
            }
        }

        void OnStatusBeforeDiscarded() {
            if (_rewardHideVfx is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(_rewardHideVfx, ParentModel.Coords, ParentModel.Rotation).Forget();
            }
            ParentModel.Discard();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Services.TryGet<UnityUpdateProvider>()?.UnregisterGeneric(this);
        }
     }
}