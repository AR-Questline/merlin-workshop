using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using Awaken.TG.Assets;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Debugging.AssetViewer.AssetGroup {
    public class ClothingAssetsGroup: PreviewAssetsGroup<LocationTemplate> {
        [SerializeField] TextMeshPro label;
        [SerializeField] LocationTemplate male;
        [SerializeField] LocationTemplate female;
        [SerializeField, FolderPath] string maleClothingPath;
        [SerializeField, FolderPath] string femaleClothingPath;

        [SerializeField] List<GameObject> loaded = new();
        public override void SpawnTemplate(LocationTemplate template, Vector3 position) { }
#if UNITY_EDITOR

        public override void SpawnTemplates() {
            var maleClothes = GetClothesByCategory(maleClothingPath);
            var femaleClothes = GetClothesByCategory(femaleClothingPath);
            
            int gridIter = 0;
            var slots = grid.GetSlots(transform, maleClothes.Count + femaleClothes.Count);
            GenerateSet(femaleClothes, slots, ref gridIter, female);
            GenerateSet(maleClothes, slots, ref gridIter, male);
        }
    
        List<ARAssetReference[]> GetClothesByCategory(string clothingPath) {
            
            return SelectAllPrefabFolders(clothingPath)
                   .Select(GetAllClothesInFolder)
                   .Select(ReferenceClothes)
                   .ToList();
            
            
            static IEnumerable<string> GetAllClothesInFolder(string folder) => Directory.EnumerateFiles(folder + "/");
    
            static IEnumerable<string> SelectAllPrefabFolders(string clothingPath) {
                return AssetDatabase.GetSubFolders(clothingPath)
                                    .SelectMany(AssetDatabase.GetSubFolders)
                                    .Where(s => s.Contains("Prefab"));
            }

            static ARAssetReference[] ReferenceClothes(IEnumerable<string> paths) {
                return paths.Where(path => !path.Contains("Preview"))
                    .Select(AssetDatabase.AssetPathToGUID)
                    .Select(guid => new ARAssetReference(guid))
                    .ToArray();
            }
        }
    
    
        void GenerateSet(List<ARAssetReference[]> allGenderClothes, List<Vector3> slots, ref int gridIter, LocationTemplate locationTemplate) {
            foreach (ARAssetReference[] femaleCloth in allGenderClothes) {
                GenerateCategory(femaleCloth.WhereNotNull(), slots, gridIter, locationTemplate);
                gridIter++;
            }
        }

        async void GenerateCategory(IEnumerable<ARAssetReference> femaleCloth, List<Vector3> slots, int gridIter, LocationTemplate locationTemplate) {
            var loc = locationTemplate.SpawnLocation(slots[gridIter]);

            string[] category = null;
            LocationAssetsGroup.ModifyGameObject(locationTemplate, loc, LocationAssetsGroup.DisableAI);
            await UniTask.WaitUntil(() => loc.TryGetElement<NpcElement>() != null && loc.TryGetElement<NpcClothes>() != null);
            var spawnedNPC = loc.Element<NpcElement>();

            foreach (ARAssetReference cloth in femaleCloth) {
                (GameObject go, bool _) = await spawnedNPC.NpcClothes.EquipTask(cloth);
                category ??= go?.name.Split('_');
                lock (loaded) {
                    loaded.Add(go);
                }
            }

            
            if (category != null) {
                LocationAssetsGroup.ModifyGameObject(locationTemplate, loc, v => AddLabel(v.gameObject, locationTemplate == male, category[1] + " " + category[2]));
            }

            loc.MoveAndRotateTo(loc.Coords, Quaternion.Euler(0,180,0) * transform.rotation);
        }

        void AddLabel(GameObject go, bool gender, string category) {
            if (label == null) {
                return;
            }
    
            string newLabelText = $"{(gender ? "Male" : "Female")}\n{category}";
            
            go.name = newLabelText;
            var newLabel = Instantiate(label, go.transform);
            newLabel.transform.rotation = transform.rotation;
            newLabel.text = newLabelText;
        }
#endif
    }
}
