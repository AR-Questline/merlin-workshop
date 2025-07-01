namespace Awaken.TG.MVC.Domains {
    public interface IWithDomainMovedCallback : IModel {
        void DomainMoved(Domain newDomain);
    }
}
