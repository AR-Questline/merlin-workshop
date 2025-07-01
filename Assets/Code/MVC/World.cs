using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.FSM.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.Profiling;
using Awaken.Utility.Threads;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.IL2CPP.CompilerServices;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UniversalProfiling;
using Debug = UnityEngine.Debug;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC {
    [Il2CppEagerStaticClassConstruction]
    public static class World {
        const int ModelsCapacity = 80_550;

        // === Fields
        static readonly List<Model> ModelsInOrder = new(ModelsCapacity);
        static readonly Dictionary<string, Model> ModelsById = new(ModelsCapacity);
        static readonly HierarchicalDictionary<Type, IModel> ModelsByType = new(920, 1, 1);

        static readonly Dictionary<IModel, View> MainViewsByModel = new(3400);
        static readonly MultiMap<IModel, View> ViewsByModel = new(3400, 1);
        static readonly MultiMap<IModel, IPresenter> PresentersByModel = new(20, 2);

        static uint s_modelsInOrderToRemove;

        //  === Profiling
        static readonly UniversalProfilerMarker MarkerVerifyAllInOrder = new("World.VerifyAllInOrder");
        
        // === Events
        [Il2CppEagerStaticClassConstruction]
        public static class Events {
            static readonly OnDemandCache<Type, Event<IModel, Model>> AddEvents = new(t => {
                ModelUtils.DebugCheckModelTypeForByTypeOperations(t);
                return new Event<IModel, Model>($"Added/{t.FullName}");
            });
            static readonly OnDemandCache<Type, Event<IModel, Model>> InitializedEvents = new(t => {
                ModelUtils.DebugCheckModelTypeForByTypeOperations(t);
                return new Event<IModel, Model>($"Initialized/{t.FullName}");
            });
            static readonly OnDemandCache<Type, Event<IModel, Model>> FullyInitializedEvents = new(t => {
                ModelUtils.DebugCheckModelTypeForByTypeOperations(t);
                return new Event<IModel, Model>($"FullyInitialized/{t.FullName}");
            });
            static readonly OnDemandCache<Type, Event<IModel, Model>> DiscardEvents = new(t => {
                ModelUtils.DebugCheckModelTypeForByTypeOperations(t);
                return new Event<IModel, Model>($"Discarded/{t.FullName}");
            });

            public static bool IsAddedEventRelevant(Type type) => AddEvents.Contains(type);
            public static bool IsInitializedEventRelevant(Type type) => InitializedEvents.Contains(type);
            public static bool IsFullyInitializedEventRelevant(Type type) => FullyInitializedEvents.Contains(type);
            public static bool IsDiscardEventRelevant(Type type) => DiscardEvents.Contains(type);
            public static Event<IModel, Model> ModelAdded<T>() => AddEvents[typeof(T)];
            public static Event<IModel, Model> ModelAdded(Type t) => AddEvents[t];
            public static readonly Event<IModel, Model> ModelAddedAnyType = new(nameof(ModelAddedAnyType));
            public static Event<IModel, Model> ModelInitialized<T>() => InitializedEvents[typeof(T)];
            public static Event<IModel, Model> ModelInitialized(Type t) => InitializedEvents[t];
            public static readonly Event<IModel, Model> ModelInitializedAnyType = new(nameof(ModelInitializedAnyType));
            public static Event<IModel, Model> ModelFullyInitialized<T>() => FullyInitializedEvents[typeof(T)];
            public static Event<IModel, Model> ModelFullyInitialized(Type t) => FullyInitializedEvents[t];
            public static readonly Event<IModel, Model> ModelFullyInitializedAnyType = new(nameof(ModelFullyInitializedAnyType));
            public static Event<IModel, Model> ModelDiscarded<T>() => DiscardEvents[typeof(T)];
            public static Event<IModel, Model> ModelDiscarded(Type t) => DiscardEvents[t];
            public static readonly Event<IModel, Model> ModelDiscardedAnyType = new(nameof(ModelDiscardedAnyType));
        }

        // === Properties

        public static Services Services { get; private set; }
        public const string PrefabPath = "Prefabs/MapViews";

        public static EventSystem EventSystem { get; private set; }

        // === Services Constructor

        public static void AssignServices(Services services) {
            if (Services != null) {
                throw new InvalidOperationException("Services have already been initialized, it can happen only once in app lifetime");
            }

            AllocateCommonByType();
            Services = services;
            EventSystem = new EventSystem();
            Services.Register(EventSystem);
        }

        public static void EDITOR_RuntimeReset() {
            Services = null;
            EventSystem = null;
            ModelsInOrder.Clear();
            ModelsById.Clear();
            ModelsByType.Clear();
            MainViewsByModel.Clear();
            ViewsByModel.Clear();
            PresentersByModel.Clear();
            s_modelsInOrderToRemove = 0;
        }
        
        // === Domains Management
        public static void DropDomain(Domain domain) {
            Log.Marking?.Warning($"Dropping {domain.Name}");
            
            // Get all models that are not elements and belong to domain or any child-domain in reverse order (start discarding from latest models)
            var models = AllInOrder()
                .Where(m => m is not IElement && m.CurrentDomain.IsChildOf(domain, true))
                .Reverse()
                .ToList();
            foreach (var model in models) {
                try {
                    model.DiscardFromDomainDrop();
                } catch (Exception e) {
                    Log.Critical?.Error($"DOMAIN ERROR! Exception below happened while discarding model: {model.ID}");
                    Debug.LogException(e);
                    
                    string summary = "DOMAIN ERROR! Model discard failed";
                    string description = $"Discard failed for: {model.ID} while dropping domain: {domain.FullName}";
                    AutoBugReporting.SendAutoReport(summary, description);
                    DomainErrorPopup.Display();
                }
            }
            Services.UnregisterDomainBoundServices(domain);

            // --- Inform of scene domains being dropped
            var sceneService = World.Services.Get<SceneService>();
            
            if (Domain.Scene(sceneService.AdditiveSceneRef).IsChildOf(domain, true)) {
                World.EventSystem.Trigger(
                    SceneLifetimeEvents.Get, 
                    SceneLifetimeEvents.Events.AfterDomainDrop, 
                    new SceneLifetimeEventData(false, sceneService.AdditiveSceneRef));
            }
            
            if (sceneService.MainDomain.IsChildOf(domain, true)) {
                World.EventSystem.Trigger(
                    SceneLifetimeEvents.Get, 
                    SceneLifetimeEvents.Events.AfterDomainDrop, 
                    new SceneLifetimeEventData(true, sceneService.MainSceneRef));
            }
        }

        // === Tracking models
        
        public static T Add<T>(T model) where T : Model {
            ThreadSafeUtils.AssertMainThread();
            var modelTypeHierarchy = ModelUtils.ModelHierarchyTypes(model);
            // index the new model
            model = Register(model, modelTypeHierarchy);
            // initialize it
            try {
                Initialize(model, modelTypeHierarchy);
            } catch (Exception e) {
                Log.Critical?.Error($"Exception happened for Model {LogUtils.GetDebugName(model)}");
                AutoBugReporting.SendAutoReport("World.Add Exception",
                    $"Exception happened for Model {LogUtils.GetDebugName(model)} \n {e}");
                throw;
            } finally {
                model.StoppedInitialization();
            }
            // trigger generic added events
            if (model) {
                foreach (var type in modelTypeHierarchy) {
                    if (Events.IsAddedEventRelevant(type)) {
                        model.Trigger(Events.ModelAdded(type), model);
                    }
                }
                model.Trigger(Events.ModelAddedAnyType, model);
            }
            // done
            return model;
        }

        public static T Register<T>(T model, Type[] modelTypeHierarchy) where T : Model {
            ThreadSafeUtils.AssertMainThread();
            // ensure we have an ID
            bool idAssigned = false;
            if (string.IsNullOrEmpty(model.ID)) {
                model.AssignID(Services);
                idAssigned = true;
            }
            if (ModelsById.TryGetValue(model.ID, out Model existingModel)) {
                Log.Critical?.Error($"Model with ID {model.ID} already exists in the world " +
                                    $"\nnew model info: '{LogUtils.GetDebugName(model)}' of domain: '{model?.CurrentDomain.FullName}'" +
                                    $"\nexisting model info: '{LogUtils.GetDebugName(existingModel)}' of domain: {existingModel?.CurrentDomain.FullName} " +
                                    $"\nnew id was assigned: {idAssigned}");
            }
            // register in the initialization-order list
            ModelsInOrder.Add(model);
            // register for the ID
            ModelsById[model.ID!] = model;
            // register for the concrete type and all supertypes
            ModelsByType.Add(modelTypeHierarchy, model);
            ProfilerValues.ModelsCounters.Add();
            // return the newly added object
            return model;
        }

        /// <summary>
        /// Initializes model for the first time.
        /// </summary>
        static void Initialize<T>(T model, Type[] modelTypeHierarchy) where T : Model {
            ThreadSafeUtils.AssertMainThread();
            if (model.IsInitialized) {
                Log.Critical?.Error("Element: '" + model.ID + "' was already initialized and is being added to world again!");
            }

            // register domain
            Domain modelDefaultDomain = model.DefaultDomain;
            if (modelDefaultDomain.Name.IsNullOrWhitespace()) {
                Log.Critical?.Error("Model: '" + model.ID + "' created with invalid domain!");
            }

            model.SetDomain(modelDefaultDomain);

            model.Initialize();
            SpawnViews(model);

            // Trigger generic initialized events
            foreach (Type implementedType in modelTypeHierarchy) {
                if (Events.IsInitializedEventRelevant(implementedType)) {
                    model.Trigger(Events.ModelInitialized(implementedType), model);
                }
            }
            model.Trigger(Events.ModelInitializedAnyType, model);

            model.InitializeAllElements();
            model.MarkAsFullyInitialized();
        }

        /// <summary>
        /// Restores a model that came from saved data. This is usually
        /// the same as Initialize-ing it, but there are lifecycle
        /// differences with elements, and some models have an OnRestore()
        /// that is different from OnInitialize().
        /// </summary>
        public static void Restore<T>(T model) where T : Model {
            ThreadSafeUtils.AssertMainThread();

            model.Restore();
            SpawnViews(model);
            
            // Trigger generic initialized events
            var implementedTypes = ModelUtils.ModelHierarchyTypes(model);
            foreach (Type implementedType in implementedTypes) {
                if (Events.IsInitializedEventRelevant(implementedType)) {
                    model.Trigger(Events.ModelInitialized(implementedType), model);
                }
            }
            model.Trigger(Events.ModelInitializedAnyType, model);
            model.InitializeNewElements();

            // we don't do anything about elements here - they will go
            // through their own Restore() later, restore themselves
            // and spawn their views on their own            
            // ... so we're ready now
        }

        static void SpawnViews(Model model) {
            foreach (SpawnsView spawn in AttributesCache.GetSpawnViews(model)) {
                SpawnView(model, spawn);
            }
        }

        public static void Remove<T>(T model) where T : Model {
            ThreadSafeUtils.AssertMainThread();
            s_modelsInOrderToRemove++;
            if (!ModelsById.Remove(model.ID)) {
                Log.Critical?.Error($"Model {model.ID} was not found in ModelsById dictionary");
                s_modelsInOrderToRemove--;
            }
            ModelsByType.Remove(model.GetType(), model);
            model.ClearDomain();
            ProfilerValues.ModelsCounters.Remove();
        }

        public static void NotifyIdChanged(string previousID, Model model) {
            if (ModelsById[previousID] != model) {
                throw new Exception("Model ID change failed, model was not found under previous ID");
            }
            ModelsById.Remove(previousID);
            ModelsById[model.ID] = model;
        }

        static void AllocateCommonByType() {
            // By hierarchy
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(NpcAnimatorState)), 110, 0);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(ARAnimatorState<NpcElement, NpcAnimatorSubstateMachine>)), 110, 0);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(HeroAnimatorState)), 95, 0);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(ARAnimatorState<Hero, HeroAnimatorSubstateMachine>)), 95, 0);
            // By own size
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(AttackGeneric)), 1, 4_300);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(Location)), 1, 2_700);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(NpcNone)), 1, 1800);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(SearchAction)), 1, 1700);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(BlockHold)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(PoiseBreakFront)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(PoiseBreakBack)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(PoiseBreakBackLeft)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(PoiseBreakBackRight)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(WyrdConversion)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(GetHit)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(CustomGesticulate)), 1, 900);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(TimeDependent)), 1, 700);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(AliveStats)), 1, 650);
            ModelsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(typeof(HealthElement)), 2, 650);
        }

        // === Querying models

        [ContractAnnotation("=> notnull")]
        public static T Only<T>() where T : class {
            ThreadSafeUtils.AssertMainThread();
            var tType = typeof(T);
            ModelUtils.DebugCheckModelTypeForByTypeOperations(tType);

            var modelsEnumerator = ModelsByType.Enumerate(tType);
            if (!modelsEnumerator.MoveNext()) {
                throw new ArgumentException($"There is no registered model of type {tType.FullName}.");
            }
            T model = modelsEnumerator.Current as T;
            if (modelsEnumerator.MoveNext()) {
                throw new ArgumentException($"There is more than one model registered of type {tType.FullName}.");
            }
            return model;
        }

        public static T Any<T>() where T : class {
            ThreadSafeUtils.AssertMainThread();
            var tType = typeof(T);
            ModelUtils.DebugCheckModelTypeForByTypeOperations(tType);

            var modelsEnumerator = ModelsByType.Enumerate(tType);
            return modelsEnumerator.MoveNext() ? modelsEnumerator.Current as T : null;
        }

        public static T Any<T>(Func<T, bool> predicate) where T : class {
            ThreadSafeUtils.AssertMainThread();
            var tType = typeof(T);
            ModelUtils.DebugCheckModelTypeForByTypeOperations(tType);

            foreach (var model in ModelsByType.Enumerate(tType)) {
                if (predicate((T)model)) {
                    return (T)model;
                }
            }
            return null;
        }

        public static bool HasAny<T>() where T : class {
            var tType = typeof(T);
            ModelUtils.DebugCheckModelTypeForByTypeOperations(tType);

            var modelsEnumerator = ModelsByType.Enumerate(tType);
            return modelsEnumerator.MoveNext();
        }
        
        public static ModelsSet<T> All<T>() where T : class, IModel => All<T>(typeof(T));

        public static ModelsSet<T> All<T>(Type type) where T : class, IModel {
            ThreadSafeUtils.AssertMainThread();
            ModelUtils.DebugCheckModelTypeForByTypeOperations(type);
#if DEBUG || AR_DEBUG
            if (!typeof(T).IsAssignableFrom(type)) {
                throw new AggregateException($"Can not get models of type {type.FullName} as {typeof(T).FullName}");
            }
#endif
            if (ModelsByType.TryGetValue(type, out var values)) {
                return new ModelsSet<T>(values);
            } else {
                return ModelsSet<T>.Empty;
            }
        }

        public static T ByID<T>(string id) where T : class {
            return ByID(id) as T;
        }

        public static Model ByID(string id) {
            return ModelsById.GetValueOrDefault(id);
        }
        
        public static IEnumerable<T> AllInOrder<T>() where T : class {
            ThreadSafeUtils.AssertMainThread();
            return AllInOrder().OfType<T>();
        }

        public static List<Model> AllInOrder() {
            VerifyAllInOrder();
            return ModelsInOrder;
        }

        public static T LastOrNull<T>() where T : class, IModel {
            ThreadSafeUtils.AssertMainThread();
            var modelsEnumerator = ModelsByType.Enumerate(typeof(T));

            if (!modelsEnumerator.MoveNext()) {
                return null;
            }
            var firstModel = modelsEnumerator.Current as T;
            if (!modelsEnumerator.MoveNext()) {
                return firstModel;
            }

            var i = ModelsInOrder.Count - 1;
            T model = null;
            while (i >= 0 && (model == null || model.HasBeenDiscarded)) {
                model = ModelsInOrder[i] as T;
                --i;
            }
            return model;
        }

        public static void VerifyAllInOrder() {
            if (s_modelsInOrderToRemove == 0) {
                return;
            }
            MarkerVerifyAllInOrder.Begin();
            int modelsCount = ModelsInOrder.Count;
            if (s_modelsInOrderToRemove + s_modelsInOrderToRemove > modelsCount) {
                VerifyFrom(0);
            } else {
                VerifyEnding();
            }
            s_modelsInOrderToRemove = 0;
            MarkerVerifyAllInOrder.End();

            // Shortpath for removing a few models. It is more likely that they will be at the end of the list
            void VerifyEnding() {
                uint modelsToFind = s_modelsInOrderToRemove;
                for (int i = modelsCount - 1; i >= 0; i--) {
                    if (ModelsInOrder[i].WasDiscarded) {
                        if (--modelsToFind == 0) {
                            VerifyFrom(i);
                            return;
                        }
                    }
                }
                Log.Critical?.Error("Removing model from ModelsInOrder list failed!");
                VerifyFrom(0);
            }
            
            void VerifyFrom(int start) {
                int move = 0;
                for (int i = start; i < modelsCount; i++) {
                    var model = ModelsInOrder[i];
                    if (model.WasDiscarded) {
                        move++;
                    } else if (move > 0) {
                        ModelsInOrder[i - move] = ModelsInOrder[i];
                    }
                }
                ModelsInOrder.RemoveRange(modelsCount - move, move);
            }
        }

        // === Tracking views

        public static View MainViewFor(Model model) {
            MainViewsByModel.TryGetValue(model, out View view);
            return view;
        }

        public static T View<T>(Model model) where T : class, IView {
            return ViewsFor(model).FirstOrDefaultCastNonAlloc<View, T>(_ => true);
        }

        public static HashSet<View> ViewsFor(IModel model) {
            return ViewsByModel.GetValues(model, true);
        }
        
        public static void BindView(IModel model, View view, bool isMainView = false, bool removeAutomatically = false, bool mountInPlace = true) {
            // Could use HasBeenDiscarded but will be concentrated into single case/message
            if (model.IsBeingDiscarded) {
                Log.Important?.Error($"Model {model.ID} starts discarding process, can not bind view to this model");
                return;
            }
            if (model.WasDiscarded) {
                Log.Important?.Error($"Model {model.ID} was discarded, can not bind view to this model");
                return;
            }
            
            view.Initialize(Services, model);
            if (isMainView) {
                if (MainViewsByModel.ContainsKey(model)) {
                    throw new InvalidOperationException($"Model '{model.ID}' already has a main view: {MainViewsByModel[model].name}, but a new view tried to add itself as main: {view.name}");
                }
                MainViewsByModel[model] = view;
            }
            ViewsByModel.Add(model, view);
            
            ProfilerValues.BoundViewsCounters.Add();

            if (removeAutomatically) {
                EventSystem.ModalListenTo(EventSystem.PatternForModel(model), Model.Events.BeingDiscarded, view, _ => {
                    if (view) {
                        view.Discard();
                    }
                });
            }
            if (mountInPlace) {
                view.Mount(view.transform.parent);
            }
        }

        public static void RemoveView(View view) {
            MainViewsByModel.TryGetValue(view.GenericTarget, out View mainView);
            if (view == mainView) {
                MainViewsByModel.Remove(view.GenericTarget);
            }

            ViewsByModel.Remove(view.GenericTarget, view);

            ProfilerValues.BoundViewsCounters.Remove();
        }

        public static void ReBind(IWithRecyclableView model, RetargetableView view, bool isMainView = false, bool removeAutomatically = false) {
            if (!view) {
                Log.Important?.Error($"View {view.ID} was destroyed, can not re bind this view to the model {model.ID}");
                return;
            }
            if (view.DestroyWaitingFor != null) {
                Log.Important?.Error($"View {model.ID} is destroying, can not re bind this view to the model {model.ID}");
                return;
            }
            // Don't rebind to the same target
            if (model == view.RecyclableTarget) {
                return;
            }
            if (view.GenericTarget != null) {
                RemoveView(view);
            }

            // Could use HasBeenDiscarded but will be concentrated into single case/message
            if (model.IsBeingDiscarded) {
                Log.Important?.Error($"Model {model.ID} starts discarding process, can not bind view to this model");
                return;
            }
            if (model.WasDiscarded) {
                Log.Important?.Error($"Model {model.ID} was discarded, can not bind view to this model");
                return;
            }
            
            view.ReTarget(model);
            if (isMainView) {
                if (MainViewsByModel.ContainsKey(model)) {
                    throw new InvalidOperationException($"Model '{model.ID}' already has a main view: {MainViewsByModel[model].name}, but a new view tried to add itself as main: {view.name}");
                }
                MainViewsByModel[model] = view;
            }
            ViewsByModel.Add(model, view);
            
            ProfilerValues.BoundViewsCounters.Add();

            if (removeAutomatically) {
                EventSystem.ModalListenTo(EventSystem.PatternForModel(model), Model.Events.BeingDiscarded, view, _ => {
                    if (view) {
                        view.Discard();
                    }
                });
            }
        }

        // === Spawning views 
        
        public static View SpawnView(IModel model, Type type, bool isMainView = false, bool removeAutomatically = true, Transform forcedParent = null, bool forcedToBeFirstChild = false) {
            ThreadSafeUtils.AssertMainThread();
            return SpawnViewFromPrefab(model, type, null, isMainView, removeAutomatically, forcedParent, forcedToBeFirstChild);
        }

        public static T SpawnView<T>(IModel model, bool isMainView = false, bool removeAutomatically = true,
            Transform forcedParent = null, bool forcedToBeFirstChild = false) where T : View {
            ThreadSafeUtils.AssertMainThread();
            return SpawnViewFromPrefab(model, typeof(T), null, isMainView, removeAutomatically, forcedParent, forcedToBeFirstChild) as T;
        }

        public static T SpawnViewFromPrefab<T>(IModel model, GameObject prefab, bool isMainView = false,
            bool removeAutomatically = true, Transform forcedParent = null, bool forcedToBeFirstChild = false) where T : View {
            ThreadSafeUtils.AssertMainThread();
            return SpawnViewFromPrefab(model, typeof(T), prefab, isMainView, removeAutomatically, forcedParent, forcedToBeFirstChild) as T;
        }

        public static View SpawnViewFromPrefab(IModel model, Type type, GameObject prefab, bool isMainView = false,
            bool removeAutomatically = true, Transform forcedParent = null, bool forcedToBeFirstChild = false) {
            ThreadSafeUtils.AssertMainThread();
            if (model is IWithRecyclableView recyclableListItem && forcedParent && forcedParent.TryGetComponent<IRecyclableViewsManager>(out var listManager)) {
                if (prefab == null) {
                    prefab = ExtractPrefab(type);
                }
                if (prefab.TryGetComponent<RetargetableView>(out var retargetableView)) {
                    listManager.AddElement(recyclableListItem, retargetableView);
                    return null;
                }
            }
            // spawn the view
            View view = MakeViewOfType(type, forcedParent, prefab, forcedToBeFirstChild);
            view.name = $"{model.ID}[{type.Name}]";
            // bind it to the model
            BindView(model, view, isMainView, removeAutomatically, mountInPlace: false);
            // mount it at the right place in the Unity hierarchy
            view.Mount(forcedParent != null ? forcedParent : view.DetermineHost());
            // done!
            return view;
        }

        static void SpawnView(Model model, SpawnsView spawnInfo) {
            const BindingFlags GetForceParentPropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            
            Transform forcedParent = null;
            if (!string.IsNullOrWhiteSpace(spawnInfo.forceParentMember)) {
                // TODO: More debug checks!!!
                var forceParentMember = model.GetType()
                    .GetProperty(spawnInfo.forceParentMember, GetForceParentPropertyFlags);
                forcedParent = forceParentMember?.GetValue(model) as Transform;
            }
            SpawnView(model, spawnInfo.view, spawnInfo.isMainView, forcedParent: forcedParent);
        }

        static View MakeViewOfType(Type type, Transform parent, GameObject prefab, bool forcedToBeFirstChild) {
            bool hasNoPrefab = AttributesCache.GetNoPrefab(type);
            
            ProfilerValues.SpawnedViewsCounters.Add();

            if (hasNoPrefab) {
                GameObject gob = new GameObject(type.Name);
                return gob.AddComponent(type) as View;
            } else {
                if (prefab == null) {
                    prefab = ExtractPrefab(type);
                }

                // instantiate it
                GameObject viewGob = UnityEngine.Object.Instantiate(prefab, parent);
                if (forcedToBeFirstChild && parent != null) {
                    viewGob.transform.SetAsFirstSibling();
                }
                View view = viewGob.GetComponent<View>();
                
                if (view == null) {
                    throw new ArgumentException($"The view prefab for type {type} is missing an actual View component.");
                }

                return view;
            }
        }

        public static GameObject ExtractPrefab(Type type, string resourcesPath = PrefabPath) {
            UsesPrefab prefabInfo = AttributesCache.GetUsesPrefab(type);
            string prefabName = prefabInfo?.prefabName ?? type.Name;
            // find the prefab
            string prefabPath = Path.Combine(resourcesPath, prefabName);
            try {
                var prefab = Resources.Load<GameObject>(prefabPath);
                if (prefab == null) {
                    throw new ArgumentException($"No view prefab found under name: '{prefabName}'.");
                }
                return prefab;
            } catch (Exception e) {
                Debug.LogException(e);
                return null;
            }
        }

        public static string ResourcesPrefabPath(string prefabName) => Path.Combine(PrefabPath, prefabName);
        
        // === Presenters
        
        public static T Presenter<T>(Model model) where T : class, IPresenter {
            return PresentersFor(model).FirstOrDefaultCastNonAlloc<IPresenter, T>(_ => true);
        }
        
        public static HashSet<IPresenter> PresentersFor(IModel model) {
            return PresentersByModel.GetValues(model, true);
        }

        public static T BindPresenter<T>(IModel model, T presenter, Action onInitialized = null, bool removeAutomatically = true) where T : IPresenter {
            PresentersByModel.Add(model, presenter);
            
            if (removeAutomatically) {
                EventSystem.ListenTo(EventSystem.PatternForModel(model), Model.Events.BeingDiscarded, presenter, _ => {
                    presenter.Discard();
                });
            }
            
            presenter.Initialize(model, onInitialized);
            return presenter;
        }
        
        public static void RemovePresenter(IPresenter presenter) {
            PresentersByModel.Remove(presenter.GenericModel, presenter);
        }
        
#if UNITY_EDITOR
        [MenuItem("TG/Optimization/Log World Allocated Collections Size")]
        static void LogWorldAllocatedCollectionsSize() {
            Debug.Log($"ModelsInOrder count: {ModelsInOrder.Count}, capacity: {ModelsInOrder.Capacity}");
            Debug.Log($"ModelsById count: {ModelsById.Count}");
            Debug.Log($"ModelsByType count: {ModelsByType.Count}. Median outer count: {GetMedianOuterCount(ModelsByType.Values)}; Median own count: {GetMedianOwnCount(ModelsByType.Values)}");
            Debug.Log($"MainViewsByModel count: {MainViewsByModel.Count}");
            Debug.Log($"ViewsByModel count: {ViewsByModel.Count}. Median count in hashset: {GetMedianCount(ViewsByModel.Values)}");
            Debug.Log($"PresentersByModel count: {PresentersByModel.Count}. Median count in hashset: {GetMedianCount(PresentersByModel.Values)}");

            static float GetMedianCount<T>(ICollection<HashSet<T>> collections) {
                var lengths = new NativeList<int>(16, ARAlloc.Temp);
                foreach (var collection in collections) {
                    lengths.Add(collection.Count);
                }

                if (lengths.Length == 0) {
                    return 0;
                }

                lengths.Sort();
                int mid = lengths.Length / 2;
                float median = lengths.Length % 2 == 0 ? (lengths[mid - 1] + lengths[mid]) / 2f : lengths[mid];
                lengths.Dispose();
                return median;
            }

            static float GetMedianOuterCount<T>(ICollection<StructList<List<T>>> collections) {
                var lengths = new NativeList<int>(16, ARAlloc.Temp);
                foreach (var collection in collections) {
                    lengths.Add(collection.Count);
                }

                if (lengths.Length == 0) {
                    return 0;
                }

                lengths.Sort();
                int mid = lengths.Length / 2;
                float median = lengths.Length % 2 == 0 ? (lengths[mid - 1] + lengths[mid]) / 2f : lengths[mid];
                lengths.Dispose();
                return median;
            }

            static float GetMedianOwnCount<T>(ICollection<StructList<List<T>>> collections) {
                var lengths = new NativeList<int>(16, ARAlloc.Temp);
                foreach (var collection in collections) {
                    lengths.Add(collection[0].Count);
                }

                if (lengths.Length == 0) {
                    return 0;
                }

                lengths.Sort();
                int mid = lengths.Length / 2;
                float median = lengths.Length % 2 == 0 ? (lengths[mid - 1] + lengths[mid]) / 2f : lengths[mid];
                lengths.Dispose();
                return median;
            }
        }

        [MenuItem("TG/Optimization/Log Models by type by outer count")]
        static void LogModelsByTypeByOuter() {
            Debug.Log($"ModelsByType count: {ModelsByType.Count}");

            foreach (var (type, values) in ModelsByType.OrderByDescending(l => l.Value.Count)) {
                Debug.Log($"For type: {TypeName(type)} there are {values.Count}/{values.Capacity} outer values and {values[0].Count}/{values[0].Capacity} inner values");
            }
        }

        [MenuItem("TG/Optimization/Log Models by type by own count")]
        static void LogModelsByTypeByOwn() {
            Debug.Log($"ModelsByType count: {ModelsByType.Count}");

            foreach (var (type, values) in ModelsByType.OrderByDescending(l => l.Value[0].Count)) {
                Debug.Log($"For type: {TypeName(type)} there are {values.Count}/{values.Capacity} outer values and {values[0].Count}/{values[0].Capacity} inner values");
            }
        }

        [MenuItem("TG/Optimization/Log Elements by type by outer count")]
        static void LogModelElementsByTypeByOuter() {
            var modelsWithElements = ModelsInOrder.Where(m => ElementsByType(m) != ModelElements.EmptyElementsByType).ToArray();
            Debug.Log($"Models count: {ModelsInOrder.Count} with elements count: {modelsWithElements.Length}");

            var allModelElements = modelsWithElements.SelectMany(model => ElementsByType(model).Select((kvp) => (model.GetType(), kvp.Key, kvp.Value)));

            foreach (var (ownerType, type, values) in allModelElements.OrderByDescending(l => l.Value.Count)) {
                Debug.Log($"Owner: {TypeName(ownerType)} For type: {TypeName(type)} there are {values.Count}/{values.Capacity} outer values and {values[0].Count}/{values[0].Capacity} inner values");
            }

            static HierarchicalDictionary<Type, IModel> ElementsByType(Model model) {
                return ModelElements.Access.ElementsByType(Model.Access.ModelElements(model));
            }
        }

        [MenuItem("TG/Optimization/Log Elements by type by own count")]
        static void LogModelElementsByTypeByOwn() {
            var modelsWithElements = ModelsInOrder.Where(m => ElementsByType(m) != ModelElements.EmptyElementsByType).ToArray();
            Debug.Log($"Models count: {ModelsInOrder.Count} with elements count: {modelsWithElements.Length}");

            var allModelElements = modelsWithElements.SelectMany(model => ElementsByType(model).Select((kvp) => (model.GetType(), kvp.Key, kvp.Value)));

            foreach (var (ownerType, type, values) in allModelElements.OrderByDescending(l => l.Value[0].Count)) {
                Debug.Log($"Owner: {TypeName(ownerType)} For type: {TypeName(type)} there are {values.Count}/{values.Capacity} outer values and {values[0].Count}/{values[0].Capacity} inner values");
            }

            static HierarchicalDictionary<Type, IModel> ElementsByType(Model model) {
                return ModelElements.Access.ElementsByType(Model.Access.ModelElements(model));
            }
        }

        [MenuItem("TG/Optimization/Log Elements count")]
        static void LogModelElementsCount() {
            var modelsWithElements = ModelsInOrder.Where(m => Elements(m) != null).ToArray();
            Debug.Log($"Models count: {ModelsInOrder.Count} with elements count: {modelsWithElements.Length}");

            foreach (var model in modelsWithElements.OrderByDescending(m => Elements(m).Count)) {
                var elements = Elements(model);
                var elementsByType = ElementsByType(model);
                Debug.Log($"Owner: {TypeName(model.GetType())} there are {elements.Count}/{elements.Capacity} elements and by type {elementsByType.Count}");
            }

            static List<Element> Elements(Model model) {
                return ModelElements.Access.Elements(Model.Access.ModelElements(model));
            }

            static HierarchicalDictionary<Type, IModel> ElementsByType(Model model) {
                return ModelElements.Access.ElementsByType(Model.Access.ModelElements(model));
            }
        }

        static string TypeName(Type type) {
            if (type == null) {
                return null;
            }
            var sb = new System.Text.StringBuilder();
            var name = type.Name;
            if (!type.IsGenericType) {
                return name;
            }
            var genericIndex = name.IndexOf('`');
            if (genericIndex == -1) {
                return type.FullName;
            }

            sb.Append(name.Substring(0, genericIndex));
            sb.Append("<");
            var genericArguments = type.GetGenericArguments();
            for (int i = 0; i < genericArguments.Length; i++) {
                sb.Append(TypeName(genericArguments[i]));
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(">");
            return sb.ToString();
        }
#endif
    }
}
