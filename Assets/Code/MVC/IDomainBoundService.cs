using Awaken.TG.MVC.Domains;

namespace Awaken.TG.MVC {
    public interface IDomainBoundService : IService {
        Domain Domain { get; }
        bool RemoveOnDomainChange();
    }
}