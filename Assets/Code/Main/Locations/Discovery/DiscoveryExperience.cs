using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Locations.Discovery {
    public class DiscoveryExperience : RichEnum {
        public float ExpMulti { get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly DiscoveryExperience
            Default = new(nameof(Default), 0.05f),
            Major = new(nameof(Major), 0.10f),
            None = new(nameof(None), 0);

        DiscoveryExperience(string enumName, float exp) : base(enumName, nameof(DiscoveryExperience)) {
            ExpMulti = exp;
        }
    }
}