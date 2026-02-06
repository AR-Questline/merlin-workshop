using System.Text.RegularExpressions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.ModManager {
    [UsesPrefab("UI/ModManager/" + nameof(VModManagerUI))]
    public class VModManagerUI : View<ModManagerUI>, IAutoFocusBase, IEmptyInfo {
        const string ModsPath = @"%LocalAppData%Low\Questline\Fall of Avalon\Mods";
        [SerializeField] Transform promptsHost, entriesParent;
        [SerializeField] RecyclableCollectionManager recyclableCollectionManager;
        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;
        
        public RecyclableCollectionManager RecyclableCollectionManager => recyclableCollectionManager;
        public Transform EntriesParent => entriesParent;
        public Transform PromptsHost => promptsHost;
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnMount() {
            PrepareEmptyInfo();
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
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups, LocTerms.EmptyModManagerInfo.Translate(), LocTerms.EmptyModManagerDesc.Translate(Regex.Escape(ModsPath.Italic())));
            TextLinkHandler.OpenLinksOf(Target);
        }
    }
}