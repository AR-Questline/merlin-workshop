using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using UnityEngine;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// Additional component for <see cref="RecyclableCollectionManager"/> to dynamic adjust size of the list.
    /// Compute and set background grid mask size.
    /// </summary>
    public class VCListAdjuster : ViewComponent {
        [Title("Recyclable Grid Adjust")]
        [SerializeField] RectTransform root;
        [SerializeField] RecyclableCollectionManager recyclableManager;
        [SerializeField] LayoutElement listParentLayoutElement;
        
        [Title("Grid Background Mask")]
        [SerializeField] RectTransform itemListBackground;
        [SerializeField] RectTransform listParent;
        [SerializeField] RectTransform itemsContent;
        
        AssetLoadingGate _loadingGate;

        void OnEnable() {
            _loadingGate = gameObject.GetOrAddComponent<AssetLoadingGate>();
            _loadingGate.gateOnlyOnCreation = false;

            if (_loadingGate.gate == null) {
                _loadingGate.gate = gameObject.AddComponent<CanvasGroup>();
            }
            
            _loadingGate.TryLock();
        }

        public int CalculateDisplayColumnCount(int maxColumnCount) {
            float cellWidth =recyclableManager.CellSize.x + recyclableManager.Spacing.x;
            float rowWidth = root.rect.width + recyclableManager.Spacing.x;
            return Mathf.Clamp(Mathf.FloorToInt(rowWidth / cellWidth), 0, maxColumnCount);
        }        
        
        public async UniTaskVoid FullAdjustWithCollectionRefresh(int maxRowCount, int maxColumnCount) {
            if (_loadingGate.IsLocked == false) {
                _loadingGate.TryLock();
            }
            
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            CalculateDynamicSize(maxRowCount, maxColumnCount);
            
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            UpdateRecyclableManager();
            SetupBackgroundSize();
            _loadingGate.Unlock();
        }
        
        public async UniTaskVoid SizeAdjust(int maxRowCount, int maxColumnCount) {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            CalculateDynamicSize(maxRowCount, maxColumnCount);
            
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            SetupBackgroundSize();
        }

        void CalculateDynamicSize(int maxRowCount, int maxColumnCount) {
            float height = (recyclableManager.Spacing.y + recyclableManager.CellSize.y) * maxRowCount + recyclableManager.Padding.top;
            int columnCount = CalculateDisplayColumnCount(maxColumnCount);
            float width = (recyclableManager.Spacing.x + recyclableManager.CellSize.x) * columnCount + recyclableManager.Padding.left;

            recyclableManager.SecondaryAxisCount = columnCount;
            listParentLayoutElement.minHeight = height - recyclableManager.Spacing.y;
            listParentLayoutElement.preferredWidth = width - recyclableManager.Spacing.x;
        }

        void SetupBackgroundSize() {
            Vector2 targetSize = itemListBackground.sizeDelta;
            targetSize.y = Mathf.Max(listParent.sizeDelta.y, itemsContent.sizeDelta.y) + recyclableManager.Spacing.y;
            itemListBackground.sizeDelta = targetSize;
        }

        void UpdateRecyclableManager() {
            recyclableManager.EnableCollectionManager();
        }
    }
}