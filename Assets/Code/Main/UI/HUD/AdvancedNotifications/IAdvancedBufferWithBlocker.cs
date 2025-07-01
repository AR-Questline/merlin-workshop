using System;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.BufferBlockers;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    internal interface IAdvancedBufferWithBlocker : IModel {
        void AddBlockerForAnotherBuffers();
        Type BlockerType { get; }
    }

    internal interface IAdvancedBufferWithBlocker<T> : IAdvancedBufferWithBlocker where T : BufferBlocker, new() {
        void IAdvancedBufferWithBlocker.AddBlockerForAnotherBuffers() => AddElement(new T());
        Type IAdvancedBufferWithBlocker.BlockerType => typeof(T);
    }
}