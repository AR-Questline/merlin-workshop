using System;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Statues {
    public class StatuePropSelector : MonoBehaviour {
        [HideLabel] public new string name;
        [HideLabel, ListDrawerSettings(CustomAddFunction = nameof(NewProp))] public Prop[] props = Array.Empty<Prop>();

        public void Set(in SerializableGuid guid) {
            foreach (ref readonly var prop in props.RefIterator()) {
                if (prop.root) {
                    prop.root.SetActive(prop.guid == guid);
                }
            }
        }
        
        Prop NewProp() {
            return new Prop {
                guid = SerializableGuid.NewGuid(),
            };
        }
        
        [Serializable]
        public struct Prop {
            [HideInInspector] public SerializableGuid guid;
            public string name;
            public GameObject root;
        }
    }
}