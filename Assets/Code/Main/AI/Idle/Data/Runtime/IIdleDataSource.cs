using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public interface IIdleDataSource : IElement<Location> {
        int Priority { get; }
        
        float PositionRange { get; }
        bool UseAttachmentSpace { get; }
        Vector3 AttachmentPosition { get; }
        Vector3 AttachmentForward { get; }
        
        IInteractionSource GetCurrentSource();
        
        public static class Events {
            public static readonly Event<Location, IIdleDataSource> InteractionIntervalChanged = new(nameof(InteractionIntervalChanged));
            public static readonly Event<Location, InteractionOneShotData> InteractionOneShotTriggered = new(nameof(InteractionOneShotTriggered));
        }
    }
}