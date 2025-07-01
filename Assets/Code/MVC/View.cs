using System;
using Awaken.Utility.Animations;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using Awaken.Utility.Profiling;
using JetBrains.Annotations;
using Unity.IL2CPP.CompilerServices;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC {
    /// <summary>
    /// Base class for Views - game objects that represent a model
    /// somewhere in the Unity hierarchy.
    /// </summary>
    [SelectionBase, Il2CppEagerStaticClassConstruction]
    public abstract class View : MonoBehaviour, IView, INamed, IReleasableOwner {

        // === Statics

        static int s_nextID = 1;
        
        public static void EDITOR_RuntimeReset() {
            s_nextID = 1;
        }

        // === References
        
        public string LocID {
            get {
                if (string.IsNullOrWhiteSpace(locID)) {
                    locID = "View";
                }
                return locID;
            }
            set => locID = value;
        }
        
        [SerializeField]
        [HideInInspector]
        string locID;

        public string ID { get; protected set; }
        public string DisplayName => GenericTarget is INamed named ? $"{named.DisplayName}:View({GetType().Name})" : "";
        public string DebugName => this != null ? name : "Destroyed View";
        [ProvidesContext]
        protected Services Services => World.Services;
        public IModel GenericTarget { get; private set; }
        public bool IsInitialized => GenericTarget != null;
        public bool IsBeingDiscarded { get; private set; } = false;
        public bool WasDiscarded { get; private set; } = false;
        /// <summary>
        /// IsBeingDiscarded || WasDiscarded
        /// </summary>
        public bool HasBeenDiscarded => (IsBeingDiscarded | WasDiscarded) || GenericTarget is {HasBeenDiscarded: true};
        protected virtual bool ShouldDestroyGameObjectOnDiscard => true;
        
        public IBackgroundTask DestroyWaitingFor { get; private set; }

        // === Assets references
        /// <summary>
        /// Track used assets to auto release on discard
        /// </summary>
        HashSet<IReleasableReference> _releasableReferences;

        // === Initialization/deinitialization

        public void Initialize(Services services, IModel model) {
            ID = GenerateID(model);
            GenericTarget = model;
            OnTargetAssigned();
            OnInitialize();
            InitializeViewComponents();
            InitializeNestedViews();
            InitializeTexts();
            OnFullyInitialized();
        }

        public void Mount(Transform parent) {
            transform.SetParent(parent, worldPositionStays: false);
            World.Any<Focus>()?.RegisterView(this);
            OnMount();
        }

        protected virtual void OnTargetAssigned() {}

        // === IDs

        protected virtual string GenerateID(IModel target) {
            return $"{target.ID}[{GetType().Name}][{s_nextID++}]";
        }

        // === View components

        void InitializeViewComponents() {
            // Add Accessibility component to UI
            if (transform is RectTransform && gameObject.GetComponent<VCAccessibility>() == null) {
                gameObject.AddComponent<VCAccessibility>();
            }
            InitializeViewComponents(transform);
        }

        protected void InitializeViewComponents(Transform root) {
            // Attach view components to model
            foreach (var component in ScanForComponents(root)) {
                try {
                    component.Attach(Services, GenericTarget, this);
                } catch (Exception e) {
                    Log.Important?.Error(
                        $"Exception below happened for ViewComponent \n" +
                        $"[component: {component.GetType().Name}|{component.gameObject.name}]\n" +
                        $"[view: {gameObject.name}]\n" +
                        $"[model: {GenericTarget}]", 
                        component, 
                        LogOption.NoStacktrace
                    );
                    Debug.LogException(e);
                }
            }

            // Initialize Visual Scripting
            foreach (var machine in MyEventMachines()) {
                Variables.Object((Component)machine).Set(VGUtils.ModelVariableName, GenericTarget);
            }
        }

        protected virtual IEventMachine[] MyEventMachines() => GetComponentsInChildren<IEventMachine>(true);

        // === Nested views

        protected abstract bool CanNestInside(View view);
        protected virtual void InitializeNestedViews() {
            foreach (var view in GetComponentsInChildren<View>(true)) {
                if (view.GenericTarget != null) continue;

                if (view.CanNestInside(this)) {
                    World.BindView(GenericTarget, view, false, true);
                }
            }
        }

        protected void InitializeTexts() {
            foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (tmp.gameObject.GetComponent<TextLinkHandler>() == null) {
                    var handler = tmp.gameObject.AddComponent<TextLinkHandler>();
                    handler.Attach(Services, GenericTarget, this);
                }
            }
        }

        /// <summary>
        /// Looks for components in children, similar to GetComponentsInChildren.
        /// However, this version respects the View boundary - it won't return
        /// components belonging to children Views.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ViewComponent> ScanForComponents(Transform root, List<ViewComponent> components = null) {
            components ??= new(2);
            components.Clear();
            root.GetComponents(components);
            foreach (ViewComponent vc in components) {
                yield return vc;
            }
            var childrenCount = root.childCount;
            for (var i = 0; i < childrenCount; i++) {
                var child = root.GetChild(i);
                if (child.GetComponent<View>()) continue;
                foreach (ViewComponent vc in ScanForComponents(child, components)) {
                    yield return vc;
                }
            }
        }

        // === Discarding

        /// <summary>
        /// Discards the view completely, destroying the game object
        /// and unregistering it from any relevant services.
        /// </summary>
        public void Discard() {
            if (WasDiscarded) {
                if (this) {
                    Log.Debug?.Warning($"Discarding already discarded view {name}", this);
                } else {
                    Log.Debug?.Warning($"Discarding already destroyed view");
                }
                return;
            }
            if (IsBeingDiscarded) {
                if (this) {
                    Log.Debug?.Warning($"Discarding view that begin being discarded {name}", this);
                } else {
                    Log.Debug?.Warning($"Discarding already destroyed view that begin being discarded");
                }
                return;
            }
            IsBeingDiscarded = true;
            DestroyWaitingFor = OnDiscard();

            World.RemoveView(this);
            World.EventSystem.RemoveAllListenersTiedTo(this, true);
            World.EventSystem.RemoveAllListenersOwnedBy(this, true);
            
            // Cleanup used assets
            ReleaseReleasable();

            if (this == null || gameObject == null) {
                IsBeingDiscarded = false;
                WasDiscarded = true;
                CleanReferences();
                return;
            }

            if (ShouldDestroyGameObjectOnDiscard) {
                if (DestroyWaitingFor == null) {
                    try {
                        ProfilerValues.SpawnedViewsCounters.Remove();
                        Destroy(gameObject);
                    } catch (Exception e) {
                        LogException(e);
                    }
                    IsBeingDiscarded = false;
                    WasDiscarded = true;
                    CleanReferences();
                } else {
                    WaitThenDestroy(DestroyWaitingFor).Forget();
                }
            } else {
                ProfilerValues.SpawnedViewsCounters.Remove();
                IsBeingDiscarded = false;
                WasDiscarded = true;
                CleanReferences();
            }
        }

        void LogException(Exception e) {
            string targetName;
            try {
                targetName = GenericTarget is INamed named ? named.DisplayName : "";
                string goName = gameObject == null ? "Null" : gameObject.name;
                Log.Important?.Error($"Exception happened for view type {GetType()} name: {goName}. Target name: {targetName}");
                Debug.LogException(e);
            } catch {
                Log.Important?.Error($"Couldn't get some data in view discard exception: Self exist: {this != null}, GameObject exist: {gameObject != null}");
                Debug.LogException(e);
            }
        }

        async UniTaskVoid WaitThenDestroy(IBackgroundTask waitingFor) {
            while (!waitingFor.Done) {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }

            ProfilerValues.SpawnedViewsCounters.Remove();
            if (this && gameObject) {
                Destroy(gameObject);
            }
            CleanReferences();
            IsBeingDiscarded = false;
            WasDiscarded = true;
        }

        protected virtual void CleanReferences() {
            GenericTarget = null;
            DestroyWaitingFor = null;
        }

        // === Hooks for overrides

        /// <summary>
        /// Called during view initialization, after the references to
        /// world, services and target model are all set.
        /// </summary>
        protected virtual void OnInitialize() {
            // empty, room for expansion
        }
        /// <summary>
        /// Called after all related views are initialized. <br/>
        /// Before OnMount if mounted through binding
        /// </summary>
        protected virtual void OnFullyInitialized() {
            // empty, room for expansion
        }

        /// <summary>
        /// Called after the model is attached to its final spot in
        /// the Unity hierarchy. At this point, checking eg. transform.parent
        /// is valid (which is not true for OnInitialize).
        /// </summary>
        protected virtual void OnMount() {
        }

        /// <summary>
        /// Called when the view is about to be discarded. Optionally, you can
        /// return an IBackgroundTask - in that case, the destruction of the
        /// game object will be delayed until that task finishes, which lets
        /// you perform an animation before the object disappears.
        /// </summary>
        /// <returns></returns>
        protected virtual IBackgroundTask OnDiscard() {
            // does nothing, subclasses will override this
            return null;
        }

        /// <summary>
        /// Called to determine where in the Unity hierarchy the view should be attached.
        /// Useful for views that need to be spawned on a canvas or inside another view.
        /// </summary>
        public virtual Transform DetermineHost() => Services.Get<ViewHosting>().DefaultHost();
        
        // === Assets management
        
        /// <summary>
        /// Register asset handle 
        /// </summary>
        public void RegisterReleasableHandle(IReleasableReference releasableReference) {
            _releasableReferences ??= new(4);
            _releasableReferences.Add(releasableReference);
        }

        // === Display

        public override string ToString() => "View:" + ID;
        
        // === Helpers
        
        public void ReleaseReleasable() {
            if (_releasableReferences == null) {
                return;
            }
            foreach (var reference in _releasableReferences) {
                reference.Release();
            }
            _releasableReferences.Clear();
        }
    }

    public abstract class View<T> : View, IView<T> where T : IModel {
        public T Target { get; private set; }

        protected sealed override void OnTargetAssigned() {
            Target = (T)GenericTarget;
        }

        protected sealed override void CleanReferences() {
            base.CleanReferences();
            Target = default;
        }

        protected override bool CanNestInside(View view) => view.GenericTarget is T;
    }
}