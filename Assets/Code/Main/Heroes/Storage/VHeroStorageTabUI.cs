using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Storage {
    [UsesPrefab("Storage/" + nameof(VHeroStorageTabUI))]
    public class VHeroStorageTabUI : View<HeroStorageTabUI>, IEmptyInfo{
        [SerializeField] Transform itemsHost;
        [SerializeField] public EventReference putSfx;
        
        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;

        public Transform ItemsHost => itemsHost;
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;
        
        protected override void OnInitialize() {
            PrepareEmptyInfo();
        }
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups, LocTerms.EmptyStorageInfo.Translate(), LocTerms.EmptyStorageDesc.Translate());
        }
        
        public void PlayPutSfx() {
            if (!putSfx.IsNull) {
                //RuntimeManager.PlayOneShot(putSfx);
            }
        }
    }
}
