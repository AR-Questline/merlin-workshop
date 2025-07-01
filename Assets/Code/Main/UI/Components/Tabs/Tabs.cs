using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using DG.Tweening;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.Tabs {

    /// <summary>
    /// Model responsible for UI Tab management <br/>
    /// see: https://www.notion.so/awaken/Tabs-59ac821af0004296a9ee840d3d076f34
    /// </summary>
    /// <typeparam name="TTarget">ParentModel of each Tab</typeparam>
    /// <typeparam name="TTabsView">View with TabButtons</typeparam>
    /// <typeparam name="TTabType">Class Responsible for providing specific Tab</typeparam>
    /// <typeparam name="TTab">BaseType of each Tab</typeparam>
    public abstract partial class Tabs<TTarget, TTabsView, TTabType, TTab> : Element<TTarget>, IUIAware, IKeyProvider<VCTabSwitchKeyIcon.TabSwitch>, IShortcut
        where TTarget : Tabs<TTarget, TTabsView, TTabType, TTab>.ITabParent 
        where TTabsView : View
        where TTabType : class, Tabs<TTarget, TTabsView, TTabType, TTab>.ITabType
        where TTab : Tabs<TTarget, TTabsView, TTabType, TTab>.ITab
    {
        public override bool IsNotSaved => true;
        public bool BlockNavigation { get; set; }
        protected TTab CurrentTab { get; private set; }
        protected VCTabButton FirstVisible => _buttons.FirstOrAny(b => b.gameObject.activeInHierarchy);
        protected IEnumerable<TTabType> VisibleTabs => _buttons.Where(b => b.gameObject.activeInHierarchy).Select(b => b.Type);
        protected VCTabButton CurrentTabButton => _buttons.FirstOrAny(b => ParentModel.CurrentType == b.Type);
        protected abstract KeyBindings Previous { get; }
        protected abstract KeyBindings Next { get; }
            
        EventReference _selectedSound;
        TabEvents _events;
        VCTabButton[] _buttons;

        protected override void OnInitialize() {
            var view = World.SpawnView<TTabsView>(this, true, true, ParentModel.TabButtonsHost);
            _buttons = GetButtons(view);
            if (_buttons.Length == 0) Log.Important?.Error("No active VCTabButtons found as child of TTabView", view);
            _events = AddElement<TabEvents>();

            ParentModel.ListenTo(Events.AfterChanged, TriggerChange, this);
            TriggerChange();
            if (ParentModel.ForceInvisibleTab == false &&
                (ParentModel.CurrentType == null || VisibleTabs.All(b => b != ParentModel.CurrentType))) {
                ParentModel.CurrentType = VisibleTabs.FirstOrDefault() ?? _buttons[0].Type;
            }
            Spawn(ParentModel.CurrentType, true);
            _selectedSound = CommonReferences.Get.AudioConfig.TabSelectedSound;
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, this));
            ParentModel.TabsController = this;
        }

        public void SelectTab(TTabType type) {
            ChangeTab(type);
        }

        protected virtual VCTabButton[] GetButtons(TTabsView view) {
            return view.GetComponentsInChildren<VCTabButton>(false);
        }

        protected virtual void ChangeTab(TTabType type) {
            TryHandleUnsavedTabChanges(() => {
                PlaySelectedSound();

                if (type == null || ParentModel.CurrentType == type) {
                    return;
                }

                Spawn(type, false);
            });
        }
        
        public void TryHandleUnsavedTabChanges(Action continueCallback) {
            switch (CurrentTab) {
                case IUnsavedChangesPopup { HasUnsavedChanges: true } blocker:
                    blocker.ShowUnsavedPopup(continueCallback);
                    return;
                case ISubTabParent parent:
                    parent.HandleUnsavedChanges(continueCallback);
                    return;
                default:
                    continueCallback();
                    break;
            }
        }
        
        public void TryHandleBack(Action backCallback) {
            switch (CurrentTab) {
                case ITabBackBehaviour back:
                    TryHandleUnsavedTabChanges(back.Back);
                    return;
                case ISubTabParent parent:
                    parent.HandleBack(backCallback);
                    return;
                default:
                    TryHandleUnsavedTabChanges(backCallback);
                    break;
            }
        }

        void PlaySelectedSound() {
            if (!_selectedSound.IsNull) {
                FMODManager.PlayOneShot(_selectedSound);
            }
        }

        void SelectPrevious(TTabType type) {
            //prevent tab switching if there is only one tab
            if (RewiredHelper.IsGamepad && VisibleTabs.Count() == 1) {
                return;
            }
            
            ChangeTab(VisibleTabs.PreviousItem(type, true));
        }
        
        void SelectNext(TTabType type) {
            //prevent tab switching if there is only one tab
            if (RewiredHelper.IsGamepad && VisibleTabs.Count() == 1) {
                return;
            }
            
            ChangeTab(VisibleTabs.NextItem(type, true));
        }

        void Spawn(TTabType type, bool force) {
            if (ParentModel.CurrentType != type || force) {
                ParentModel.RemoveElementsOfType<ITab>();
                ParentModel.CurrentType = type;
                var tab = ParentModel.AddElement(type.Spawn(ParentModel));

                var viewType = tab.TabView;
                if (viewType != null) {
                    var view = World.SpawnView(tab, viewType, true, true, ParentModel.ContentHost);
                    tab.AfterViewSpawned(view);
                }

                CurrentTab = tab;
                _events.TriggerTabsChanged(tab);
            }
        }

        public bool IsSelected(VCTabButton b) => b.Type == ParentModel.CurrentType;

        public UIResult Handle(UIEvent evt) {
            if (HasBeenDiscarded || !this.IsActive() || BlockNavigation) {
                return UIResult.Ignore;
            }
            
            if (evt is UIKeyDownAction action && RewiredHelper.IsGamepad) {
                if (Previous != null && action.Name == Previous) {
                    SelectPrevious(ParentModel.CurrentType);
                    return UIResult.Accept;
                }
                if (Next != null && action.Name == Next) {
                    SelectNext(ParentModel.CurrentType);
                    return UIResult.Accept;
                }
            }
            return OnHandle(evt);
        }
        protected virtual UIResult OnHandle(UIEvent evt) => UIResult.Ignore;
        
        
        public KeyIcon.Data GetKey(VCTabSwitchKeyIcon.TabSwitch key) {
            return key switch {
                VCTabSwitchKeyIcon.TabSwitch.Next => new(Next, false),
                VCTabSwitchKeyIcon.TabSwitch.Previous => new(Previous, false),
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
            };
        }

        public partial class TabEvents : Element<Tabs<TTarget, TTabsView, TTabType, TTab>> {
            public static class Events {
                public static readonly Event<Tabs<TTarget, TTabsView, TTabType, TTab>, TTab> TabsChanged = new(nameof(TabsChanged));
            }

            public void TriggerTabsChanged(TTab tab) {
                ParentModel.Trigger(Events.TabsChanged, tab);
            }
        }

        // === TabTypes

        public interface ITabType {
            TTab Spawn(TTarget target);
            bool IsVisible(TTarget target);
        }

        public abstract class TabTypeEnum : RichEnum, ITabType {
            readonly LocString _title;
            readonly LocString _description;
            
            public string Title => _title;
            [UnityEngine.Scripting.Preserve] public string Description => _description;

            protected TabTypeEnum(string enumName, string title, string description = "") : base(enumName) {
                _title = new LocString {ID = title};
                _description = new LocString {ID = description};
            }

            public abstract TTab Spawn(TTarget target);
            public abstract bool IsVisible(TTarget target);
        }
        
        public abstract class DelegatedTabTypeEnum : TabTypeEnum {
            protected SpawnDelegate _spawn { get; init; }
            protected VisibleDelegate _visible { get; init; }

            protected static readonly VisibleDelegate Always = _ => true;
            protected static readonly VisibleDelegate Never = _ => false;
            
            protected DelegatedTabTypeEnum(string enumName, string title, SpawnDelegate spawn, VisibleDelegate visible, string description = "") : base(enumName, title, description) {
                _spawn = spawn;
                _visible = visible;
            }

            protected DelegatedTabTypeEnum(string enumName, string title, string description = "") : base(enumName, title, description) { }

            public override TTab Spawn(TTarget target) => _spawn(target);
            public override bool IsVisible(TTarget target) => _visible(target);

            protected delegate TTab SpawnDelegate(TTarget target);
            protected delegate bool VisibleDelegate(TTarget target);
        }

        // === TabButton
        
        public abstract class VCTabButton : ViewComponent<Tabs<TTarget, TTabsView, TTabType, TTab>> {
            [Tags(TagsCategory.Flag)][SerializeField]
            string requiredFlag = "";
            [SerializeField] public ARButton button;
            
            public abstract TTabType Type { get; }
            public bool IsSelected => Target.IsSelected(this);
            
            [UnityEngine.Scripting.Preserve]
            protected TTabsView TabsView => (TTabsView) ParentView;
            
            protected override void OnAttach() {
                if (!gameObject.activeSelf) return;
                button.OnClick += Select;
                Target.ListenTo(Events.AfterChanged, Refresh, this);
            }

            public void Select() {
                Target.ChangeTab(Type);
            }

            [UnityEngine.Scripting.Preserve]
            protected void SelectPrevious() {
                Target.SelectPrevious(Type);   
            }
            
            [UnityEngine.Scripting.Preserve]
            protected void SelectNext() {
                Target.SelectNext(Type);   
            }

            void Refresh() {
                bool requiresFlag = !string.IsNullOrEmpty(requiredFlag) && StoryFlags.Get(requiredFlag) == false;
                bool isVisible = Type.IsVisible(Target.ParentModel) && !requiresFlag;
                gameObject.SetActive(isVisible);
                if (isVisible) {
                    Refresh(Type == Target.ParentModel.CurrentType);
                }
            }
            protected abstract void Refresh(bool selected);

            [UnityEngine.Scripting.Preserve] public virtual Component Selectable => this;
        }
        
        public abstract class VCHeaderTabButton : VCTabButton {
            [SerializeField] ButtonConfig buttonConfig;
            [SerializeField] bool tabAsSingleHeader;

            public abstract string ButtonName { get; }
            
            protected override void OnAttach() {
                base.OnAttach();
                buttonConfig.InitializeButton(buttonName: ButtonName, nonInteractive: tabAsSingleHeader);
            }
            
            protected override void Refresh(bool selected) {
                if (tabAsSingleHeader) return;
                buttonConfig.SetSelection(selected);
            }
        }
        
        public abstract class VCSelectableTabButton : VCTabButton {
            [FormerlySerializedAs("selected")] [SerializeField] Image icon;
            
            [Title("Transition animation")]
            [SerializeField] float iconScale = 1.3f;
            [SerializeField] float iconScaleDuration = 0.2f;
            [SerializeField] float iconColorDuration = 0.2f;
            [SerializeField] Color selectedColor;
            [SerializeField] Color hoverColor;
            [SerializeField] Color defaultColor;
            
            bool _isSelected;

            protected override void OnAttach() {
                base.OnAttach();
                button.OnHover += OnHover;
            }
            
            void OnHover(bool isHovered) {
                if (_isSelected) return;

                if (isHovered) {
                    AnimateTabTransition(Vector3.one * iconScale, hoverColor);
                } else {
                    AnimateTabTransition(Vector3.one, defaultColor);
                }
            }
            
            protected override void Refresh(bool selected) {
                _isSelected = selected;

                if (selected) {
                    AnimateTabTransition(Vector3.one, selectedColor);
                } else {
                    AnimateTabTransition(Vector3.one, defaultColor);
                }
            }
            
            void AnimateTabTransition(Vector3 targetScale, Color targetColor) {
                icon.transform.DOScale(targetScale, iconScaleDuration).SetUpdate(true);
                icon.DOColor(targetColor, iconColorDuration).SetUpdate(true);
            }
        }
        
        // === TabParent
        
        public interface ITabParent : IModel {
            Transform TabButtonsHost { get; }
            Transform ContentHost { get; }
            TTabType CurrentType { get; set; }
            bool ForceInvisibleTab => false;

            Tabs<TTarget, TTabsView, TTabType, TTab> TabsController { get; set; }
        }
        
        public interface ITabParent<TParentView> : ITabParent where TParentView : class, ITabParentView {
            Transform ITabParent.TabButtonsHost => View<TParentView>().TabButtonsHost;
            Transform ITabParent.ContentHost => View<TParentView>().ContentHost;
            
            [UnityEngine.Scripting.Preserve] void ToggleTabAndContent(bool showTabs) => View<TParentView>().ToggleTabAndContent(showTabs);
            [UnityEngine.Scripting.Preserve] void HideTabs() => View<TParentView>().HideTabs();
            [UnityEngine.Scripting.Preserve] void ShowTabs() => View<TParentView>().ShowTabs();
            [UnityEngine.Scripting.Preserve] void ShowContent() => View<TParentView>().ShowContent();
            [UnityEngine.Scripting.Preserve] void HideContent() => View<TParentView>().HideContent();
        }
        
        public interface ISubTabParent<TParentView> : ITabParent<TParentView>, ISubTabParent where TParentView : class, ITabParentView {
            ISubTabParent<TParentView> SubTabParent { get; }
            void ISubTabParent.HandleBack(Action backCallback) => TabsController.TryHandleBack(backCallback);
            void ISubTabParent.HandleUnsavedChanges(Action continueCallback) => TabsController.TryHandleUnsavedTabChanges(continueCallback);
        }

        // === Tab
        
        public interface ITab : IElement<TTarget> {
            public Type TabView { get; }
            public bool TryReceiveFocus() => false;
            public void AfterViewSpawned(View view);
        }
        
        public abstract partial class Tab<TTabView> : Element<TTarget>, ITab where TTabView : View {
            public sealed override bool IsNotSaved => true;

            public Type TabView => typeof(TTabView);
            public virtual bool TryReceiveFocus() => false;
            
            public void AfterViewSpawned(View view) => AfterViewSpawned((TTabView) view);
            protected virtual void AfterViewSpawned(TTabView view) { }
        }
        
        public abstract partial class TabWithoutView : Element<TTarget>, ITab {
            public sealed override bool IsNotSaved => true;

            public Type TabView => null;
            public void AfterViewSpawned(View view) { }
        }
        
        public abstract partial class EmptyTabWithBackBehaviour : TabWithoutView, ITabBackBehaviour {
            public abstract void Back();
        }
        
        public abstract partial class TabWithBackBehaviour<TTabView> : Tab<TTabView>, ITabBackBehaviour where TTabView : View {
            public abstract void Back();
        }

        interface ITabBackBehaviour {
            void Back();
        }
    }
}