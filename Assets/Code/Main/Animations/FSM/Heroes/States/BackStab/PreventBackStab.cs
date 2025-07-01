using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab {
    public partial class PreventBackStab : DurationProxy<Hero> {
        public sealed override bool IsNotSaved => true;

        public override IModel TimeModel => ParentModel;

        PreventBackStab(IDuration duration) : base(duration) { }

        public static void Prevent(Hero hero, TimeDuration duration) {
            var prevent = hero.TryGetElement<PreventBackStab>();
            if (prevent != null) {
                prevent.Renew(duration);
            } else {
                hero.AddElement(new PreventBackStab(duration));
            }
        }
    }
}