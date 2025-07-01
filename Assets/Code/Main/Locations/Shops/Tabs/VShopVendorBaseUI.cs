using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    [UsesPrefab("Shop/VShopVendorBaseUI")]
    public class VShopVendorBaseUI : View<ShopVendorBaseUI>, IEmptyInfo {
        [SerializeField] Transform itemsHost;
        [SerializeField] public EventReference sellSfx;
        [SerializeField] public EventReference cantAffordSfx;
        
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
            emptyInfo.Setup(ContentGroups);
        }
        
        public void PlaySellSfx() {
            if (!sellSfx.IsNull) {
                // RuntimeManager.PlayOneShot(sellSfx);
            }
        }

        public void PlayCantAffordSfx() {
            // RuntimeManager.PlayOneShot(!cantAffordSfx.IsNull
            //     ? cantAffordSfx
            //     : CommonReferences.Get.AudioConfig.StrongNegativeFeedbackSound);
        }
    }
}