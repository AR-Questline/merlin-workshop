using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Fights.FPP {
    public class  WeaponEventsListener : MonoBehaviour {
        readonly List<CharacterHandBase> _currentWeapons = new();
        OnDemandCache<CharacterHandBase, ScriptMachine[]> _weaponsScriptMachines = new(_ => Array.Empty<ScriptMachine>());
        List<ScriptMachine> _scriptMachines;

        IItemOwner Owner => _currentWeapons.FirstOrDefault(w => w != null && w.Owner != null)?.Owner;
        IHandOwner<ICharacter> HandOwner => _currentWeapons.FirstOrDefault(w => w != null && w.HandOwner != null)?.HandOwner;
        bool Initialized => _currentWeapons.Count > 0;
        int _lastAnimationEventFrame;
        Object _lastAnimationEventObject;

        void Awake() {
            _scriptMachines = GetComponents<ScriptMachine>().ToList();
        }

        public void InitWeapon(CharacterHandBase weapon) {
            _currentWeapons.Add(weapon);
            _weaponsScriptMachines[weapon] = weapon.GetComponents<ScriptMachine>();
        }

        public void Clear(CharacterHandBase weapon) {
            if (!Initialized) {
                return;
            }
            _currentWeapons.Remove(weapon);
            _weaponsScriptMachines.Remove(weapon);
        }
        
        // --- Called from animator event
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {
            if (!Initialized || (HandOwner?.HasBeenDiscarded ?? true)) {
                return;
            }

            if (Owner is Hero { IsInHitStop: true }) {
                return;
            }

            // --- Head animations use the same animation as the weapons, so we need to filter out events from those animations.
            if (_lastAnimationEventFrame == Time.frameCount && obj == _lastAnimationEventObject) {
                return;
            }
            _lastAnimationEventFrame = Time.frameCount;
            _lastAnimationEventObject = obj;

            if (obj is ARAnimationEvent animationEvent) {
                var handBase = _currentWeapons.FirstOrDefault(w => animationEvent.restriction.Match(w));
                if (handBase == null) {
                    handBase = _currentWeapons.First();
                }
                // --- Actions
                var eventData = animationEvent.CreateData();
                if (animationEvent.actionType == ARAnimationEvent.ActionType.AttackRelease) {
                    HandOwner.OnAttackRelease(eventData);
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.AttackRecovery) {
                    HandOwner.OnAttackRecovery(eventData);
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.FinisherRelease) {
                    Vector3 position = handBase is CharacterWeapon weapon ? weapon.ColliderPivot.position : handBase.transform.position;
                    HandOwner.OnFinisherRelease(position);
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.BackStabRelease) {
                    HandOwner.OnBackStabRelease();
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.ToolStartInteraction) {
                    handBase.OnToolInteractionStart();
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.ToolEndInteraction) {
                    handBase.OnToolInteractionEnd();
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.EffectInvoke) {
                    HandOwner.OnEffectInvoke(eventData);
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.QuickUseItemUsed) {
                    HandOwner.OnQuickUseItemUsed(eventData);
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.Appear) {
                    HandOwner.ParentModel.Trigger(
                        animationEvent.appearType == ARAnimationEvent.AppearType.Character
                            ? ICharacter.Events.SwitchCharacterVisibility
                            : ICharacter.Events.SwitchCharacterWeaponVisibility, true);
                } else if (animationEvent.actionType == ARAnimationEvent.ActionType.Disappear) {
                    HandOwner.ParentModel.Trigger(
                        animationEvent.appearType == ARAnimationEvent.AppearType.Character
                            ? ICharacter.Events.SwitchCharacterVisibility
                            : ICharacter.Events.SwitchCharacterWeaponVisibility, false);
                }

                // --- Visual Scripting Unity Events
                string actionType = animationEvent.actionType.ToString();
                _scriptMachines.ForEach(s => s.TriggerUnityEvent(actionType));
                foreach (ScriptMachine[] scriptMachines in _weaponsScriptMachines.Values) {
                    scriptMachines.ForEach(s => s.TriggerUnityEvent(actionType));
                }

                // --- Audio
                PlayItemAudio(handBase, eventData);

                if (Owner?.Character != null) {
                    PlayCharacterAudio(eventData, Owner.Character);
                }

                // --- Behaviour animation event
                if (Owner?.Character is NpcElement npc) {
                    var behavioursOwner = npc.ParentModel?.TryGetElement<IBehavioursOwner>();
                    behavioursOwner?.TriggerAnimationEvent(animationEvent);
                    behavioursOwner?.Trigger(EnemyBaseClass.Events.AnimationEvent, animationEvent);
                }
            } else if (obj is ARVfxAnimationEvent vfxEvent) {
                vfxEvent.SpawnOnCharacter(Owner?.Character).Forget();
            }
        }

        static void PlayItemAudio(CharacterHandBase weapon, ARAnimationEventData eventData) {
            foreach (ItemAudioType itemAudioType in eventData.ItemAudio) {
                weapon.PlayAudioClip(itemAudioType);
            }
        }
        
        void PlayCharacterAudio(ARAnimationEventData eventData, ICharacter character) {
            foreach (AliveAudioType characterAudio in eventData.AliveAudio) {
                bool asOneShot = characterAudio == AliveAudioType.FootStep;
                character.PlayAudioClip(characterAudio, asOneShot);
            }
                
            FMODManager.PlayBodyMovement(eventData.ArmorAudio, character);
        }
        
        void OnDestroy() {
            _currentWeapons.Clear();
            _weaponsScriptMachines.Clear();
            _scriptMachines.Clear();
        }
    }
}
