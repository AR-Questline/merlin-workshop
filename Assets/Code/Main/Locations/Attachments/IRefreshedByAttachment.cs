using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments {
    /// <summary>
    /// Caution: This shouldn't be used on elements that are dynamically added and shouldn't be used with fields/properties that can change in runtime. 
    /// </summary>
    public interface IRefreshedByAttachment : IElement {
        /// <summary>
        /// This is invoked before Element's OnInitialize and OnRestore methods, but Location has already been Initialized by now.
        /// </summary>
        void InitFromAttachment(IAttachmentSpec spec, bool isRestored);
    }
    
    public interface IRefreshedByAttachment<in T> : IRefreshedByAttachment where T : IAttachmentSpec {
        void IRefreshedByAttachment.InitFromAttachment(IAttachmentSpec spec, bool isRestored) => InitFromAttachment((T) spec, isRestored);
        void InitFromAttachment(T spec, bool isRestored);
    }
}