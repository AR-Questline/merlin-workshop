using System.Linq;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class UnlockItemSkin : Element<Item>, IRefreshedByAttachment<UnlockItemSkinAttachment> {
        public override ushort TypeForSerialization => SavedModels.UnlockItemSkin;
        
        UnlockItemSkinAttachment _spec;
        bool _isValid = true;
        
        public void InitFromAttachment(UnlockItemSkinAttachment spec, bool isRestored) {
            _spec = spec;
            
            if (_spec.SkinItems.IsNullOrEmpty()) {
                Log.Critical?.Error($"Attempted to use an invalid {nameof(UnlockItemSkinAttachment)} setup with no SkinItem template assigned. Item: " + LogUtils.GetDebugName(ParentModel), _spec);
                _isValid = false;
                return;
            }
            
            if (_spec.AllowedActionType.IsNullOrEmpty()) {
                Log.Critical?.Error($"{nameof(UnlockItemSkinAttachment)} has no allowed {nameof(ItemActionType)} set and will not function properly. Item: " + LogUtils.GetDebugName(ParentModel), _spec);
                _isValid = false;
            }
        }

        protected override void OnInitialize() {
            if (!_isValid) {
                return;
            }
            
            ParentModel.ListenTo(Item.Events.BeforeActionPerformed, UnlockSkin, this);
        }

        void UnlockSkin(ItemActionEvent args) {
            if (!_spec.AllowedActionType.Contains(args.ActionType)) {
                return;
            }

            for (int i = 0; i < _spec.SkinItems.Length; i++) {
                var skinItem = _spec.SkinItems[i];
                if (skinItem == null) { 
                    Log.Important?.Error($"Attempted to unlock a null skin item. Invalid {nameof(ItemTemplate)} setup. Item: " + LogUtils.GetDebugName(ParentModel), _spec);
                    continue;
                }
                
                Hero.Current?.HeroItems.AddToKnownItems(skinItem);
            }
        }
    }
}