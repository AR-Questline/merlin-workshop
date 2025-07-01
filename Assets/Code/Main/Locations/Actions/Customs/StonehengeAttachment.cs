using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Main.Locations.Actions.Customs {
    public class StonehengeAttachment : MonoBehaviour, IAttachmentSpec {
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.Weapons)]
        public ShareableARAssetReference shockWavePrefab;
        public Volume volume;
        public GameObject[] stoneCircle = Array.Empty<GameObject>();
        public StoryBookmark storyRef;

        public LocationReference pedestalRef;
        
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference _druid1, _druid2;


        public Transform druid1Pos, druid2Pos;

        public LocationTemplate Druid1 => _druid1.Get<LocationTemplate>();
        public LocationTemplate Druid2 => _druid2.Get<LocationTemplate>();
        
        public Element SpawnElement() => new Stonehenge();

        public bool IsMine(Element element) => element is Stonehenge;
    }
}