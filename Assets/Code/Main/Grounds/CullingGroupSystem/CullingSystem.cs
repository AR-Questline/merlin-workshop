using System;
using System.Collections.Generic;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UniversalProfiling;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public class CullingSystem : IDomainBoundService {
        static readonly UniversalProfilerMarker RequestAllUpdate = new("CullingSystem: RequestChunkUpdate");
        
        public Domain Domain => Domain.CurrentMainScene();
        public bool RemoveOnDomainChange() {
            Discard();
            return true;
        }

        int _multiCallBlocker = 0;
        Dictionary<ICullingSystemRegistree, Registree> _registry = new ();
        Dictionary<Type, BaseCullingGroup> _availableCullingGroups = new();

        public T GetCullingGroupInstance<T>() where T : BaseCullingGroup => (T) _availableCullingGroups[typeof(T)];

        CullingSystem() { }
        
        public static void Init() {
            var cs = new CullingSystem();
            ModelUtils.DoForFirstModelOfType<Hero>(h => h.ListenToLimited(GroundedEvents.AfterTeleported, _ => cs.InitOnceHeroReady(), cs), cs);
        }

        void InitOnceHeroReady() {
            World.Services.Register(this);
            AddCullingGroup(new LocationSpawnerCullingGroup());
            AddCullingGroup(new RegrowableCullingGroup());
            AddCullingGroup(new LocationCullingGroup());
            AddCullingGroup(new LargeLocationCullingGroup());
            AddCullingGroup(new CompassMarkerCullingGroup());
            AddCullingGroup(new LightControllerCullingGroup());
            
            ModelUtils.ListenToFirstModelOfType<Hero, IGrounded>(GroundedEvents.AfterTeleported, _ => RequestChunkUpdate().Forget(), this);
            RequestChunkUpdate().Forget();
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<ICullingSystemRegistreeModel>(), this, AutoRegister);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<ICullingSystemRegistreeModel>(), this, AutoUnregister);

            foreach (var model in World.All<ICullingSystemRegistreeModel>()) {
                AutoRegister(model);
            }
            CullingSystemRegistrator.RegisterWaiting(this);

#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Discard;
            UnityEditor.EditorApplication.quitting += Discard;
#else
            UnityEngine.Application.quitting += Discard;
#endif
        }

        public void PauseCurrentElementsDistanceBands() {
            foreach (var cullingGroup in _availableCullingGroups.Values) {
                cullingGroup.PauseCurrentElements();
            }
        }

        public void UnpauseAllCullingGroupsElements() {
            foreach (var cullingGroup in _availableCullingGroups.Values) {
                cullingGroup.UnpauseAllElements();
            }
        }

        void AddCullingGroup<TGroup>(TGroup group) where TGroup : BaseCullingGroup {
            if (!_availableCullingGroups.TryAdd(typeof(TGroup), group)) {
                Log.Important?.Error("[CullingSystem] Trying to add multiple groups of the same type. Its forbidden.");
            }
        }

        public void Discard() {
            _availableCullingGroups.Values.ForEach(x => x.Dispose());
            _availableCullingGroups.Clear();
            _registry.Clear();
        }

        void AutoRegister(IModel model) {
            if (model is ICullingSystemRegistreeModel group) {
                Register(group);
            }
        }

        void AutoUnregister(IModel model) {
            if (model is ICullingSystemRegistreeModel group) {
                Unregister(group);
            }
        }

        public void Register(ICullingSystemRegistree group) {
            if (_registry.ContainsKey(group)) return;
                
            Registree registree = group.GetRegistree();
            if (registree == null) return;
            _registry.Add(group, registree);
            registree.RegisterSelf();
        }
        
        public void Unregister(ICullingSystemRegistree group) {
            if (!_registry.TryGetValue(group, out Registree found)) return;
                
            found.UnregisterSelf();
            _registry.Remove(group);
        }
        
        async UniTaskVoid RequestChunkUpdate() {
            _multiCallBlocker++;
            await UniTask.DelayFrame(3);
            _multiCallBlocker--;
            if (_multiCallBlocker != 0) return;
            
            RequestAllUpdate.Begin();
            foreach (var group in _availableCullingGroups.Values) {
                group.ScheduleUpdateAll();
            }
            RequestAllUpdate.End();
        }

        public int GetDistanceBand(ICullingSystemRegistree registree) {
            if (_registry.TryGetValue(registree, out var r)) {
                return r.CurrentDistanceBand;
            } else {
                throw new Exception("Registree not present in culling groups");
            }
        }
        public int GetDistanceBandSafe(ICullingSystemRegistree registree, int fallback) {
            if (_registry.TryGetValue(registree, out var r)) {
                return r.CurrentDistanceBand;
            } else {
                return fallback;
            }
        }
    }
}
