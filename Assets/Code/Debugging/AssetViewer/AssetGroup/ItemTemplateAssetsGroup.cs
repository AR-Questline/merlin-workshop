using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Debugging.AssetViewer.AssetGroup {
    public class ItemTemplateAssetsGroup : PreviewAssetsGroup<ItemTemplate> {
        [SerializeField] TextMeshPro label;
        [SerializeField] bool spawnEmpty = true;
        
        public override void SpawnTemplate(ItemTemplate template, Vector3 position) {
            try {
                if (template.DropPrefab.IsSet) {
                    var handle = template.DropPrefab.Get().LoadAsset<GameObject>();
                    handle.OnComplete(h => {
                        GameObject instance = GameObject.Instantiate(h.Result, position, Quaternion.Euler(0,180,0) * transform.rotation);
                        AddLabel(instance, template);
                    });
                    return;
                }
            } catch (Exception e) {
                Log.Important?.Warning(e.ToString());
            }

            SpawnDefault(template, position);
        }

        void AddLabel(GameObject go, ItemTemplate template) {
            if (label == null) {
                return;
            }

            go.name = name;
            var newLabel = Instantiate(label, go.transform);
            newLabel.transform.rotation = transform.rotation;
            newLabel.text = $"{template.ItemName}\n{template.name}";
        }

        void SpawnDefault(ItemTemplate template, Vector3 position) {
            if (spawnEmpty) {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = position;
                go.transform.rotation = Quaternion.Euler(0, 180, 0) * transform.rotation;
                go.GetComponent<Collider>().isTrigger = true;
                AddLabel(go, template);
            }
        }
    }
}