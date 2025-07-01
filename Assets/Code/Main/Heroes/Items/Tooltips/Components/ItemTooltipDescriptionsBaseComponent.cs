using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public abstract class ItemTooltipDescriptionsBaseComponent<T> : IItemTooltipComponent {
        [SerializeField] protected ItemDescriptionElement prefab;
        [SerializeField, Required] GameObject parentSection;

        protected Transform ParentSection => parentSection.transform;
        protected ObjectPool<ItemDescriptionElement> _elementPool;
        List<ItemDescriptionElement> _visibleElements;
        DescriptionComponentConfig _config;
        
        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; protected set; }

        protected ItemTooltipDescriptionsBaseComponent(DescriptionComponentConfig config) {
            _config = config;
        }
        
        public virtual void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            PreparePool();
            Setup(descriptor, TargetView);
            SetupElementVisibility();
        }
        
        public virtual void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            PreparePool();
            Setup(descriptor, TargetView ? TargetView : view);
            SetupElementVisibility();
        }

        public abstract void ToggleSectionActive(bool active);
        protected abstract void Setup(IItemDescriptor descriptor, View view);
        protected abstract void PrepareItemDescription(T item, ItemDescriptionElement descriptionElement, View view);

        protected void PrepareDescription(T item, View view) {
            PrepareItemDescription(item, _elementPool.Get(), view);
        }
        
        protected void PrepareDescription(IEnumerable<T> items, View view) {
            foreach (var item in items) {
                PrepareItemDescription(item, _elementPool.Get(), view);
            }
        }
        
        protected void SetParentSectionVisibility(bool visible) {
            if (!_config.CanDisableParent() && visible) {
                parentSection.SetActiveOptimized(true);
            } else if (_config.CanDisableParent()) {
                parentSection.SetActiveOptimized(visible);
            }
        }
        
        void PreparePool() {
            if (_visibleElements == null) {
                _visibleElements = new List<ItemDescriptionElement>();
            } else {
                foreach (var item in _visibleElements) {
                    _elementPool.Release(item);
                }
                _visibleElements.Clear();
            }
            
            _elementPool ??= new ObjectPool<ItemDescriptionElement>(CreateElement, element => OnGetElement(element), element => element.OnReleaseElement(), element => element.OnDestroyElement(), true, 3, 6);
        }

        ItemDescriptionElement CreateElement() {
            ItemDescriptionElement instance = Object.Instantiate(prefab, parentSection.transform);

            if (TargetView != null) {
                instance.Attach(World.Services, TargetView.GenericTarget, TargetView);
                instance.GetOrAddComponent<VCAccessibility>().Attach(World.Services, TargetView.GenericTarget, TargetView);
            }

            instance.gameObject.SetActive(false);
            return instance;
        }
        
        ItemDescriptionElement OnGetElement(ItemDescriptionElement element) {
            _visibleElements.Add(element);
            return element;
        }
        
        void SetupElementVisibility() {
            foreach (var element in _visibleElements) {
                element.gameObject.SetActive(true);
            }
        }
    }
    
    public readonly struct DescriptionComponentConfig {
        bool HasSharedParent { get; }
        bool IsParentManager { get; }
        [UnityEngine.Scripting.Preserve] 
        public DescriptionComponentConfig(bool hasSharedParent, bool isParentManager = false) {
            HasSharedParent = hasSharedParent;
            IsParentManager = isParentManager;
        }

        public bool CanDisableParent() {
            return !HasSharedParent || (HasSharedParent && IsParentManager);
        }
    }
}