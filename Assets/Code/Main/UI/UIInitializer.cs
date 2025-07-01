using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI {
    public class UIInitializer : MonoBehaviour {
        [field: Title("UTK")]
        [field: Required, SerializeField] public UIDocumentProvider UIDocumentProvider { get; private set; }
        [field: Required, SerializeField] public PresenterDataProvider PresenterDataProvider { get; private set; }
        [field: Required, SerializeField] public UTKPanelSettingsService PanelSettingsService { get; private set; }
        [field: Title("UGUI")]
        [field: Required, SerializeField] public CanvasService CanvasServices { get; private set; }
        [field: Required, SerializeField] public Transform SpawnRoot { get; private set; }
        [field: Required, SerializeField] public MapStickerUI MapStickerUI { get; private set; }

        static Services Services => World.Services;
        
        public void InitAfterServices() {
            Services.Register(UIDocumentProvider);
            Services.Register(PresenterDataProvider);
            Services.Register(PanelSettingsService);
            PanelSettingsService.Init();
            
            Services.Register(CanvasServices);
            
            // view hosting: this service lets views spawn in different places (on the map, on the HUD, etc.)
            Services.Register(new ViewHosting(SpawnRoot));
        }
        
        public void InitAfterWorld() {
            // stickers: for handling UI that is attached to a world object
            Services.Register(MapStickerUI);
            MapStickerUI.Init();
            CanvasServices.HandleAspectRatioScaler(World.Only<GameCamera>());
        }
    }
}