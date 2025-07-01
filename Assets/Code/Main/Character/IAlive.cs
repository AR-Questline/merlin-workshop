using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Memories.Journal.Conditions.Models;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public interface IAlive : IWithStats, IGrounded, IAliveAudio, IAliveVfx {
        public static class Events {
            public static readonly Event<IAlive, DamageOutcome> BeforeDeath = new(nameof(BeforeDeath));

            public static readonly Event<IAlive, DamageOutcome> AfterDeath = new(nameof(AfterDeath));
            
            public static readonly Event<IAlive, bool> Fracture = new(nameof(Fracture));
            public static readonly Event<IAlive, bool> ResetFracture = new(nameof(ResetFracture));
        }

        bool IsAlive { get; }
        bool IsDying { get; }
        bool Grounded { get; }
        
        AliveStats AliveStats { get; }
        AliveStats.ITemplate AliveStatsTemplate { get; }
        LimitedStat Health { get; }
        Stat MaxHealth { get; }

        float TotalArmor(DamageSubType damageType) => AliveStats.Armor;

        HealthElement HealthElement { get; }
        void DieFromDamage(DamageOutcome damageOutcome) {
            this.CallDieEvents(damageOutcome);
        }

        void CallDieEvents(DamageOutcome damageOutcome) {
            this.Trigger(Events.BeforeDeath, damageOutcome);
            HealthElement.TriggerVisualScriptingOnDeath();
            this.Trigger(Events.AfterDeath, damageOutcome);

            // To not listen to every NPC for Events.AfterDeath, invert dependency and call directly
            foreach (var killCount in World.All<KillCountRuntime>()) {
                killCount.OnNpcDeath(damageOutcome);
            }
        }

        void Kill(ICharacter killer = null, bool allowPrevention = false) {
            HealthElement?.Kill(killer, allowPrevention);
        }
        // --- VFX
        ShareableARAssetReference HitVFX { get; }
        // --- ParentTransform for Health
        Transform ParentTransform { get; }
        // --- Audio
        SurfaceType AudioSurfaceType { get; }
        void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams);
        void PlayAudioClip(EventReference eventReference, bool asOneShot = false, params FMODParameter[] eventParams);
    }
}