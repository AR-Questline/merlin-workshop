using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEditor;
#endif

namespace Awaken.Utility.Animators {
    public abstract class CustomBlendTree : StateMachineBehaviour {
#if UNITY_EDITOR
        [SerializeField] BlendTree blendTree;
#endif
        [SerializeField, TableList] protected Child[] _children;

        [UnityEngine.Scripting.Preserve] public Child GetChild(Motion motion) => GetChild(motion, child => child.Motion);
        [UnityEngine.Scripting.Preserve] public Child GetChild(string parameter) => GetChild(parameter, child => child.Parameter);
        [UnityEngine.Scripting.Preserve] public Child GetChild(Vector2 position) => GetChild(position, child => child.Position);
        Child GetChild<T>(T test, Func<Child, T> getter) {
            for (int i = 0; i < _children.Length; i++) {
                if (Equals(getter(_children[i]), test)) {
                    return _children[i];
                }
            }
            return null;
        }

#if UNITY_EDITOR
        [Button]
        public void Upload() {
            var motions = new ChildMotion[_children.Length];

            for (int i = 0; i < motions.Length; i++) {
                motions[i] = new ChildMotion {
                    motion = _children[i].Motion,
                    directBlendParameter = _children[i].Parameter,
                    timeScale = _children[i].Speed,
                    mirror = _children[i].Mirror,
                };
            }

            blendTree.children = motions;
            EditorUtility.SetDirty(blendTree);
        }
#endif
        [Serializable]
        public class Child {
            int? _hash;
            int Hash => _hash ??= Animator.StringToHash(parameter);
            
            [SerializeField, HideLabel, VerticalGroup("motion"), TableColumnWidth(120)] Motion motion;
            [SerializeField, HideLabel, VerticalGroup("parameter"), TableColumnWidth(120)] string parameter;
            [SerializeField, HideLabel, VerticalGroup("position"), TableColumnWidth(100, false)] Vector2 position;
            [SerializeField, HideLabel, VerticalGroup("speed"), TableColumnWidth(50, false)] float speed = 1;
            [SerializeField, HideLabel, VerticalGroup("mirror"), TableColumnWidth(50, false)] bool mirror;

            
            public string Parameter => parameter;
            public Motion Motion => motion;
            public Vector2 Position => position;
            public float Speed => speed;
            public bool Mirror => mirror;
            
            public void SetWeight(Animator animator, float percent) {
                animator.SetFloat(Hash, percent);
            }

            public float GetWeight(Animator animator) {
                return animator.GetFloat(Hash);
            }
        }
    }
}