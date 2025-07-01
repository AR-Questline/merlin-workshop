using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCGameplayTools {
        const string ID = "QuantumConsole";
        
        [Command("toggle.toolbox", "All locks will be automatically unlocked")][UnityEngine.Scripting.Preserve]
        static void Toolbox() {
            Hero hero = Hero.Current;
            if (hero.HasElement<ToolboxOverridesMarker>()) {
                hero.RemoveElementsOfType<ToolboxOverridesMarker>();
                QuantumConsole.Instance.LogToConsoleAsync("Toolbox disabled \nLocks will no longer have requirements");
            } else {
                hero.AddMarkerElement<ToolboxOverridesMarker>();
                QuantumConsole.Instance.LogToConsoleAsync("Toolbox enabled \nLocks will now skip requirements");
            }
        }

        [Command("hero.teleport", "Teleport to target coordinates")][UnityEngine.Scripting.Preserve]
        static void Teleport(string coords) {
            var match = System.Text.RegularExpressions.Regex.Match(coords, @"\(?(-?\d+[.,]\d+), ?(-?\d+[.,]\d+), ?(-?\d+[.,]\d+)\)?");
            if (!match.Success) {
                QuantumConsole.Instance.LogToConsoleAsync("Provided content doesn't match the expected format: (x, y, z) of float values");
                return;
            }
            var x = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var y = float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            var z = float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
            var hero = Hero.Current;
            var position = new UnityEngine.Vector3(x, y, z);
            hero.TeleportTo(position);
        }
        
        [Command("hero.teleport", "Teleport to target coordinates")][UnityEngine.Scripting.Preserve]
        static void Teleport(string f4ignore, string f4ignore2, string coords, string scene) {
            var match = System.Text.RegularExpressions.Regex.Match(coords, @"\(?(-?\d+[.,]\d+), ?(-?\d+[.,]\d+), ?(-?\d+[.,]\d+)\)?");
            if (!match.Success) {
                QuantumConsole.Instance.LogToConsoleAsync("Provided content doesn't match the expected format: (x, y, z) of float values");
                return;
            }
            var x = float.Parse(match.Groups[1].Value);
            var y = float.Parse(match.Groups[2].Value);
            var z = float.Parse(match.Groups[3].Value);
            var hero = Hero.Current;
            var position = new UnityEngine.Vector3(x, y, z);
            hero.TeleportTo(position);
        }
        
        [Command("toggle.compass", "Toggles the compass visibility")][UnityEngine.Scripting.Preserve]
        static void ToggleCompass(bool force = false) {
            if (force) {
                bool any = false;
                foreach (HideCompass hideCompass in World.All<HideCompass>().ToArraySlow()) {
                    hideCompass.Discard();
                    any = true;
                }
                if (any) {
                    return;
                }
            }
            
            var existing = World.All<HideCompass>().FirstOrDefault(c => c.SourceID == ID);
            if (existing != null) {
                existing.Discard();
            } else {
                World.Add(new HideCompass(ID));
            }
        }
        
        static readonly string[] SFlags = { "RedDeathTalentTree:Unlocked" };
        [Command("set.ui-tabs-story-flag-unlocked", "Sets story flags state which constraining ui tabs")][UnityEngine.Scripting.Preserve]
        static void ToggleUITabStoryFlagRequirement(bool state) {
            foreach (string flag in SFlags) {
                StoryFlags.Set(flag, state);
            }
        }
        
        [Command("set.timescale", "Sets the timescale, volatile")][UnityEngine.Scripting.Preserve]
        static void SetTimescale(float value) {
            World.Only<GlobalTime>().DEBUG_SetTimeScale(value);
        }

        [Command("set.thievery-noise-boundary", "Sets the thievery noise boundary")][UnityEngine.Scripting.Preserve]
        static void SetThieveryNoiseBoundary(float value) {
            ThieveryNoise.strengthReactBoundary = value;
        }

        #region HeroStatEffects

        [Command("hero.set-stat", "Sets a hero stat to a specific value")][UnityEngine.Scripting.Preserve]
        static void SetHeroStat(StatType targetStat, float value) {
            Hero hero = Hero.Current;
            Stat stat = hero.Stat(targetStat);
            if (stat != null) {
                stat.SetTo(value);
            } else {
                QuantumConsole.Instance.LogToConsoleAsync("Stat not present or accessible on hero: " + targetStat);
            }
        }
        
        [Command("hero.restore-stats", "Restores all hero stats to their maximum values")][UnityEngine.Scripting.Preserve]
        static void FullRestore() {
            Hero.Current.RestoreStats();
        }

        #endregion
        
        #region HeroModes

        // Toggle: god mode
        // - immortality
        // - infinite mana
        [Command("toggle.hero.god-mode", "Toggles both immortality and infinite mana")][UnityEngine.Scripting.Preserve]
        static void ToggleGodMode() {
            if (s_infiniteManaTweak == null || s_immortalityTweak == null) {
                EnableImmortality();
                EnableInfiniteMana();
            } else {
                DisableImmortality();
                DisableInfiniteMana();
            }
            
            QuantumConsole.Instance.LogToConsoleAsync("God mode: " + (ImmortalityEnabled && InfiniteManaEnabled ? "enabled" : "disabled"));
        }


        // === Toggle: immortality
        // - does not take damage
        static StatTweak s_immortalityTweak;
        public static bool ImmortalityEnabled => s_immortalityTweak != null;
        
        [Command("toggle.hero.immortality", "Toggles immortality")][UnityEngine.Scripting.Preserve]
        static void ToggleImmortality() {
            if (s_immortalityTweak == null) {
                EnableImmortality();
            } else {
                DisableImmortality();
            }
            QuantumConsole.Instance.LogToConsoleAsync("Immortality: " + (ImmortalityEnabled ? "enabled" : "disabled"));
        }

        public static void EnableImmortality() {
            s_immortalityTweak ??= new StatTweak(Hero.Current.CharacterStats.IncomingDamage, 0, TweakPriority.Override, OperationType.Override, Hero.Current);
            s_immortalityTweak.MarkedNotSaved = true;
        }

        public static void DisableImmortality() {
            s_immortalityTweak?.Discard();
            s_immortalityTweak = null;
        }

        
        // === Toggle: infinite mana
        // - does not consume mana
        static StatTweak s_infiniteManaTweak;
        public static bool InfiniteManaEnabled => s_infiniteManaTweak != null;
        
        [Command("toggle.hero.infinite-mana", "Toggles infinite mana")][UnityEngine.Scripting.Preserve]
        static void ToggleInfiniteMana() {
            if (s_infiniteManaTweak == null) {
                EnableInfiniteMana();
            } else {
                DisableInfiniteMana();
            }
            
            QuantumConsole.Instance.LogToConsoleAsync("Infinite mana: " + (InfiniteManaEnabled ? "enabled" : "disabled"));
        }

        public static void EnableInfiniteMana() {
            s_infiniteManaTweak ??= new StatTweak(Hero.Current.CharacterStats.ManaUsageMultiplier, 0, TweakPriority.Override, OperationType.Override, Hero.Current);
            s_infiniteManaTweak.MarkedNotSaved = true;
        }

        public static void DisableInfiniteMana() {
            s_infiniteManaTweak?.Discard();
            s_infiniteManaTweak = null;
        }


        // === Toggle: infinite stamina
        // - does not consume stamina
        static StatTweak s_infiniteStaminaTweak;
        public static bool InfiniteStaminaEnabled => s_infiniteStaminaTweak != null;

        [Command("toggle.hero.infinite-stamina", "Toggles infinite stamina")][UnityEngine.Scripting.Preserve]
        static void ToggleInfiniteStamina() {
            if (s_infiniteStaminaTweak == null) {
                EnableInfiniteStamina();
            } else {
                DisableInfiniteStamina();
            }
            
            QuantumConsole.Instance.LogToConsoleAsync("Infinite stamina: " + (InfiniteStaminaEnabled ? "enabled" : "disabled"));
        }

        public static void EnableInfiniteStamina() {
            s_infiniteStaminaTweak ??= new StatTweak(Hero.Current.CharacterStats.StaminaUsageMultiplier, 0, TweakPriority.Override, OperationType.Override, Hero.Current);
            s_infiniteStaminaTweak.MarkedNotSaved = true;
        }

        public static void DisableInfiniteStamina() {
            s_infiniteStaminaTweak?.Discard();
            s_infiniteStaminaTweak = null;
        }

        #endregion
    }
}