using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class ToolInteractAction : AbstractLocationAction, ILocationNameModifier {
        public override ushort TypeForSerialization => SavedModels.ToolInteractAction;

        [Saved] protected ToolType _requiredToolType;
        
        protected IAlive _alive;

        public override InfoFrame ActionFrame => IsHeroPossessingTool ? 
            new InfoFrame(_requiredToolType.InteractionName, true) : 
            new InfoFrame(LocTerms.Blocked.Translate(), false);
        protected override InteractRunType RunInteraction => InteractRunType.DontRun;
        public int ModificationOrder => 10;

        Hero Hero => Hero.Current;
        HeroItems HeroItems => World.Only<HeroItems>();
        protected bool IsHeroPossessingTool => HeroItems.Items.Any(IsToolSuitable);
        protected virtual bool CanInteractThroughDamage => true;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ToolInteractAction() { }

        public ToolInteractAction(ToolType requiredToolType) {
            _requiredToolType = requiredToolType;
        }
        
        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(LocationFullyInitialized);
        }
        
        protected override void OnRestore() {
            ParentModel.AfterFullyInitialized(LocationFullyInitialized);
        }

        void LocationFullyInitialized() {
            _alive = ParentModel.TryGetElement<IAlive>();
            _alive?.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamageHook, this);
            OnLocationFullyInitialized();
        }
        
        protected virtual void OnLocationFullyInitialized() { }
        
        void OnTakingDamageHook(HookResult<HealthElement, Damage> hook) {
            if (CanInteractThroughDamage) {
                TryToReplaceDamageWithInteractionDamage(hook);
            }
        }
        
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (hero.HasElement<ToolboxOverridesMarker>() && ParentModel.TryGetElement<DigOutAction>() is { } doa) {
                doa.DigOutOverride();
                return;
            }
            if (!IsHeroPossessingTool) return;
            
            Item tool = HeroItems.EquippedItems().FirstOrDefault(IsToolSuitable);
            if (tool != null) {
                var characterHandBase = tool.View<CharacterHandBase>();
                if (characterHandBase != null && characterHandBase.IsHidden) {
                    characterHandBase.ShowWeapon();
                    UseTool().Forget();
                } else {
                    UseTool(0).Forget();
                }
            } else {
                tool = HeroItems.Items
                    .Where(IsToolSuitable)
                    .OrderByDescending(item => item.Quality.Priority)
                    .FirstOrDefault();
                if (tool != null) {
                    hero.HeroItems.LoadoutAt(HeroLoadout.HiddenLoadoutIndex).EquipItem(null, tool);
                    hero.HeroItems.ActivateLoadout(HeroLoadout.HiddenLoadoutIndex, false);
                    UseTool().Forget();
                }
            }
        }

        public virtual string ModifyName(string original) {
            if (!IsHeroPossessingTool) {
                return LocTerms.ToolRequired.Translate(_requiredToolType.DisplayName);
            }

            return original;
        }
        
        void TryToReplaceDamageWithInteractionDamage(HookResult<HealthElement, Damage> hook) {
            var damage = hook.Value;
            if (IsInteractionDamage(damage)) {
                return;
            }
            
            if (!IsDamageSuitableToReplace(damage)) {
                damage.RawData.SetToZero();
                return;
            }

            ReplaceDamageWithInteractionDamage(hook);
        }
        
        bool IsInteractionDamage(Damage damage) {
            return !damage.IsPrimary && damage.Type == DamageType.Interact;
        }
        
        bool IsDamageSuitableToReplace(Damage damage) {
            if (damage.IsPush) {
                return false;
            }
            
            if (ParentModel.Interactability != LocationInteractability.Active) {
                return false;
            }
            
            if (!IsToolSuitable(damage.Item)) {
                return false;
            }

            if (!damage.Item.TryGetElement(out Tool tool) || tool.Type != _requiredToolType) {
                return false;
            }
            
            return damage.Type switch {
                DamageType.Interact => true,
                DamageType.PhysicalHitSource when damage.IsHeavyAttack => tool.CanInteractWithHeavyAttack,
                DamageType.PhysicalHitSource => tool.CanInteractWithLightAttack,
                _ => false
            };
        }

        void ReplaceDamageWithInteractionDamage(HookResult<HealthElement, Damage> damageHook) {
            damageHook.Prevent();
            var damage = damageHook.Value;

            var damageParams = damage.Parameters;
            damageParams.IsPrimary = false;
            damageParams.DamageTypeData = new RuntimeDamageTypeData(DamageType.Interact);
            var newDamage = new Damage(damageParams, damage.DamageDealer, damage.Target, new RawDamageData(1.0f))
                .WithItem(damage.Item)
                .WithHitCollider(damage.HitCollider);
            _alive.HealthElement.TakeDamage(newDamage);
        }
        
        bool IsToolSuitable(Item item) {
            return item?.TryGetElement<Tool>()?.Type == _requiredToolType;
        }
        
        async UniTaskVoid UseTool(int delaySeconds = 1) {
            bool result = await AsyncUtil.DelayTime(Hero, delaySeconds);
            if (result) {
                Hero.Trigger(HeroToolAction.Events.HeroToolInteracted, true);
            }
        }
    }
}