using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.AutoGuards {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Spawns guards automatically on given spots.")]
    public class AutoGuardSpawningAttachment : MonoBehaviour, IAttachmentSpec {
        [ShowInInspector, PropertyOrder(0)]
        [InfoBox("You can add / modify spawn points by changing child game objects", GUIAlwaysEnabled = true)]
        Transform[] SpawnTransforms => transform.GetComponentsInChildren<Transform>().Where(t => t != transform && t.childCount == 0).ToArray();

        [SerializeField, TemplateType(typeof(LocationTemplate)), PropertyOrder(1)]
        TemplateReference[] guardTemplates = Array.Empty<TemplateReference>();

        [SerializeField, TemplateType(typeof(FactionTemplate)), PropertyOrder(2)]
        TemplateReference factionTemp;

        public IEnumerable<Vector3> SpawnPoints => SpawnTransforms.Select(t => t.position);
        public IEnumerable<LocationTemplate> GuardTemplates => guardTemplates.Select(g => g.Get<LocationTemplate>());
        public FactionTemplate FactionTemplate => factionTemp.Get<FactionTemplate>();
        
        public Element SpawnElement() {
            return new AutoGuardSpawning();
        }

        public bool IsMine(Element element) {
            return element is AutoGuardSpawning;
        }

        void OnDrawGizmosSelected() {
            if (Hero.Current != null) {
                foreach (var point in SpawnPoints.Select(Ground.SnapNpcToGround)) {
                    float score = AutoGuardSpawning.CalculatePointScore(point);
                    float colorLerpT = score;
                    Color color = new(colorLerpT, colorLerpT, colorLerpT, 1f);
                    Gizmos.color = color;
                    Gizmos.DrawSphere(point + Vector3.up * 0.5f, 0.5f);
                }
            } else {
                Gizmos.color = Color.cyan;
                foreach (var pos in SpawnPoints.Select(Ground.SnapNpcToGround)) {
                    Gizmos.DrawSphere(pos + Vector3.up * 0.5f, 0.5f);
                }
            }
        }
    }
}