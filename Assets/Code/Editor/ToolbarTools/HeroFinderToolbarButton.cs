using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class HeroFinderToolbarButton : EditorToolbarButton {
        public const string ID = "X/1";

        public HeroFinderToolbarButton() {
            text = "Hero";
            tooltip = "Finds Hero on scene";
            clicked += OnClick;
        }

        void OnClick() {
            var scene = SceneView.lastActiveSceneView;
            if (scene == null) {
                throw new Exception($"{nameof(SceneView)}.{nameof(SceneView.lastActiveSceneView)} is {scene}");
            }

            var hero = Hero.Current?.VHeroController;
            if (hero == null) {
                throw new NullReferenceException("Hero not found.");
            }

            scene.LookAtDirect(hero.CameraPosition, Quaternion.LookRotation(hero.LookDirection), 1F);
            Selection.activeGameObject = hero.gameObject;
            scene.Repaint();
        }
    }
}