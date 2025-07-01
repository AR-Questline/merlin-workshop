using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
using Awaken.Utility;
using Sirenix.OdinInspector;

namespace Awaken.TG
{
    public static class MapGridGameobjectsGenerator
    {
        public static int MapXCount = 8192;
        public static int MapYCount = 8192;
        public static int SectorXCount = 4;
        public static int SectorYCount = 4;
        public static int TileXCount = 4;
        public static int TileYCount = 4;
        public static bool CreatePrefabs = false;
        static private GameObject parentGO;
        [MenuItem("TG/Map/CreateMapGrid")]
        static void GenerateGrid()
        {
            parentGO = new GameObject("MapGrid");
            for(int i = 1; i <= SectorXCount; i++)
            {
                for(int j = 1; j <= SectorYCount; j++)
                {
                    GenerateSector(parentGO.transform,i,j);
                }
            }
        }
        static GameObject GenerateSector(Transform parent, int x, int y)
        {
            int sectorXSize = MapXCount / SectorXCount;
            int sectorYSize = MapYCount / SectorYCount;

            GameObject sector = new GameObject($"Sector_{x}_{y}");
            sector.transform.parent = parent;
            sector.transform.localRotation = Quaternion.identity;
            sector.transform.localPosition = new Vector3(sectorXSize * (x-0.5f-SectorXCount/2), 0, sectorYSize * (y-0.5f-SectorYCount/2));
            
            sector.AddComponent<MapGridGizmos>();
            
            for(int i = 1; i <= TileXCount; i++)
            {
                for(int j = 1; j <= TileYCount; j++)
                {
                    GenerateTile(sector.transform,i,j);
                }
            }
            return sector;
        }
        static GameObject GenerateTile(Transform parent, int x, int y) 
        {
            int tileXSize = MapXCount / SectorXCount / TileXCount;
            int tileYSize = MapYCount / SectorYCount / TileYCount;

            GameObject tile = new GameObject($"{parent.name}_Tile_{x}_{y}");
            tile.transform.parent = parent;
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localPosition = new Vector3(tileXSize * (x-0.5f-TileXCount/2), 0, tileYSize * (y-0.5f-TileYCount/2));
            
            tile.AddComponent<MapGridGizmos>();
            if (CreatePrefabs) CreatePrefab(tile, $"{parent.name}/");
            return tile;
        }

        static void CreatePrefab(GameObject go, string pathEnd)
        {
            Scene scene = SceneManager.GetActiveScene();
            string path = scene.path;
            path = path.Remove(path.Length - 6);
            path = $"{path}/MapGrid/{pathEnd}";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{path}/{go.name}.prefab", InteractionMode.AutomatedAction);
        }
    }
}
