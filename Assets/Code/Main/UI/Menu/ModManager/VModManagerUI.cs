using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.ModManager {
    [UsesPrefab("UI/ModManager/" + nameof(VModManagerUI))]
    public class VModManagerUI : View<ModManagerUI>, IAutoFocusBase {
        [SerializeField] Transform promptsHost, entriesParent;
        [SerializeField] RecyclableCollectionManager recyclableCollectionManager;
        
        public RecyclableCollectionManager RecyclableCollectionManager => recyclableCollectionManager;
        public Transform EntriesParent => entriesParent;
        public Transform PromptsHost => promptsHost;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnMount() {
            EnableCollectionManagerDelayed().Forget();
        }
        
        async UniTaskVoid EnableCollectionManagerDelayed() {
            if (await AsyncUtil.DelayFrame(Target)) {
                RecyclableCollectionManager.EnableCollectionManager();
                var modEntry = Target.Elements<ModEntryUI>().FirstOrDefault();
                var modEntryButton = modEntry?.View<VModEntryUI>().FocusTarget;
                if (modEntry != null && modEntryButton != null) {
                    World.Only<Focus>().Select(modEntryButton);
                    RecyclableCollectionManager.FocusTarget(modEntry);
                }
            }
        }
        
    }
}