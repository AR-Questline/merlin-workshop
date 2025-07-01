using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class BuffSelfBehaviour : SpellCastingBehaviourBase {
        // === Serialized Fields
        [SerializeField] bool addStatus;

        [SerializeField, ShowIf(nameof(addStatus))]
        StatusTemplate bonusStatusTemplate;

        [SerializeField] bool addItemBuff;

        [SerializeField, ShowIf(nameof(addItemBuff))]
        EquipmentSlotType itemSlot = EquipmentSlotType.MainHand;

        [SerializeField, LabelText("Duration[s]"), ShowIf(nameof(addItemBuff))]
        int itemDuration;

        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX),
         ShowIf(nameof(addItemBuff))]
        ShareableARAssetReference itemVFX;

        [SerializeField, ShowIf(nameof(addItemBuff))]
        float itemVfxApplyTime = 1f;

        [SerializeField, ShowIf(nameof(addItemBuff))]
        List<SkillReference> itemSkills = new();
        
        AppliedItemBuff _appliedItemBuff;

        protected override UniTask SpawnFireBallInHand() {
            var spawnTask = base.SpawnFireBallInHand();
            if (addItemBuff) {
                AddItemBuff();
            }
            return spawnTask;
        }
        
        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            if (addStatus) {
                AddStatus();
            }

            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }

            PlaySpecialAttackReleaseAudio();
            
            return UniTask.CompletedTask;
        }

        void AddStatus() {
            Npc.Statuses.AddStatus(bonusStatusTemplate, new StatusSourceInfo().WithCharacter(Npc));
        }
        
        void AddItemBuff() {
            Item equipped = Npc.Inventory.EquippedItem(itemSlot);
            if (equipped == null) {
                return;
            }

            if (_appliedItemBuff is { HasBeenDiscarded: false }) {
                _appliedItemBuff.Discard();
            }

            _appliedItemBuff = new(equipped.Template, itemVFX, itemDuration);
            foreach (Skill s in itemSkills.Select(skillRef => skillRef.CreateSkill())) {
                _appliedItemBuff.AddElement(s);
            }

            equipped.AddElement(_appliedItemBuff);
            ApplyItemBuffEffect(equipped.Owner).Forget();
        }

        async UniTaskVoid ApplyItemBuffEffect(IItemOwner itemOwner) {
            itemOwner.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdate, 0f);
            float percent = 0f;
            while (percent < 1f) {
                itemOwner.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdate, percent);
                if (!await AsyncUtil.DelayFrame(this)) {
                    return;
                }

                percent += Time.deltaTime / itemVfxApplyTime;
            }

            itemOwner.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdate, 1f);
            itemOwner.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdateCompleted, true);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            if (!fromDomainDrop) {
                if (_appliedItemBuff is { HasBeenDiscarded: false }) {
                    _appliedItemBuff.Discard();
                }
                _appliedItemBuff = null;
            }
        }
    }
}