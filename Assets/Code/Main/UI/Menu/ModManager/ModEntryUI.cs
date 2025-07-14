using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.Utility.Assets.Modding;

namespace Awaken.TG.Main.UI.Menu.ModManager {
    public partial class ModEntryUI : Element<ModManagerUI>, IWithRecyclableView {
        readonly int _initialIndex;
        readonly bool _initialActiveState;
        
        public sealed override bool IsNotSaved => true;
        
        public bool HasBeenModified => _initialIndex != Index || _initialActiveState != Active;

        /// <summary>
        /// Implementation of IWithRecyclableView. This is the index on the UI list.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// This is the index of the mod in the ModManager's ordered mods array.
        /// </summary>
        public int ModIndex { get; private set; }
        
        public bool IsSelected { get; private set; }
        public bool Active { get; private set; }
        public ModMetadata Metadata { get; private set; }
        
        VModEntryUI View => View<VModEntryUI>();
        ModHandle ModHandle { get; set; }

        public ModEntryUI(int index, ModHandle modHandle) {
            Index = index;
            ModHandle = modHandle;
            ModIndex = modHandle.index;
            Active = modHandle.active;
            _initialIndex = index;
            _initialActiveState = Active;
        }

        protected override void OnInitialize() {
            Metadata = ParentModel.ModManager.Metadata(ModHandle);
        }

        public void ToggleActive() {
            Active = !Active;
            var handle = ModHandle;
            handle.active = Active;
            ModHandle = handle;
            ParentModel.ModManager.OrderedMods[Index] = ModHandle;
            View.RefreshModActiveText();
        }
        
        public void RefreshIndex(int index) {
            Index = index;
        }

        public void Select() {
            ParentModel.ChangeEntrySelection(IsSelected ? null : this);
        }
        
        public void RefreshSelection(bool isSelected) {
            IsSelected = isSelected;
            if (View != null) {
                View.RefreshSelection();
            }
        }
    }
}