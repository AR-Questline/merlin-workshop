using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.NPCs {
    [UnitCategory("AR/NPCs")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class NPCBleedingOutUnit  : ARUnit, ISkillUnit {
        protected override void Definition() {
            var targetNPC = RequiredARValueInput<NpcElement>("Target NPC");
            var shouldBleedOut = InlineARValueInput("Should Bleed Out", true);
            var bleedoutDelay = InlineARValueInput("Bleedout Delay", 15f);
            var bleedoutHealthPercentage = InlineARValueInput("Bleedout Health Percentage", 0.05f);
            
            DefineNoNameAction(flow => {
                var npc = targetNPC.Value(flow);
                if (npc == null) return;

                if (shouldBleedOut.Value(flow)) {
                    npc.TryGetElement<NPCBleedingOut>()?.Discard();
                    npc.AddElement(new NPCBleedingOut(bleedoutDelay.Value(flow), bleedoutHealthPercentage.Value(flow)));
                } else {
                    npc.TryGetElement<NPCBleedingOut>()?.Discard();
                }
            });
        }
    }

    public partial class NPCBleedingOut : Element<NpcElement> {
        public override bool IsNotSaved => true;

        readonly float _bleedoutDelay;
        readonly float _bleedoutHealthPercentage;
        
        float _elapsedTime;
        float _lastDamageTime;
        bool _wasVisibleOnce;

        public NPCBleedingOut(float bleedoutDelay, float bleedoutHealthPercentage) {
            var deviation = bleedoutDelay * 0.1f;
            _bleedoutDelay = bleedoutDelay + Random.Range(-deviation, deviation);
            _bleedoutHealthPercentage = bleedoutHealthPercentage;
        }
        
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            ParentModel.ParentModel.OnVisualLoaded(_ => Hero.Current.GetOrCreateTimeDependent().WithUpdate(Update));
            ParentModel.ParentModel.ListenTo(Location.Events.LocationVisibilityChanged, OnLocationVisibilityChanged, this);
        }

        void OnLocationVisibilityChanged(bool obj) {
            if (obj) {
                _wasVisibleOnce = true;
                return;
            }
            if (_wasVisibleOnce) {
                OnLocationHidden().Forget();
            }
        }

        async UniTaskVoid OnLocationHidden() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            var location = ParentModel.ParentModel;

            ParentModel.HealthElement?.Kill();
            if (!location.HasBeenDiscarded) {
                location.Discard();
            }
        }

        void Update(float deltaTime) {
            if (!ParentModel.IsAlive) return;

            _elapsedTime += deltaTime;
            
            if (_elapsedTime >= _bleedoutDelay) {
                float deltaTimeGrouped = _elapsedTime - _lastDamageTime;
                
                if (deltaTimeGrouped >= 1f) {
                    _lastDamageTime = _elapsedTime;
                
                    float extraDamagePercentage = (_elapsedTime - _bleedoutDelay) * _bleedoutHealthPercentage / 2;
                
                    LimitedStat health = ParentModel.Health;
                    health.DecreaseBy(ParentModel.MaxHealth * (_bleedoutHealthPercentage + extraDamagePercentage * deltaTimeGrouped));
                    if (health <= 0) {
                        ParentModel.HealthElement.Kill();
                    }
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Hero.Current?.GetTimeDependent()?.WithoutUpdate(Update);
        }
    }
}