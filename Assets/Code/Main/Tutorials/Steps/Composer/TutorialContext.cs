using System;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Tutorials.Steps.Composer {
    public class TutorialContext {
        public IModel target;
        public ViewComponent vc;
        public Action onFinish;
        public bool IsDone { get; private set; }
        public void Finish() {
            if (IsDone) return;
            IsDone = true;
            onFinish?.Invoke();
            onFinish = null;
        }
    }
}