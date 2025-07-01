using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class CustomDeathController : MonoBehaviour {
        [BoxGroup("Ragdoll"), SerializeField]
        bool allowRagdoll = true;
        [BoxGroup("Ragdoll"), SerializeField, ShowIf(nameof(allowRagdoll))]
        bool canRagdollWhenAlive = true;
        [BoxGroup("Ragdoll"), SerializeField, ShowIf(nameof(allowRagdoll)), Tooltip("If true, will always fall in ragdoll on death. If false, will only fall in ragdoll if other Death Behaviour forces the ragdoll")]
        bool shouldRagdollOnDeath = true;
        [BoxGroup("Ragdoll"), SerializeField, HideIf(nameof(allowRagdoll)), LabelText("Has Ragdoll Components"), Tooltip("Check if this prefab contains ragdoll components (all standard humans and monsters do)")] 
        bool addDeathRagdollBehaviour = true;
        public bool keepBody = true;
        
        public bool AddRagdollBehaviour => allowRagdoll || addDeathRagdollBehaviour;
        public bool CanRagdollWhenAlive => allowRagdoll && canRagdollWhenAlive;
        public bool ShouldRagdollOnDeath => allowRagdoll && shouldRagdollOnDeath;

        public void SetRagdollOnDeath(bool? allowRagdoll, bool? canRagdollWhenAlive, bool? shouldRagdollOnDeath) {
            if (allowRagdoll.HasValue) {
                this.allowRagdoll = allowRagdoll.Value;
            }
            if (canRagdollWhenAlive.HasValue) {
                this.canRagdollWhenAlive = canRagdollWhenAlive.Value;
            }
            if (shouldRagdollOnDeath.HasValue) {
                this.shouldRagdollOnDeath = shouldRagdollOnDeath.Value;
            }
        }

        public IEnumerable<IDeathBehaviour> GetAdditionalBehaviours() {
            foreach (var behaviour in GetComponents<IDeathBehaviour>()) {
                yield return behaviour;
            }
        }
        
#if UNITY_EDITOR
        [ShowInInspector, ValueDropdown(nameof(PossibleBehavioursString)), HorizontalGroup("Add")]
        string _addNew;

        [Button, HorizontalGroup("Add", 70f), DisableIf(nameof(CannotAdd))]
        void Add() {
            Type type = PossibleBehaviours.FirstOrDefault(t => t.Name == _addNew);
            if (type == null) return;
            gameObject.AddComponent(type);
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        bool CannotAdd => string.IsNullOrWhiteSpace(_addNew);
        string[] PossibleBehavioursString => PossibleBehaviours.Select(b => b.Name).ToArray();
        Type[] PossibleBehaviours {
            get {
                Type[] allBehaviours = UnityEditor.TypeCache.GetTypesDerivedFrom<IDeathBehaviour>()
                    .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t))
                    .ToArray();
                return allBehaviours;
            }
        }
#endif
    }
}