using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.AssetManager
{
public class AssetManagerCharacters{
        [AssetList(CustomFilterMethod = "PrefabComponentFilter", Path = "/3DAssets/")]
        public List<GameObject> PrefabList = new List<GameObject>();

        [ShowIf("spawnType", SpawnEnum.Points)]
        public List<GameObject> SpawnPointList = new List<GameObject>();

        [EnumPaging, LabelWidth(120)] public SpawnEnum spawnType;
        
        [HorizontalGroup("Conditions"), BoxGroup("Conditions/Left/Options"), VerticalGroup("Conditions/Left"), LabelWidth(120)] 
        public bool flipHero = true;
        [VerticalGroup("Conditions/Left"), BoxGroup("Conditions/Left/Options"), LabelWidth(120)] 
        public bool groundPlane = true;
        [VerticalGroup("Conditions/Right"), BoxGroup("Conditions/Right/Spawn"), ShowIf("spawnType", SpawnEnum.Points), LabelWidth(120)]
        public bool combatPositions = true;
        [VerticalGroup("Conditions/Left"), BoxGroup("Conditions/Left/Options"), LabelWidth(120)] 
        public bool spotlight;
        [VerticalGroup("Conditions/Right"), BoxGroup("Conditions/Right/Spawn"), ShowIf("spawnType", SpawnEnum.Points), LabelWidth(120)]
        public bool lookAtHero = true;
        [VerticalGroup("Conditions/Right"), BoxGroup("Conditions/Right/Spawn"), ShowIf("spawnType", SpawnEnum.Circle), LabelWidth(120)] 
        public float radius = 120f;
        [VerticalGroup("Conditions/Right"), BoxGroup("Conditions/Right/Spawn"), ShowIf("spawnType", SpawnEnum.Grid), LabelWidth(120)] 
        public int col = 20;
        [VerticalGroup("Conditions/Right"), BoxGroup("Conditions/Right/Spawn"), ShowIf("spawnType", SpawnEnum.Grid), LabelWidth(120)] 
        public int row = 20;
        [VerticalGroup("Conditions/Right"), BoxGroup("Conditions/Right/Spawn"), ShowIf("spawnType", SpawnEnum.Grid), LabelWidth(120)] 
        public int spacing = 20;

        [HorizontalGroup("Spawn"), VerticalGroup("Spawn/Left"), Button(ButtonSizes.Medium)]
        void Spawn(){
            SpawnPrefabs();
            CalculateMesh();
            
            // assetManagerCamera.GetAllTargets();
            // assetManagerCamera.enabled = false;
            // assetManagerCamera.enabled = true;
        }    
        
        [VerticalGroup("Spawn/Right")]
        [Button(ButtonSizes.Medium)]
        void Clear(){
            ClearAll();
            // assetManagerCamera.targets.Clear();
        }

        [Button(ButtonSizes.Medium)]
        void Calibrator(){
            SpawnColorCalibrator();
        }

        Bounds _modelBounds;

        [BoxGroup("Additional components"), LabelWidth(120)] public GameObject groundPlanePrefab; 
        [BoxGroup("Additional components"), LabelWidth(120)] public GameObject spotLightPrefab;
        [BoxGroup("Additional components"), LabelWidth(120)] public GameObject colorCalibratorPrefab;
        
        [BoxGroup("Stats"), DisplayAsString, LabelWidth(120)] [UnityEngine.Scripting.Preserve] public int _totalSubmeshes;
        [BoxGroup("Stats"), DisplayAsString, LabelWidth(120)] [UnityEngine.Scripting.Preserve] public int _totalVertex;
        [BoxGroup("Stats"), DisplayAsString, LabelWidth(120)] [UnityEngine.Scripting.Preserve] public int _totalTriangles;

        // [HideInInspector] public AssetManagerCamera assetManagerCamera = GameObject.Find("Main Camera").GetComponent<AssetManagerCamera>();
        
        public enum SpawnEnum{
            Points,
            Grid,
            Circle
        }
        // ==============
        void SpawnPrefabs(){
            switch (spawnType){
                case SpawnEnum.Points:
                    SpawnOnPoints();
                    break;
                case SpawnEnum.Grid:
                    SpawnOnGrid(col, row);
                    break;
                case SpawnEnum.Circle:
                    SpawnOnCircle(PrefabList, radius);
                    break;
            }
        }
        
        void SpawnOnPoints(){
            Object.DestroyImmediate(GameObject.Find("AssetManager")); 
            
            GameObject mainGo = new GameObject("AssetManager");
            
            PrefabList = new List<GameObject>(PrefabList.OrderBy(p => p.name));
            
            if (combatPositions){
                AddCombatSpawnPoints();
            }

            if (PrefabList.Count <= SpawnPointList.Count){
                for (int i = 0; i < PrefabList.Count; i++) {
                    GameObject instance = Object.Instantiate(PrefabList[i]);
                    if (instance != null){
                        instance.transform.position = SpawnPointList[i].transform.position;
                        instance.transform.SetParent(SpawnPointList[i].transform);

                        if (lookAtHero && i >= 1){
                            instance.transform.LookAt(new Vector3(0, mainGo.transform.position.y,0));
                        }else{
                            if (flipHero){
                                instance.transform.rotation = Quaternion.Euler(0, 180, 0);
                            }else{
                                instance.transform.rotation = Quaternion.Euler(0, 0, 0);
                            }
                        }
                        
                        if (groundPlane && groundPlanePrefab != null){
                            GameObject groundPlaneInstance = Object.Instantiate(groundPlanePrefab);
                            if (groundPlaneInstance != null){
                                groundPlaneInstance.transform.position = SpawnPointList[i].transform.position;
                                groundPlaneInstance.transform.SetParent(SpawnPointList[i].transform);
                            }
                        }
                    }
                    SpawnSpotlight(i, SpawnPointList[i].transform.position, instance);
                }
            }else{
                SpawnPointList = new List<GameObject>();
                Log.Important?.Info("Set more spawn points!");
            }
        }
        
        void SpawnOnGrid(int columns, int rows){
            Object.DestroyImmediate(GameObject.Find("AssetManager"));
            
            GameObject mainGo = new GameObject("AssetManager");
            
            PrefabList = new List<GameObject>(PrefabList.OrderBy(p => p.name));
            
            int i = 0;
            for (int z = 0; z <= rows; z++){
                for (int x = 0; x <= columns; x++){
                    if (i < PrefabList.Count){
                        Vector3 position = new Vector3(x - columns * 0.5f, mainGo.transform.localPosition.y, z - columns * 0.5f) * spacing;
                        GameObject instance = Object.Instantiate(PrefabList[i]);
                        if (instance != null){
                            instance.transform.position = position;
                            instance.transform.SetParent(mainGo.transform);

                            if (flipHero){
                                instance.transform.rotation = Quaternion.Euler(0, 180, 0);
                            }

                            if (groundPlane && groundPlanePrefab != null){
                                GameObject groundPlaneInstance = Object.Instantiate(groundPlanePrefab);
                                if (groundPlaneInstance != null){
                                    groundPlaneInstance.transform.position = instance.transform.position;
                                    groundPlaneInstance.transform.SetParent(instance.transform);
                                }
                            }
                                
                            SpawnSpotlight(i, instance.transform.position, instance);
                        }
                        i++;
                    }
                }
            }
        }
        
        void AddCombatSpawnPoints(){
            GameObject hero1 = new GameObject("Hero1");
            GameObject enemy1 = new GameObject("Enemy1");
            GameObject enemy2 = new GameObject("Enemy2");
            GameObject enemy3 = new GameObject("Enemy3");
            GameObject enemy4 = new GameObject("Enemy4");
            GameObject enemy5 = new GameObject("Enemy5");

            hero1.transform.position = new Vector3(0, 0, 0);
            enemy1.transform.position = new Vector3(0, 0, 30);
            enemy2.transform.position = new Vector3(15, 0, 24);
            enemy3.transform.position = new Vector3(-15, 0, 24);
            enemy4.transform.position = new Vector3(28, 0, 15);
            enemy5.transform.position = new Vector3(-28, 0, 15);
                
            SpawnPointList = new List<GameObject>{hero1, enemy1, enemy2, enemy3, enemy4, enemy5};

            foreach (var go in SpawnPointList){
                go.transform.SetParent(GameObject.Find("AssetManager").transform);
            }
        }

        void SpawnOnCircle(List<GameObject> list, float circleRadius){
            Object.DestroyImmediate(GameObject.Find("AssetManager")); 
            
            GameObject mainGo = new GameObject("AssetManager");
            
            PrefabList = new List<GameObject>(PrefabList.OrderBy(p => p.name));

            if (PrefabList.Count > 0){
                for (int i = 0; i < list.Count; i++){
                    float radians = 2 * Mathf.PI / list.Count * i;
                    float vertical = Mathf.Sin(radians);
                    float horizontal = Mathf.Cos(radians);
                    var spawnDir = new Vector3(horizontal, 0, vertical);
                    Vector3 centralPoint = mainGo.transform.position;
                    Vector3 position = centralPoint + spawnDir * circleRadius;

                    GameObject instance = Object.Instantiate(PrefabList[i]);
                    if (instance != null){
                        instance.transform.position = position;
                        instance.transform.LookAt(centralPoint);
                        instance.transform.SetParent(mainGo.transform);

                        if (groundPlane && groundPlanePrefab != null){
                            GameObject groundPlaneInstance = Object.Instantiate(groundPlanePrefab);
                            if (groundPlaneInstance != null){
                                groundPlaneInstance.transform.position = instance.transform.position;
                                groundPlaneInstance.transform.SetParent(instance.transform);
                            }
                        }
                        SpawnSpotlight(i, instance.transform.position, instance);
                    }
                }
            }
        }

        void SpawnSpotlight(int i, Vector3 position, GameObject parent){
            if (spotlight && spotLightPrefab != null){
                _modelBounds = TransformBoundsUtil.FindBounds(PrefabList[i].transform, false);
                float height = _modelBounds.size.y;
                Vector3 pos = new Vector3(0, 0, 0);
                pos.y = Mathf.Lerp(18f, 40f, height / 30f);

                GameObject spotlightInstance = Object.Instantiate(spotLightPrefab);
                if (spotlightInstance != null){
                    spotlightInstance.transform.position = position + pos;
                    spotlightInstance.transform.rotation = Quaternion.Euler(90, 0, 0);
                    spotlightInstance.transform.SetParent(parent.transform);
                }
            }
        }
        
        void SpawnColorCalibrator(){
            if (GameObject.Find("ColorCalibrator") == null && colorCalibratorPrefab != null){
                GameObject instance = Object.Instantiate(colorCalibratorPrefab);
                if (instance != null){
                    instance.transform.position = new Vector3(-10, 0, 0);
                }
            }
        }
        
        void CalculateMesh() {
            _totalSubmeshes = 0;
            _totalVertex = 0;
            _totalTriangles = 0;

            GameObject go = GameObject.Find("AssetManager");

            // Mesh
            foreach(var component in go.GetComponentsInChildren(typeof(MeshFilter))) {
                if (component.name != "PrefabPreview_GroundPlane(Clone)"){
                    
                    var m = (MeshFilter) component;
                    var sharedMesh = m.sharedMesh;
                    
                    var subMeshCount = sharedMesh.subMeshCount;
                    var vertexCount = sharedMesh.vertexCount;
                    var trianglesCount = sharedMesh.triangles.Length / 3;

                    _totalSubmeshes += subMeshCount;
                    _totalVertex += vertexCount;
                    _totalTriangles += trianglesCount;
                    
                    // Log.When(LogType.Important)?.Info("Mesh: " + m.name + " has " + trianglesCount + " triangles");
                }
            }
            
            //Skinned mesh
            foreach (var component in go.GetComponentsInChildren(typeof(SkinnedMeshRenderer))){

                var sm = (SkinnedMeshRenderer) component;
                var sharedMesh = sm.sharedMesh;

                var subMeshCount = sharedMesh.subMeshCount;
                var vertexCount = sharedMesh.vertexCount;
                var trianglesCount = sharedMesh.triangles.Length / 3;

                _totalSubmeshes += subMeshCount;
                _totalVertex += vertexCount;
                _totalTriangles += trianglesCount;
                
                // Log.When(LogType.Important)?.Info("Skinned Mesh: " + sm.name + " has " + trianglesCount + " triangles");
            }
        }

        public void ClearAll(){
            Object.DestroyImmediate(GameObject.Find("AssetManager"));
            Object.DestroyImmediate(GameObject.Find("ColorCalibrator"));
            SpawnPointList = new List<GameObject>();
            _totalSubmeshes = 0;
            _totalVertex = 0;
            _totalTriangles = 0;
        }
    }
}
