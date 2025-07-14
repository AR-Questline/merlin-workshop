using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// To use this component, ensure you have a view that inherits from <see cref="RetargetableView"/> and a model implementing the <see cref="IWithRecyclableView"/> interface.
    /// Init manager manually by calling <see cref="EnableCollectionManager"/> method.
    /// </summary>
    // IMPORTANT! Many changes have been made to work with many recyclable collection at one scroll view. At the moment it only works in vertical direction!
    public class RecyclableCollectionManager : ViewComponent, IRecyclableViewsManager {
        const int BufferSize = 2;

        const string ScrollGroup = "Scroll";
        const string ReferencesGroup = "References";
        const string PresentationGroup = "Presentation";
        
        [SerializeField, BoxGroup(ScrollGroup)] SelectionAutoScroll autoScroll;
        [SerializeField, BoxGroup(ReferencesGroup)] RectTransform container;
        [SerializeField, BoxGroup(ReferencesGroup)] Mask viewport;
        [SerializeField, BoxGroup(ReferencesGroup)] RectTransform focusTarget;
        [SerializeField, BoxGroup(PresentationGroup)] int secondaryAxisCount = 1;
        [SerializeField, BoxGroup(PresentationGroup)] RectOffset padding;
        [SerializeField, BoxGroup(PresentationGroup)] Vector2 spacing;
        [SerializeField, BoxGroup(PresentationGroup)] Direction mainAxis = Direction.Vertical;
        [SerializeField, BoxGroup(PresentationGroup)] int firstItemPositionShift;
        [SerializeField, BoxGroup(PresentationGroup)] bool constantSize;
        [SerializeField, BoxGroup(PresentationGroup), ShowIf(nameof(constantSize))]
        Vector2 constantCellSize;
        [SerializeField, BoxGroup(PresentationGroup)] int additionalViewsCount;

        [ShowInInspector, FoldoutGroup("Debug")]
        readonly List<IWithRecyclableView> _models = new();
        [ShowInInspector, FoldoutGroup("Debug")]
        readonly List<RetargetableViewWithRectTransform> _viewsWithRectTransforms = new();

        bool _dirty;
        bool _enabled;
        int _focusIndex;
        int _visibleViewsCount;
        float _scaleFactor;
        RectTransform _viewportRT;
        RetargetableView _prefab;
        IWithRecyclableView _focusModelTarget;
        Vector2Int _placingDirection;
        Vector2 _itemSize;
        VHostItemsListWithCategory _hostItemsList;
        
        Rect _viewportWorldRect;
        Rect _containerCurrentWorldRect;
        Rect _firstRowLocalRect;

        public Vector2 Spacing => spacing;
        public RectOffset Padding => padding;
        public Vector2 CellSize => constantSize ? constantCellSize : _itemSize;
        public SelectionAutoScroll AutoScroll => autoScroll;
        
        Vector2 MaskSize => _viewportRT.rect.size;
        int ViewsCount => _viewsWithRectTransforms.Count;
        int MainDirectionCount => (int)math.ceil((_models.Count + firstItemPositionShift) / (float)secondaryAxisCount);
        bool IsMultipleRecyclableManagerAtOnce => _hostItemsList != null;
        Vector2 ScaledMaskSize => _viewportRT.rect.size * _scaleFactor;
        Vector2 ScaledItemSize => _itemSize * _scaleFactor;
        Vector2 ScaledContainerSize => container.rect.size * _scaleFactor;
        
        public int SecondaryAxisCount {
            [UnityEngine.Scripting.Preserve] get => secondaryAxisCount;
            set {
                if (value == secondaryAxisCount || value < 1) {
                    return;
                }

                secondaryAxisCount = value;
                _dirty = true;
            }
        }

        protected override void OnAttach() {
            if (mainAxis == Direction.Horizontal) {
                Log.Important?.Error("Horizontal direction is not supported yet in RecyclableCollectionManager. Auto switching to vertical mode", this);
                mainAxis = Direction.Vertical;
                return;
            }
            
            _placingDirection = mainAxis == Direction.Horizontal ? Vector2Int.right : Vector2Int.up;
            _hostItemsList = GetComponentInParent<VHostItemsListWithCategory>();
            _scaleFactor = World.Services.Get<CanvasService>().MainCanvas.scaleFactor;
            InitViewport();
        }

        void InitViewport() {
            _viewportRT = IsMultipleRecyclableManagerAtOnce ? _hostItemsList.Viewport : viewport.GetComponent<RectTransform>();
        }

        public void EnableCollectionManager() {
            UpdateVisibleCount();
            Resize();
            
            if (!_enabled) {
                InitBounds();
                InitScroll();
            }
            
            _enabled = true;
        }
        
        void InitBounds() {
            Vector2 maskSizeWithBuffer = ScaledMaskSize + _itemSize * 0.5f;
            var containerPosition = container.position.XY();
            var containerSize = ScaledContainerSize * 0.5f;
            
            _viewportWorldRect = new Rect(_viewportRT.position, maskSizeWithBuffer);
            _containerCurrentWorldRect = new Rect(new Vector2(containerPosition.x + containerSize.x, containerPosition.y - containerSize.y), ScaledContainerSize);
            _firstRowLocalRect = new Rect(Vector2.zero, ScaledItemSize);
        }
        
        void InitScroll() {
            if (IsMultipleRecyclableManagerAtOnce) {
                AttachEventToScrollbar(_hostItemsList.CategoriesScrollRect);
            }
            
            var scrollRectInParent = GetComponentInParent<ScrollRect>();
            AttachEventToScrollbar(scrollRectInParent);
            return;

            void AttachEventToScrollbar(ScrollRect scrollRect) {
                if (scrollRect == null) {
                    return;
                }

                Scrollbar scrollbar = mainAxis == Direction.Horizontal ?
                    scrollRect.horizontalScrollbar :
                    scrollRect.verticalScrollbar;

                if (scrollbar) {
                    scrollbar.onValueChanged.AddListener(_ => SetDirty());
                }
            }
        }

        void LateUpdate() {
            if (_viewportRT == null || !_enabled) {
                return;
            }
            
            UpdateFocus();
            
            if (_dirty) {
                _dirty = false;
                InitBounds();
                UpdatePlacing();
            }
        }

        // === Elements
        public void AddElement(IWithRecyclableView model, RetargetableView prefab) {
            if (GenericTarget.IsBeingDiscarded) {
                return;
            }

            if (_prefab == null) {
                _prefab = prefab;
                InitializePrefab(_prefab);
                SetupRectTransform(focusTarget);
            }

            model.ListenTo(Model.Events.BeforeDiscarded, _ => RemoveElement(model), this);
            _models.Add(model);

            if (_enabled) {
                Resize();
            }
        }

        public void RemoveElement(IWithRecyclableView model) {
            var index = _models.IndexOf(model);
            if (index == -1) {
                return;
            }
            
            _models.RemoveAt(index);
            
            var requiredViews = math.min(_models.Count, _visibleViewsCount + BufferSize * secondaryAxisCount);
            requiredViews = math.max(requiredViews, 0);

            if (requiredViews < ViewsCount) {
                Resize();
            } else {
                var toRemoveIndex = _viewsWithRectTransforms.FindIndex(v => v.view.RecyclableTarget.IsNotIn(_models));
                var toRemoveView = _viewsWithRectTransforms[toRemoveIndex].view;
                toRemoveView.Discard();
                
                var spawned = Instantiate(_prefab, container.transform);

                RectTransform rectTransform = spawned.GetComponent<RectTransform>();
                SetupRectTransform(rectTransform);

                _viewsWithRectTransforms[toRemoveIndex] = new(spawned, rectTransform);
            }
            
            UpdatePlacing();
        }

        void InitializePrefab(RetargetableView prefab) {
            if (constantSize) {
                _itemSize = constantCellSize;
            } else {
                var prefabRect = prefab.GetComponent<RectTransform>();
                var itemSize = prefabRect.rect.size;
                if (mainAxis == Direction.Vertical) {
                    itemSize.x = MaskSize.x - padding.horizontal;
                } else {
                    itemSize.y = MaskSize.y - padding.vertical;
                }
                _itemSize = itemSize;
            }
        }
        
        // === Focus
        public void FocusTarget(IWithRecyclableView target) {
            _focusModelTarget = null;
            _focusIndex = -1;
            var focusIndex = _models.IndexOf(target);
            if (focusIndex == -1) {
                Log.Important?.Error($"Trying to focus on model which isn't present in collection", this);
                return;
            }
            _focusModelTarget = target;
            _focusIndex = focusIndex;

            AssignFocus();
        }

        void UpdateFocus() {
            if (_focusModelTarget?.HasBeenDiscarded ?? true) {
                _focusModelTarget = null;
                _focusIndex = -1;
                return;
            }

            var focusIndex = _models.IndexOf(_focusModelTarget);
            if (focusIndex == _focusIndex) {
                return;
            }
            if (focusIndex == -1) {
                _focusModelTarget = null;
                _focusIndex = -1;
                return;
            }
            _focusIndex = focusIndex;

            AssignFocus();
        }

        void AssignFocus() {
            if (!autoScroll) {
                return;
            }
            MoveItemToIndex(focusTarget, _focusIndex);
            autoScroll.FindRectAndRecalculate(focusTarget).Forget();
            _dirty = true;
        }

        // === Operations
        public void SetDirty() { 
            _dirty = true;
        }
        
        public void OrderChangedRefresh() {
            _dirty = true;
            _models.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        public void ContainerResize() {
            container.sizeDelta = CalculateContainerSize();
        }
        
        public void Resize() {
            ContainerResize();

            var requiredViews = math.min(_models.Count, _visibleViewsCount + BufferSize * secondaryAxisCount);
            requiredViews = math.max(requiredViews, 0);

            for (int i = ViewsCount; i < requiredViews; i++) {
                var spawned = Instantiate(_prefab, container.transform);

                RectTransform rectTransform = spawned.GetComponent<RectTransform>();
                SetupRectTransform(rectTransform);

                _viewsWithRectTransforms.Add(new(spawned, rectTransform));

                MoveItemToIndex(rectTransform, i);
                World.ReBind(_models[i], _viewsWithRectTransforms[i].view);
            }
            
            while (requiredViews < ViewsCount) {
                var toRemoveIndex = _viewsWithRectTransforms.FindIndex(v => v.view.RecyclableTarget.IsNotIn(_models));
                
                //Remove when view out of viewports bounds
                if (toRemoveIndex == -1) {
                    toRemoveIndex = _viewsWithRectTransforms.FindIndex(v => !_viewportWorldRect.ContainsInWorldSpace(v.rectTransform.position.XY()));
                }
                
                var toRemoveView = _viewsWithRectTransforms[toRemoveIndex].view;
                _viewsWithRectTransforms.RemoveAt(toRemoveIndex);
                toRemoveView.Discard();
            }

            _dirty = true;
        }

        void MoveItemToIndex(RectTransform item, int index) {
            var itemRect = ItemRect(IndexToIndex2(index));
            item.anchoredPosition3D = new Vector3(itemRect.xMin, itemRect.yMax);
        }

        void UpdatePlacing() {
            if (IsMultipleRecyclableManagerAtOnce) {
                UpdateVisibleCountInViewport();
            }
            
            if (ViewsCount == 0 || _models.Count == 0) {
                return;
            }

            CalculateRebindIndices(out var startViewIndex, out var modelIndex);

            for (var i = startViewIndex; i < ViewsCount; i++) {
                MoveItemToIndex(_viewsWithRectTransforms[i].rectTransform, modelIndex % _models.Count);
                World.ReBind(_models[modelIndex % _models.Count], _viewsWithRectTransforms[i].view);
                modelIndex++;
            }
            
            for (var i = 0; i < startViewIndex; i++) {
                MoveItemToIndex(_viewsWithRectTransforms[i].rectTransform, modelIndex % _models.Count);
                World.ReBind(_models[modelIndex % _models.Count], _viewsWithRectTransforms[i].view);
                modelIndex++;
            }
        }

        void UpdateVisibleCount() {
            var maskSize = mainAxis == Direction.Horizontal ? MaskSize.x : MaskSize.y;
            var rowAlignSize = mainAxis == Direction.Horizontal ? _itemSize.x + spacing.x : _itemSize.y + spacing.y;
            var visibleRowsCount = (int)math.ceil(maskSize / math.max(rowAlignSize, 0.001f));
            _visibleViewsCount = (visibleRowsCount * secondaryAxisCount) - firstItemPositionShift + additionalViewsCount;
        }

        void UpdateVisibleCountInViewport() {
            var prevVisibleCount = _visibleViewsCount;
            var containerPosition = container.position.XY();
            var containerSize = ScaledContainerSize * 0.5f;
            _containerCurrentWorldRect.position = new Vector2(containerPosition.x + containerSize.x, containerPosition.y - containerSize.y);

            //TODO: change to better performance friendly discard views when they are out of bounds, not all at once
            // _visibleViewsCount = _viewsWithRectTransforms.Count(v => _viewportWorldBounds.Contains(v.rectTransform.position));
            if (!_viewportWorldRect.OverlapInWorldSpace(_containerCurrentWorldRect)) {
                _visibleViewsCount = 0;
            } else {
                UpdateVisibleCount();
            }
            
            if (prevVisibleCount != _visibleViewsCount) {
                Resize();
            }
        }

        void SetupRectTransform(RectTransform rectTransform) {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.sizeDelta = _itemSize;
        }

        // === Math
        Vector2 CalculateContainerSize() {
            var paddingSize = new Vector2(padding.horizontal, padding.vertical);
            var containerSize = MaskSize - paddingSize;

            CalculateDirectionContainerSize(mainAxis == Direction.Horizontal ? 0 : 1, ref containerSize);

            containerSize += paddingSize;
            return containerSize;
        }

        void CalculateDirectionContainerSize(int directionIndex, ref Vector2 containerSize) {
            var fullSpacing = (math.max(MainDirectionCount, 0) * spacing[directionIndex]) - spacing[directionIndex];
            var contentSize = _itemSize[directionIndex] * MainDirectionCount;
            containerSize[directionIndex] = fullSpacing + contentSize;
        }
        
        void CalculateRebindIndices(out int startViewIndex, out int modelIndex) {
            var firstVisibleModel = FirstVisibleModel();
            var firstModelInMainAxis = math.max(0, firstVisibleModel - 1);
            
            var startViewsInMainAxis = firstModelInMainAxis % (int)math.ceil(ViewsCount / (float)secondaryAxisCount);
            
            modelIndex = firstModelInMainAxis*secondaryAxisCount;
            startViewIndex = startViewsInMainAxis*secondaryAxisCount;
        }

        int FirstVisibleModel() {
            for (var i = 0; i < MainDirectionCount; i++) {
                var rectIndex = _placingDirection * i;
                Vector2 rowItemPosition = FirsRowItemPosition(rectIndex.y);
                _firstRowLocalRect.position = rowItemPosition;

                if (_viewportWorldRect.OverlapInWorldSpace((_firstRowLocalRect))) {
                    return i;
                }
            }
            
            return 0;
        }

        Vector2 FirsRowItemPosition(int index) {
            var itemSize = ScaledItemSize;
            var halfItemSize = itemSize * 0.5f;
            var scaledPadding = new Vector2(padding.left, padding.top) * _scaleFactor;
            var scaledSpacing = spacing * _scaleFactor;
            var containerPosition = container.position.XY();
            
            return new Vector2(containerPosition.x + halfItemSize.x + scaledPadding.x * _scaleFactor, 
                containerPosition.y - halfItemSize.y - (itemSize.y * index) - scaledPadding.y * _scaleFactor - scaledSpacing.y * index);
        }

        Rect ItemRect(Vector2 index) {
            var posX = padding.left + index.x * _itemSize.x + index.x * spacing.x;
            var posY = -(padding.top + index.y * _itemSize.y + index.y * spacing.y);
            return new Rect(posX, posY - _itemSize.y, _itemSize.x, _itemSize.y);
        }

        Vector2Int IndexToIndex2(int index) {
            index += firstItemPositionShift;
            int x = index % secondaryAxisCount;
            int y = index / secondaryAxisCount;
            
            return mainAxis == Direction.Vertical ? new Vector2Int(x, y) : new Vector2Int(y, x);
        }
        
        protected override void OnDiscard() {
            _models.Clear();
            Resize();
        }

        // === Helpers
        readonly struct RetargetableViewWithRectTransform {
            public readonly RetargetableView view;
            public readonly RectTransform rectTransform;

            public RetargetableViewWithRectTransform(RetargetableView view, RectTransform rectTransform) {
                this.view = view;
                this.rectTransform = rectTransform;
            }
        }

        enum Direction : byte {
            Horizontal = 0,
            Vertical = 1,
        }
        
#if UNITY_EDITOR
        bool _debugDraw;
        
        [Button]
        void ToggleDebugDraw() {
            _debugDraw = !_debugDraw;
        }
        
        void OnDrawGizmos() {
            if (!_debugDraw) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(_firstRowLocalRect.position, _firstRowLocalRect.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_viewportWorldRect.position, _viewportWorldRect.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_containerCurrentWorldRect.position, _containerCurrentWorldRect.size);
        }
#endif
    }
}
