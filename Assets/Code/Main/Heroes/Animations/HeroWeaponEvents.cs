using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Animations {
    public partial class HeroWeaponEvents : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        readonly List<CharacterHandBase> _currentWeapons = new();
        readonly OnDemandCache<CharacterHandBase, ScriptMachine[]> _weaponsScriptMachines = new(_ => Array.Empty<ScriptMachine>());

        public static HeroWeaponEvents Current { get; private set; }

        protected override void OnInitialize() {
            Current = this;
        }

        public void RegisterWeapon(CharacterHandBase weapon) {
            _currentWeapons.Add(weapon);
            _weaponsScriptMachines[weapon] = weapon.GetComponents<ScriptMachine>();
        }

        public void UnregisterWeapon(CharacterHandBase weapon) {
            _currentWeapons.Remove(weapon);
            _weaponsScriptMachines.Remove(weapon);
        }

        public bool IsLoadingAnimations() {
            return _currentWeapons.Any(w => w.IsLoadingAnimator);
        }

        public void TriggerAnimancerEvent(ARAnimationEventData eventData) {
            IHandOwner<ICharacter> handOwner = Hero.Current.Element<HeroHandOwner>();
            if (!eventData.CanBeInvokedInHitStop && Hero.Current.IsInHitStop) {
                return;
            }

            // --- Actions
            if (eventData.actionType == ARAnimationEvent.ActionType.AttackRelease) {
                handOwner.OnAttackRelease(eventData);
            } else if (eventData.actionType == ARAnimationEvent.ActionType.AttackRecovery) {
                handOwner.OnAttackRecovery(eventData);
            } else if (eventData.actionType == ARAnimationEvent.ActionType.FinisherRelease) {
                CharacterHandBase handBase = _currentWeapons.First();
                Vector3 position = handBase is CharacterWeapon weapon ? weapon.ColliderPivot.position : handBase.transform.position;
                handOwner.OnFinisherRelease(position);
            } else if (eventData.actionType == ARAnimationEvent.ActionType.BackStabRelease) {
                handOwner.OnBackStabRelease();
            } else if (eventData.actionType == ARAnimationEvent.ActionType.ToolStartInteraction) {
                _currentWeapons.First().OnToolInteractionStart();
            } else if (eventData.actionType == ARAnimationEvent.ActionType.ToolEndInteraction) {
                _currentWeapons.First().OnToolInteractionEnd();
            } else if (eventData.actionType == ARAnimationEvent.ActionType.EffectInvoke) {
                handOwner.OnEffectInvoke(eventData);
            } else if (eventData.actionType == ARAnimationEvent.ActionType.QuickUseItemUsed) {
                handOwner.OnQuickUseItemUsed(eventData);
            }

            // --- Visual Scripting Unity Events
            string actionType = eventData.actionType.ToString();
            
            foreach (var scriptMachines in _weaponsScriptMachines.Values) {
                foreach (var scriptMachine in scriptMachines) {
                    scriptMachine.TriggerUnityEvent(actionType);
                }
            }

            // --- Audio
            if (_currentWeapons.Count <= 0) {
                return;
            }

            PlayItemAudio(eventData);
            PlayCharacterAudio(eventData, Hero.Current);
        }

        public void TriggerAnimancerEvent(ARFinisherEffectsData effectsData) {
            ParentModel.Trigger(FinisherState.Events.FinisherAnimationEvent, effectsData);
        }
        
        void PlayItemAudio(ARAnimationEventData eventData) {
            CharacterHandBase weapon = _currentWeapons[0];
            foreach (ItemAudioType itemAudioType in eventData.ItemAudio) {
                if (itemAudioType == ItemAudioType.MeleeSwing) {
                    weapon.PlayAudioClip(eventData.attackType switch {
                        AttackType.Heavy => ItemAudioType.MeleeSwingHeavy,
                        AttackType.Lunge => ItemAudioType.MeleeDashAttack,
                        AttackType.Pommel => ItemAudioType.PommelSwing,
                        _ => ItemAudioType.MeleeSwing
                    });
                } else {
                    weapon.PlayAudioClip(itemAudioType);
                }
            }
        }

        void PlayCharacterAudio(ARAnimationEventData eventData, ICharacter character) {
            foreach (AliveAudioType characterAudio in eventData.AliveAudio) {
                bool asOneShot = characterAudio == AliveAudioType.FootStep;
                character.PlayAudioClip(characterAudio, asOneShot);
            }

            FMODManager.PlayBodyMovement(eventData.ArmorAudio, character);
        }

        public void ToggleArrowInMainHand(int enabled) {
            CharacterBow bow = _currentWeapons.OfType<CharacterBow>().FirstOrDefault();
            if (bow != null) {
                bow.ToggleArrows(enabled == 1);
            }
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            Current = null;
            base.OnDiscard(fromDomainDrop);
        }
    }
}