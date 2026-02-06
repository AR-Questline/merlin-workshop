using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    public partial class ItemInstrument : Element<Item>, IRefreshedByAttachment<ItemInstrumentAttachment>, IItemAction {
        public override ushort TypeForSerialization => SavedModels.ItemInstrument;

        ARAssetReference _instrumentRef;
        ARAsyncOperationHandle<GameObject> _instrumentHandle;
        GameObject _instrumentInstance;
        bool _assetLoaded;
        bool _isSubmitted;
        IEventListener _quickUseItemUsedListener;

        public ItemActionType Type => _isSubmitted ? ItemActionType.Passive : ItemActionType.Use;
        
        public new static class Events {
            public static readonly Event<IItemOwner, bool> PlayingInstrumentEnded = new(nameof(PlayingInstrumentEnded));
        }
        
        public void InitFromAttachment(ItemInstrumentAttachment spec, bool isRestored) {
            _instrumentRef = spec.instrumentAssetRef?.Get();
        }
        
        public void Submit() {
            if (ParentModel.Owner is not Hero { CanUseEquippedWeapons: true } h || _instrumentInstance != null) {
                return;
            }

            if (h.View<VHeroController>().PerspectiveChangeInProgress) {
                return;
            }

            _isSubmitted = true;
            h.OverridesFSM.SetCurrentState(HeroStateType.PlayInstrument, 0);

            World.Any<CharacterSheetUI>()?.Discard();
            World.Any<QuickUseWheelUI>()?.Discard();
            InstantiateVisual();
            
            _quickUseItemUsedListener = h.ListenToLimited(ICharacter.Events.OnEffectInvokedAnimationEvent, OnQuickUseItemUsedEvent, this);
            h.ListenToLimited(Events.PlayingInstrumentEnded, OnPlayingInstrumentEnded, this);
            h.AddElement<PacifistMarker>().MarkedNotSaved = true;
        }
        
        void InstantiateVisual() {
            if (ParentModel.Owner is not Hero hero) {
                return;
            }

            if (_instrumentRef is not { IsSet: not false }) {
                Log.Important?.Error("Instrument asset reference is not set.");
                return;
            }

            if (_assetLoaded) {
                Log.Important?.Error("Failed to load instrument asset it is already loaded.");
                return;
            }

            _instrumentHandle = _instrumentRef.LoadAsset<GameObject>();
            _instrumentHandle.OnComplete(h => {
                if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null || HasBeenDiscarded) {
                    h.Release();
                    return;
                }
                
                _instrumentInstance = Object.Instantiate(h.Result, hero.MainHand);
                _instrumentInstance.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                    linkedLifetime = true,
                    movable = true
                });
            });
            _assetLoaded = true;
        }

        void OnQuickUseItemUsedEvent() {
            ParentModel.StartPerforming(ItemActionType.Use);
            ParentModel.EndPerforming(ItemActionType.Use);
        }

        void OnPlayingInstrumentEnded() {
            _isSubmitted = false;
            Hero.Current.RemoveElementsOfType<PacifistMarker>();
            World.EventSystem.TryDisposeListener(ref _quickUseItemUsedListener);
            
            _instrumentHandle.Release();
            _instrumentHandle = default;
            _assetLoaded = false;
            if (_instrumentInstance != null) {
                Object.Destroy(_instrumentInstance);
                _instrumentInstance = null;
            }
        }
        
        public void AfterPerformed() { }
        public void Perform() { }
        public void Cancel() { }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_isSubmitted) {
                OnPlayingInstrumentEnded();
            }
            _instrumentRef = null;
            base.OnDiscard(fromDomainDrop);
        }
    }
}