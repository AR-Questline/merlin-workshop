using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public partial class ArenaSpawnerUI : Element<ArenaSpawner>, IClosable, IUIStateSource {
        public sealed override bool IsNotSaved => true;

        static UIDocumentProvider UIDocumentProvider => Services.Get<UIDocumentProvider>();
        static PresenterDataProvider PresenterDataProvider => Services.Get<PresenterDataProvider>();
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime();
        PArenaSpawnerUI PArenaSpawnerUI => Presenter<PArenaSpawnerUI>();

        public void Close() {
            Discard();
        }
        
        protected override void OnInitialize() {
            ParentModel.ResetPositionOffset();
        }

        protected override void OnFullyInitialized() {
            var parent = UIDocumentProvider.TryGetDocument(UIDocumentType.Default).rootVisualElement;
            var arenaSpawnerData = new ArenaSpawnerData(); 
            var arenaSpawnerUI = new PArenaSpawnerUI(PresenterDataProvider.arenaSpawnerData, parent, arenaSpawnerData, SpawnNpc);
            World.BindPresenter(this, arenaSpawnerUI, OnArenaSpawnerUIPrepared);
        }

        void OnArenaSpawnerUIPrepared() {
            var arenaSpawnerElementReference = PresenterDataProvider.arenaSpawnerData.ArenaContainerElementData.uxml;
            arenaSpawnerElementReference.GetAndLoad<VisualTreeAsset>(handle => OnArenaSpawnListElementUIPrepared(handle.Result));
        }

        void OnArenaSpawnListElementUIPrepared(VisualTreeAsset listElementAsset) {
            PArenaSpawnerUI.Setup(listElementAsset);
        }

        void SpawnNpc(LocationTemplate template) {
            ParentModel.SpawnNpc(template);
        }
    }
}