using System.Threading;
using Awaken.TG.Debugging;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools;
using Awaken.TG.Debugging.ModelsDebugs.Runtime;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility;
using Awaken.Utility.PhysicUtils;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI {
    /// <summary>
    /// The global key handler that handles key shortcuts that work independent
    /// of what object is selected.
    /// </summary>
    public class GlobalKeys : IService, IUIAware {
        public static bool secondaryDebugActions = false;
        // === Key handling

        public UIResult Handle(UIEvent evt) {
            if (evt is UIAction actionEvent) {
                string name = actionEvent.Name;
                if (evt is UIKeyDownAction) return HandleKeyDown(name);
            }
            return UIResult.Ignore;
        }

        UIResult HandleKeyDown(string name) {
            if (QuantumConsole.Instance.IsActive) {
                return UIResult.Ignore;
            }

            if (name == KeyBindings.Debug.DebugToggleGraphy) {
                MemoryClear.ClearProgramming().Forget();
                return UIResult.Accept;
            }

            var gameplayResult = GameplayKeyDown(name);
            return gameplayResult != UIResult.Ignore ? gameplayResult : DebugKeyDown(name);
        }

        UIResult GameplayKeyDown(string name) {
            if (name == KeyBindings.UI.Generic.Cancel) {
                IClosable closable = World.LastOrNull<IClosable>();

                if (closable != null) {
                    // closable is closed only if it is active shortcut
                    if (closable.IsActive()) {
                        closable.Close();
                        return UIResult.Accept;
                    }
                }
            } else if (name == KeyBindings.UI.Generic.Menu) {
                if (!RewiredHelper.IsGamepad && World.HasAny<IClosable>()) {
                    return UIResult.Ignore;
                }
                if (!World.HasAny<MenuUI>() && !World.HasAny<Cutscene>() && UIStateStack.Instance.State.IsMapInteractive) {
                    World.Add(new MenuUI());
                    return UIResult.Accept;
                }
            } else if (name == KeyBindings.Debug.DebugSwitchCanvas) {
                var canvas = World.Services.TryGet<CanvasService>().MainCanvas;
                if (canvas != null) {
                    bool enable = !canvas.gameObject.activeSelf;
                    canvas.gameObject.SetActive(enable);
                    World.Services.TryGet<TransitionService>().gameObject.SetActive(enable);
                    World.Services.TryGet<MapStickerUI>().SetActive(enable);
                }

                return UIResult.Accept;
            } else if (name == KeyBindings.Gameplay.QuickSave) {
                if (LoadSave.Get.CanQuickSave()) {
                    LoadSave.Get.QuickSave().Forget();
                    return UIResult.Accept;
                } else if (World.HasAny<Hero>()) {
                    SaveLoadUnavailableInfo.ShowSaveUnavailableInfo();
                    return UIResult.Ignore;
                }
            } else if (name == KeyBindings.Gameplay.QuickLoad) {
                if (LoadSave.Get.LoadAllowedInGame()) {
                    LoadSave.Get.QuickLoad();
                    return UIResult.Accept;
                } else {
                    SaveLoadUnavailableInfo.ShowLoadUnavailableInfo();
                    return UIResult.Ignore;
                }
            }

            return UIResult.Ignore;
        }

        UIResult DebugKeyDown(string name) {
            if (CheatController.CheatsEnabled() == false 
                || CheatController.CheatShortcutsEnabled == false) {
                return UIResult.Ignore;
            }

            if (name == KeyBindings.Debug.DebugGodMode) {
                if (!World.HasAny<MarvinMode>()) {
                    World.Add(new MarvinMode());
                } else {
                    World.Any<MarvinMode>().ToggleView();
                }

                return UIResult.Accept;
            }
            
            // Always active debug actions
            if (name == KeyBindings.Debug.DebugHeroSkillKillEverything) {
                if (UIStateStack.Instance.State.IsMapInteractive) {
                    KillEveryoneNearHero();
                    return UIResult.Accept;
                }
            } else if (name == KeyBindings.Debug.DebugDelete) {
                IModel toDelete = Hero.Current.VHeroController.Raycaster.NPCRef.Get();
                if (toDelete != null) {
                    toDelete.Discard();
                }
                return UIResult.Accept;
            } else if (name == KeyBindings.Debug.DebugHeroSkillRegenHPAndStamina) {
                Hero hero = Hero.Current;
                if (hero.MaxStamina.ModifiedValue > 99999) {
                    var rpgStats = hero.HeroRPGStats;
                    rpgStats.Strength.SetTo(100);
                    rpgStats.Dexterity.SetTo(100);
                    rpgStats.Spirituality.SetTo(100);
                    rpgStats.Perception.SetTo(100);
                    rpgStats.Endurance.SetTo(100);
                    rpgStats.Practicality.SetTo(100);
                }

                hero.MaxStamina.SetTo(100000);
                hero.Stamina.SetTo(100000);
                hero.MaxHealth.SetTo(100000);
                hero.Health.SetTo(100000);
                hero.MaxMana.SetTo(100000);
                hero.Mana.SetTo(100000);
                hero.HeroStats.EncumbranceLimit.SetTo(100000);
                return UIResult.Accept;
            } else if (name == KeyBindings.Debug.DebugHeroSkillLogInfo) {
                Vector3 pos = Hero.Current.Coords;
                string log = $"Player Position: {pos.ToString()} ({SceneManager.GetActiveScene().name})";
                Awaken.Utility.Debugging.Log.Important?.Error(log, logOption: LogOption.NoStacktrace);
                // save to clipboard
                GUIUtility.systemCopyBuffer = log;
                QCDebugTools.ToggleExtendedDebugNames();
                return UIResult.Accept;
            } else if (name == KeyBindings.Debug.DebugCallMount && Hero.Current.Grounded) {
                Hero.Current.CallMount();
            } else if (name == KeyBindings.Debug.DebugToggleGraphicsLevel) {
                World.Only<GraphicPresets>().EnumOption.NextOptionCarousel();
                return UIResult.Accept;
            }
            if (secondaryDebugActions) {
                return SecondaryDebugActions(name);
            }

            return PrimaryDebugActions(name);
        }

        UIResult PrimaryDebugActions(string name) {
            if (name == KeyBindings.Debug.DebugAddResourcesAndExp) {
                Hero hero = Hero.Current;
                
                bool bufferWasBlocked = AdvancedNotificationBuffer.AllNotificationsSuspended;
                AdvancedNotificationBuffer.AllNotificationsSuspended = true;
                try {
                    float requiredXp = hero.HeroStats.XPForNextLevel - hero.HeroStats.XP;
                    hero.HeroStats.XP.IncreaseBy(requiredXp);
                    hero.Wealth.SetTo(99000);
                    hero.Cobweb.SetTo(9999);

                    World.Only<PlayerJournal>().TreatAllEntriesAsUnlocked();

                    foreach (var discovery in World.All<LocationDiscovery>()) {
                        if (discovery.IsFastTravel) {
                            discovery.Discover();
                        }
                    }
                } finally {
                    AdvancedNotificationBuffer.AllNotificationsSuspended = bufferWasBlocked;
                }

                return UIResult.Accept;
            } else if (name == KeyBindings.Debug.DebugModelsDebug) {
                if (!World.HasAny<ModelsDebugModel>()) {
                    World.Add(new ModelsDebugModel());
                }

                return UIResult.Accept;
            } else if (name == KeyBindings.Debug.DebugHeroSkillSuperDash) {
                if (UIStateStack.Instance.State.IsMapInteractive) {
                    DebugHeroDash(new Vector3(0, 3, 25), 1f).Forget();
                    return UIResult.Accept;
                }
            } else if (name == KeyBindings.Debug.DebugHeroSkillSuperJump) {
                if (UIStateStack.Instance.State.IsMapInteractive) {
                    DebugHeroDash(new Vector3(0, 25, 2), 0.3f).Forget();
                    return UIResult.Accept;
                }
            } else if (name == KeyBindings.Debug.DebugToggleGraphy) {
                //GraphyManager.Instance.ToggleActive();
                return UIResult.Accept;
            }


            return UIResult.Ignore;
        }

        static UIResult SecondaryDebugActions(string name) {
            if (name == KeyBindings.Debug.DebugHeroSkillSuperJump) {
                DebugSimulateSpike().Forget();
                return UIResult.Accept;
                
            }
            return UIResult.Ignore;
        }

        static async UniTaskVoid DebugSimulateSpike(float laggedFrames = 5, int millisecondLag = 200) {
            for (int i = 0; i < laggedFrames; i++) {
                Thread.Sleep(millisecondLag);
                await UniTask.NextFrame();
            }
        }

        async UniTaskVoid DebugHeroDash(Vector3 force, float duration) {
            Hero hero = Hero.Current;
            Vector3 cameraEuler = World.Only<GameCamera>().MainCamera.transform.rotation.eulerAngles;
            Quaternion startLookRotation = Quaternion.Euler(0, cameraEuler.y, 0);
            CharacterController controller = hero.VHeroController.GetComponentInChildren<CharacterController>();
            hero.Element<HeroFallDamage>().IgnoreFallDamageForDuration(5);
            float timer = duration;
            while (timer > 0) {
                Vector3 motion = (Time.deltaTime / duration) * (startLookRotation * force);
                controller.Move(motion);
                timer -= Time.deltaTime;
                await UniTask.NextFrame();
            }
        }

        void KillEveryoneNearHero() {
            Hero hero = Hero.Current;
            foreach (var colliderHit in PhysicsQueries.OverlapSphere(hero.Coords, 10f, RenderLayers.Mask.Hitboxes, QueryTriggerInteraction.Collide)) {
                Damage.DetermineTargetHit(colliderHit, out IAlive receiver, out HealthElement healthElement);
                if (receiver != null && healthElement != null && healthElement != hero.HealthElement) {
                    DamageParameters parameters = DamageParameters.Default;
                    parameters.Position = colliderHit.bounds.center;
                    parameters.Direction = Vector3.up;
                    parameters.ForceDirection = parameters.Direction;
                    parameters.ForceDamage = 0;
                    parameters.Inevitable = true;
                    parameters.DamageTypeData = new RuntimeDamageTypeData(DamageType.PhysicalHitSource);
                    Damage damage = new Damage(parameters, hero, receiver, new RawDamageData(float.MaxValue)).WithHitCollider(colliderHit);
                    healthElement.TakeDamage(damage);
                }
            }
        }
    }
}
