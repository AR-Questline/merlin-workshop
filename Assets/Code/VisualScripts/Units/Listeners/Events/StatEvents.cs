using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills.Units.Effects;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/General/Events/Stats")]
    public abstract class EvtStat<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }

    [UnityEngine.Scripting.Preserve]
    public class EvtStatChanged : EvtStat<IWithStats, Stat> {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [RichEnumExtends(typeof(StatType))]
        public RichEnumReference slot;

        protected override Event<IWithStats, Stat> Event => Stat.Events.StatChanged(slot.EnumAs<StatType>());
        protected override IWithStats Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtStatChangedBy : EvtStat<IWithStats, Stat.StatChange> {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [RichEnumExtends(typeof(StatType))]
        public RichEnumReference slot;

        protected override Event<IWithStats, Stat.StatChange> Event => Stat.Events.StatChangedBy(slot.EnumAs<StatType>());
        protected override IWithStats Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtManaSpent : EvtStat<Hero, SpendManaCostUnit.ManaSpendData> {
        protected override Event<Hero, SpendManaCostUnit.ManaSpendData> Event => Hero.Events.ManaSpend;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }
}