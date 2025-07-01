using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public partial class IdleDataOverride : Element<Location>, IIdleDataSource {
        public override ushort TypeForSerialization => SavedModels.IdleDataOverride;

        [Saved] IIdleDataSource _source;
        
        public int Priority => 1;
        
        public float PositionRange => _source.PositionRange;
        public bool UseAttachmentSpace => _source.UseAttachmentSpace;
        public Vector3 AttachmentPosition => _source.AttachmentPosition;
        public Vector3 AttachmentForward => _source.AttachmentForward;
        
        public IInteractionSource GetCurrentSource() => _source.GetCurrentSource();

        [JsonConstructor, UnityEngine.Scripting.Preserve] IdleDataOverride() { }
        
        public IdleDataOverride(IIdleDataSource source) {
            _source = source;
        }

        protected override void OnFullyInitialized() {
            if (_source is { HasBeenDiscarded: false }) {
                _source.ParentModel.ListenTo(IIdleDataSource.Events.InteractionIntervalChanged, _ => OnIntervalChanged(), this);
                OnIntervalChanged();
            } else {
                Discard();
            }
        }

        void OnIntervalChanged() {
            ParentModel.Trigger(IIdleDataSource.Events.InteractionIntervalChanged, this);
        }
    }
}