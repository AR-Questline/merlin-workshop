using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Sketching {
    public partial class Sketch : Element<Item>, IItemAction {
        public override ushort TypeForSerialization => SavedModels.Sketch;

        public static readonly bool AllowGlobalRemoval = PlatformUtils.IsPS5;
        public static readonly bool IsGlobalCountLimited = PlatformUtils.IsPS5;
        public const int GlobalCountLimit = 80;

        public const int Width = 960;
        public const int Height = 720;

        public ItemActionType Type => ItemActionType.Use;
        [Saved] public LargeFileIndex SketchIndex { get; private set; }
        [Saved] public Vector3 CreatedPosition { get; private set; }
        [Saved] public SceneReference CreatedScene { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public Sketch() { }
        
        public Sketch(int sketchIndex) {
            SketchIndex = sketchIndex;
            CreatedPosition = Hero.Current.Coords;
            CreatedScene = World.Services.Get<SceneService>().ActiveSceneRef;
        }

        protected override void OnInitialize() {
            Hero.Current.ListenTo(IMerchant.Events.ItemSold, OnItemSold, this);
        }

        public void RemoveSketch() {
            World.Services.Get<LargeFilesStorage>().ForceRemoveFile(SketchIndex);
            SketchIndex = 0;
        }
        
        public void Submit() {
            World.Add(new SketchPopupUI(this));
        }

        public void AfterPerformed() {}
        public void Perform() {}
        public void Cancel() {}
        
        void OnItemSold(Item item) {
            if (item != ParentModel) {
                return;
            }
            
            if (AllowGlobalRemoval) {
                RemoveSketch();
            }
            ParentModel.Discard();
        }
    }
}
