using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Saving {
    /// <summary>
    /// Interface for services that need to be serialized used by LoadSave.
    /// Can add more callbacks in future, depending on needs.
    /// By default, SerializedService is saved in "Gameplay" domain, you can specify it by using <see cref="IDomainBoundService"/>
    /// </summary>
    public abstract partial class SerializedService : IService {
        public virtual ushort TypeForSerialization => 0;
        
        public virtual void OnBeforeSerialize() { }
        public virtual void OnAfterDeserialize() { }
        
        public static readonly Domain DefaultDomain = Domain.Globals;
    }
}