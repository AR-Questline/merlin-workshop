using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine.Serialization;

namespace Awaken.TG.Editor.EnemyCounter {
    public class EnemyCounterWindow : OdinEditorWindow {

        const string SpecificEnemyAcrossScenesMode = "Specific Enemy Across Scenes";
        const string DifferentEnemiesInSceneMode = "Different Enemies in Scene";
        
        [TabGroup(SpecificEnemyAcrossScenesMode)]
        public LocationReference locationToFind = new() {targetTypes = TargetType.Templates};
        
        [TabGroup(SpecificEnemyAcrossScenesMode)]    
        [TableList(ShowIndexLabels = true, ShowPaging = true)]
        public List<ResultRow> resultsEnemyCountByScenes = new ();
        
        [TabGroup(DifferentEnemiesInSceneMode)]
        public SceneReference sceneToSearchIn;
        
        [TabGroup(DifferentEnemiesInSceneMode)]
        [TableList(ShowIndexLabels = true, ShowPaging = true)]
        public List<ResultRow> resultEnemiesInScene = new ();
        
        [Serializable]
        public class ResultRow { 
            [TableColumnWidth(200)]
            public string key;
            [TableColumnWidth(100)]
            public int count;
            
            public ResultRow(string key, int count) {
                this.key = key;
                this.count = count;
            }
        }
        
        [MenuItem("TG/Design/Enemy Counter")]
        public static void ShowWindow() {
            var window = CreateWindow<EnemyCounterWindow>("Enemy Counter");
            window.Show();
        }
        
        [Button, TabGroup(SpecificEnemyAcrossScenesMode)]
        void SearchSpecificEnemyAcrossScenes() {
            resultsEnemyCountByScenes = SpecificEnemySearcher.SearchSpecificEnemyAcrossScenes(locationToFind);
        }
        
        [Button, TabGroup(DifferentEnemiesInSceneMode)]
        void SearchEnemiesInScene() {
            resultEnemiesInScene = SceneEnemySearcher.SearchEnemiesInScene(sceneToSearchIn);
        }
    }
}
