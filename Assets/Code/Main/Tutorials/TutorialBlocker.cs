using Awaken.TG.Main.Tutorials.Steps.Composer;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Tutorials {
    /// <summary>
    /// Responsible for making sure that only one tutorial is run at the same moment
    /// </summary>
    public partial class TutorialBlocker : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public TutorialContext Context { get; }

        public TutorialBlocker(TutorialContext context) {
            Context = context;
        }

        protected override void OnFullyInitialized() {
            if (Context.IsDone) {
                Discard();
            } else {
                Context.onFinish += Discard;
            }
        }
    }
}