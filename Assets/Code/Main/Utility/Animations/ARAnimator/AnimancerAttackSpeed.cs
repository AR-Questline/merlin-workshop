using System;
using Awaken.TG.Main.Heroes;
using Awaken.Utility.Enums;
using UnityEngine;
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    public class AnimancerAttackSpeed : RichEnum {
        public int AnimatorHash { get; }
        public Action<ARHeroAnimancer, float> SetAttackSpeed { get; }

        AnimancerAttackSpeed(string enumName, Action<ARHeroAnimancer, float> setAttackSpeed, string inspectorCategory = "") : base(enumName,
            inspectorCategory) {
            AnimatorHash = Animator.StringToHash(enumName);
            SetAttackSpeed = setAttackSpeed;
        }

        public static AnimancerAttackSpeed 
            HeavyAttackMult1H = new(nameof(HeavyAttackMult1H), (animancer, value) => {
                animancer.heavyAttackMult1H = value;
            }),
            LightAttackMult1H = new(nameof(LightAttackMult1H), (animancer, value) => {
                animancer.lightAttackMult1H = value;
            }),
            HeavyAttackMult2H = new(nameof(HeavyAttackMult2H), (animancer, value) => {
                animancer.heavyAttackMult2H = value;
            }),
            LightAttackMult2H = new(nameof(LightAttackMult2H), (animancer, value) => {
                animancer.lightAttackMult2H = value;
            }),
            BowDrawSpeed = new(nameof(BowDrawSpeed), (animancer, value) => {
                animancer.bowDrawSpeed = value;
            });
    }
}