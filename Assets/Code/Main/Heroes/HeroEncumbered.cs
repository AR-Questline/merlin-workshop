using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroEncumbered: Element<Hero> {
        public sealed override bool IsNotSaved => true;

        public bool IsEncumbered { get; private set; }

        public new static class Events {
            public static readonly Event<HeroEncumbered, bool> EncumberedChanged = new(nameof(EncumberedChanged));
        }
        
        public void ToggleEncumbered(bool activate) {
            if (IsEncumbered == activate) {
                return;
            }
            
            if (CommonReferences.Get.OverEncumbranceStatus.TryGet(out StatusTemplate status)) {
                var statuses = ParentModel.Statuses;
                
                if (activate) {
                    statuses.AddStatus(status, StatusSourceInfo.FromStatus(status).WithCharacter(ParentModel));
                } else {
                    statuses.RemoveStatus(status);
                }
            } else {
                Log.Important?.Error($"OverEncumbrance {nameof(StatusTemplate)} not found in {nameof(CommonReferences)}");
            }
            
            IsEncumbered = activate;
            this.Trigger(Events.EncumberedChanged, activate);
        }
    }
}