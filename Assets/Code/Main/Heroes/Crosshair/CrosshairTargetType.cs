using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.Utility;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Crosshair {
    public class CrosshairTargetType : RichEnum {
        public Color Color { get; }
        
        CrosshairTargetType(string name, Color color) : base(name) {
            Color = color;
        }

        public static readonly CrosshairTargetType
            Default = new CrosshairTargetType(nameof(Default), ARColor.DefaultCrosshair),
            Hostile = new CrosshairTargetType(nameof(Hostile), ARColor.HostileCrosshair),
            NonHostile = new CrosshairTargetType(nameof(NonHostile), ARColor.NonHostileCrosshair);
    }
}
