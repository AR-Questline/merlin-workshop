using System;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Utility.Tags;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Technical {
    [Serializable]
    public partial class FlagRequiringBehaviourForwarder : EnemyBehaviourForwarder {
        [SerializeField] FlagLogic flag;
        [SerializeReference] EnemyBehaviourBase behaviour;

        protected override EnemyBehaviourBase BehaviourToClone {
            get => behaviour;
            set => behaviour = value;
        }

        protected override bool AdditionalConditions => flag.Get();
    }
}
