using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Views {
    [RequireComponent(typeof(ARFmodEventEmitter))]
    public abstract class VLocation : View<Location>, ICharacterView, IInteractableWithHeroProvider, IDamageable {
        static readonly HashSet<GameObject> VSGameObjectSet = new();

        public bool HasHealthBar => !Target.HasElement<HideHealthBar>() && Target.HasElement<IWithHealthBar>();
        public bool IsCharacter => Target.HasElement<NpcElement>();
        public ICharacter Character => Target.TryGetElement<NpcElement>();
        
        ARFmodEventEmitter _emitter;
        GameObject[] _machineOwners;
        Action _afterGatheredMachineOwners;

        public IInteractableWithHero InteractableWithHero => Target;
        
        // === Initialization
        protected override void OnInitialize() {
            gameObject.AddComponent<Variables>();
            _emitter = GetComponent<ARFmodEventEmitter>();
            Target.OnVisualLoaded(GatherVisualScripting);
        }

        // === Initialization Overrides
        protected override IEventMachine[] MyEventMachines() {
            LocationParent locationParent = GetComponentInParent<LocationParent>();
            return locationParent != null ? locationParent.GetComponentsInChildren<IEventMachine>(true) : GetComponentsInChildren<IEventMachine>();
        }

        protected override bool CanNestInside(View view) {
            if (view is VLocation other) {
                Log.Critical?.Error($"Location has more than one VLocation: {gameObject.name} and {other.gameObject.name}", this);
                return false;
            }
            return base.CanNestInside(view);
        }

        // === Public Methods
        public void TriggerVisualScriptingEvent(string action, params object[] parameters) {
            if (_machineOwners == null) {
                _afterGatheredMachineOwners += () => TriggerVisualScriptingEvent(action, parameters);
                return;
            }
            _machineOwners.ForEach(o => CustomEvent.Trigger(o, action, parameters));
        }
        
        // === Helpers
        protected void VSTriggerOnVisualLoaded() {
            VSGameObjectSet.Clear();
            foreach (var machine in MyEventMachines()) {
                var go = ((Component) machine).gameObject;
                if (VSGameObjectSet.Add(go)) {
                    SafeGraph.Trigger(VisualLoadedUnit.Hook, go);
                }
            }

            VSGameObjectSet.Clear();
        }
        
        [UnityEngine.Scripting.Preserve]
        Transform GetChildPosition(string tagToFind, string childToFind) {
            Transform[] gameObjects = gameObject.GetComponentsInChildren<Transform>(true);
            Transform tagTransform = gameObjects.FirstOrDefault(c => c.gameObject.CompareTag(tagToFind));
            if (tagTransform == null) {
                Transform headTransform = gameObjects.FirstOrDefault(t => t.name.Equals(childToFind));
                if (headTransform != null) {
                    return headTransform;
                }
            } else {
                return tagTransform;
            }

            Log.Important?.Error("Could not find bone '" + childToFind + "' in children of '" + gameObject.name + "'", gameObject);
            return transform;
        }

        protected void ChangeRenderLayer(GameObject instance) {
            Stack<Transform> moveTargets = new Stack<Transform>();
            moveTargets.Push(instance.transform);
            while (moveTargets.Count != 0) {
                Transform currentTarget = moveTargets.Pop();
                currentTarget.gameObject.layer = RenderLayers.Objects;
                foreach (Transform child in currentTarget) {
                    if (child.gameObject.layer == RenderLayers.Default) {
                        moveTargets.Push(child);
                    }
                }
            }
        }
        
        void GatherVisualScripting(Transform t) {
            var graphPointers = MyEventMachines()
                .Select(m => m.GetReference())
                .WhereNotNull();
            _machineOwners = graphPointers.Select(p => p.gameObject).Distinct().ToArray();
            
            _afterGatheredMachineOwners?.Invoke();
            _afterGatheredMachineOwners = null;
        }

        // === Animator Logic
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot, GameObject followObject = null, params FMODParameter[] eventParams) {
            EventReference eventRef = audioType.RetrieveFrom(Character);
            PlayAudioClip(eventRef, asOneShot, followObject, eventParams);
        }
        
        public void PlayAudioClip(EventReference eventReference, bool asOneShot, GameObject followObject = null, params FMODParameter[] eventParams) {
            if (followObject == null) {
                followObject = gameObject;
            }
            
            if (asOneShot) {
                FMODManager.PlayAttachedOneShotWithParameters(eventReference, followObject, _emitter, eventParams);
            } else {
                //_emitter.PlayNewEventWithPauseTracking(eventReference, eventParams);
            }
        }

        public void StopEmittingSounds() {
            if (_emitter != null) {
                //_emitter.Stop();
            }

            SalsaFmodEventEmitter salsa = GetComponentInChildren<SalsaFmodEventEmitter>();
            if (salsa != null) {
                //salsa.Stop();
            }
        }

        public enum LocationVisualSource : byte {
            FromScene,
            FromPrefab,
        }
    }
}