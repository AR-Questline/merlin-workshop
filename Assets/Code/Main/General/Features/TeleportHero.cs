using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.General.Features {
    public class TeleportHero : MonoBehaviour {
        bool _visible;
        string _currentSetting;
        Transform[] _teleportPositions;
        Transform _current;
        
        Awaken.TG.Main.UI.Cursors.Cursor _cursor;

        void Awake() {
            _teleportPositions = transform.Cast<Transform>().ToArray();
            _current = _teleportPositions.FirstOrDefault();
            _cursor = FindAnyObjectByType<Awaken.TG.Main.UI.Cursors.Cursor>();
        }
        
        void ShowUI() {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<")) {
                _current = _teleportPositions.PreviousItem(_current, true);
            }
            GUILayout.Label(_current.name, GUILayout.Width(100));
            if (GUILayout.Button(">")) {
                _current = _teleportPositions.NextItem(_current, true);
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Teleport")) {
                TeleportHeroToPlace();
            }
            GUILayout.Space(10);
            GUILayout.Label("Press right/left arrow to change teleport target.\nPress space to teleport.");
        }

        void TeleportHeroToPlace() {
            Hero.Current.TeleportTo(_current.position);
        }

#if DEBUG
        void Update() {
            if (Input.GetKeyDown(KeyCode.F3)) {
                _visible = !_visible;
                _cursor.ToggleEnabled(!_visible);
            }

            if (_visible) {
                Cursor.visible = true;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.lockState = CursorLockMode.None;
                if (Input.GetKeyDown(KeyCode.RightArrow)) {
                    _current = _teleportPositions.PreviousItem(_current, true);
                }
                
                if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                    _current = _teleportPositions.NextItem(_current, true);
                }
                
                if (Input.GetKeyDown(KeyCode.Space)) {
                    TeleportHeroToPlace();
                }
            }
        }

        void OnGUI() {
            if (!_visible) return;
            GUILayout.Width(250);
            GUILayout.BeginVertical();
            GUILayout.Space(120);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Teleport to:");
            ShowUI();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }
#endif 
    }
}