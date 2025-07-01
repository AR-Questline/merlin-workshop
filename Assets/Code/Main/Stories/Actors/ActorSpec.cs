using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Actors {
    public class ActorSpec : MonoBehaviour {
        [LocStringCategory(Category.Actor)]
        public LocString displayName;
        public bool showNameInDialogues = true;
        public bool isFake;
        [ShowIf(nameof(isFake))]
        public int fakeIndex;
        
        [SerializeField, Tags(TagsCategory.Barks)]
        public string[] tags = new string[0];
        
        [Tooltip("Check this box and complete the field below if you want to use a custom bark configuration—one " +
                 "that is manually created and not intended for automatic updates")]
        public bool useCustomBarkGraph;
        [SerializeField]
        public BarkConfig barkConfig;
        
        [SerializeField, HideInInspector] 
        string guid;
        
#if UNITY_EDITOR
        [field: SerializeField]
        public string[] AvailableBookmarks { get; set; } = Array.Empty<string>();
#endif
        IEnumerable<ActorState> States => GetComponentsInChildren<ActorState>();
        
        [ShowInInspector, ReadOnly]
        public string Guid {
            get {
                if (string.IsNullOrWhiteSpace(guid)) {
                    guid = System.Guid.NewGuid().ToString();
                }

                return guid;
            }
            set => guid = value;
        }
        
        public string GetPath() {
            return GetNameRecursive(gameObject.transform.parent, gameObject.name);

            string GetNameRecursive(Transform parent, string fullPath) {
                if (parent == null || parent.parent == null || this.gameObject.transform.parent.name == "Actors") {
                    return fullPath;
                }

                fullPath = parent.name + "/" + fullPath;
                return GetNameRecursive(parent.parent, fullPath);
            }
        }

        public Actor Create(string stateId) {
            Actor actor = new(Guid, displayName.ToString(), showNameInDialogues, barkConfig, isFake, fakeIndex);
            ActorState state = States.FirstOrDefault(s => s.Id == stateId);
            state?.Apply(ref actor);
            return actor;
        }

        void OnValidate() {
            var actorsRegister = GetComponentInParent<ActorsRegister>();
            if (actorsRegister && actorsRegister.AllActors.Count(a => a.Guid == Guid) > 1) {
                Guid = System.Guid.NewGuid().ToString();
            }
        }
    }
}