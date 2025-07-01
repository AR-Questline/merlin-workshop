using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Timing.ARTime {
    /// <summary>
    /// Empty model that only exist to provide ARTime API for non-model 
    /// </summary>
    public partial class TimeModel : Model {
        Domain _domain;
        public override Domain DefaultDomain => _domain;
        public sealed override bool IsNotSaved => true;

        public TimeModel() : this(Domain.Gameplay) { }
        public TimeModel(Domain domain) {
            _domain = domain;
        }

        protected override void OnInitialize() {
            this.GetOrCreateTimeDependent();
        }
    }
}