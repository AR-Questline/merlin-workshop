using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class SearchActionVfx : Element<Location>, IRefreshedByAttachment<SearchVfxAttachment> {
        public override ushort TypeForSerialization => SavedModels.SearchActionVfx;

        SearchVfxAttachment _spec;
        IPooledInstance _vfxInstance;
        CancellationTokenSource _vfxCts;
        
        public void InitFromAttachment(SearchVfxAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            if (!ParentModel.TryGetElement<SearchAction>(out var searchAction)) {
                Discard();
                return;
            }

            ParentModel.ListenTo(Location.Events.InteractabilityChanged, OnInteractabilityChanged, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<ContainerUI>(), this, AttachToContainerUI);
        }

        void OnInteractabilityChanged(LocationInteractability interactability) {
            if (interactability == LocationInteractability.Active) {
                ShowVfx();
            } else {
                HideVfx();
            }
        }
        
        void AttachToContainerUI(Model container) {
            var containerUI = (ContainerUI) container;
            if (containerUI.ParentModel != ParentModel) {
                return;
            }
            
            containerUI.ListenTo(ContainerUI.ContainerEvents.ContentChanged, ui => {
                if (ui.IsEmpty) {
                    Discard();
                }
            }, this);
        }

        async UniTaskVoid ShowVfx() {
            if (_vfxInstance != null) {
                return;
            }
            
            _vfxCts = new CancellationTokenSource();
            _vfxInstance = await PrefabPool.Instantiate(_spec.vfx, ParentModel.Coords, Quaternion.identity, cancellationToken: _vfxCts.Token);
            if (_vfxInstance.Instance == null) {
                _vfxInstance.Release();
                _vfxInstance = null;
            }
        }

        void HideVfx() {
            _vfxCts?.Cancel();
            _vfxCts = null;
            
            VFXUtils.StopVfxAndReturn(_vfxInstance, 5f);
            _vfxInstance = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            HideVfx();
        }
    }
}