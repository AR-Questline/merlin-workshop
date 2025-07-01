using System;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Animations {
    public class EditorAnimationPlayer : EditorWindow {
        [SerializeField] GameObject body;
        [SerializeField] GameObject[] clothes = Array.Empty<GameObject>();
        [SerializeField] AnimationClip clip;
        [SerializeField] float time;
        [SerializeField] float speed = 1;

        GameObject _instance;
        float _clipLength;

        void OnDisable() {
            DestroyImmediate(_instance);
            _instance = null;
        }
        
        void OnGUI() {
            EditorGUI.BeginChangeCheck();
            body = EditorGUILayout.ObjectField(GUIUtils.Content("Body"), body, typeof(GameObject), false) as GameObject;
            for (int i = 0; i < clothes.Length; i++) {
                EditorGUILayout.BeginHorizontal();
                clothes[i] = EditorGUILayout.ObjectField(GUIUtils.Content($"Cloth {i}"), clothes[i], typeof(GameObject), false) as GameObject;
                if (GUILayout.Button("X", GUILayout.Width(30))) {
                    ArrayUtils.RemoveAt(ref clothes, i);
                    --i;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("+", GUILayout.Width(30))) {
                ArrayUtils.Add(ref clothes, null);
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck()) {
                RefreshInstance();
            }
            EditorGUILayout.Space();
            clip = EditorGUILayout.ObjectField(GUIUtils.Content("Clip"), clip, typeof(AnimationClip), false) as AnimationClip;
            if (clip) {
                time = EditorGUILayout.Slider(GUIUtils.Content("Time"), time, 0, _clipLength);
                speed = EditorGUILayout.Slider(GUIUtils.Content("Speed"), speed, -3, 3);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(nameof(Rewind))) {
                    Rewind();
                }
                if (GUILayout.Button(nameof(Pause))) {
                    Pause();
                }
                if (GUILayout.Button(nameof(Play))) {
                    Play();
                }
                EditorGUILayout.EndHorizontal();
            } else {
                time = 0;
                speed = 0;
                _clipLength = 0;
            }
        }

        void Update() {
            if (clip) {
                _clipLength = clip.length;
                time += Time.deltaTime * speed;
                time = (time + _clipLength * 1000) % _clipLength;
                if (_instance) {
                    clip.SampleAnimation(_instance, time);
                }
                Repaint();
            }
        }

        void RefreshInstance() {
            DestroyImmediate(_instance);
            if (body == null) {
                _instance = null;
                return;
            }
            _instance = Instantiate(body);
            _instance.hideFlags = HideFlags.DontSave;
            foreach (var cloth in clothes) {
                if (cloth) {
                    ClothStitcher.Stitch(cloth, _instance);
                }
            }
        }
        
        void Rewind() {
            speed = speed switch {
                -1 => -2,
                -2 => -3,
                _ => -1,
            };
        }
        
        void Pause() {
            speed = 0;
        }
        
        void Play() {
            speed = speed switch {
                1 => 2,
                2 => 3,
                _ => 1,
            };
        }

        [MenuItem("TG/Animations/Editor Animation Player")]
        static void Open() {
            GetWindow<EditorAnimationPlayer>().Show();
        }
    }
}