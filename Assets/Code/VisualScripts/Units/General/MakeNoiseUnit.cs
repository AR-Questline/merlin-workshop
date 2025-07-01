using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/AI_Systems")]
    [UnityEngine.Scripting.Preserve]
    public class MakeNoiseUnit : ARUnit {
        InlineValueInput<float> _strength;
        RequiredValueInput<Vector3> _center;
        OptionalValueInput<IWithFaction> _source;
        OptionalValueInput<float> _range;
        FallbackValueInput<NoiseStrength> _noiseStrength;
        FallbackValueInput<bool> _ignoreWalls;

        protected override void Definition() {
            _strength = InlineARValueInput(nameof(_strength), 0f);
            _center = RequiredARValueInput<Vector3>(nameof(_center));
            _source = OptionalARValueInput<IWithFaction>(nameof(_source));
            _range = OptionalARValueInput<float>(nameof(_range));
            _noiseStrength = FallbackARValueInput(nameof(_noiseStrength), _ => NoiseStrength.VeryStrong);
            _ignoreWalls = FallbackARValueInput(nameof(_ignoreWalls), _ => true);
            
            var (enter, _) = DefineSimpleAction(Enter);

            Requirement(_strength.Port, enter);
        }

        void Enter(Flow flow) {
            var strength = _strength.Value(flow);
            var center = _center.Value(flow);
            NoiseStrength noiseStrength = _noiseStrength.Value(flow);
            bool ignoreWalls = _ignoreWalls.Value(flow);

            if (_source.HasValue) {
                var source = _source.Value(flow);
                AINoises.MakeNoise(strength, noiseStrength, ignoreWalls, center, source);
            } else if (_range.HasValue) {
                var hearingNpcs = World.Services.Get<NpcGrid>().GetHearingNpcs(center, _range.Value(flow));
                foreach (var npc in hearingNpcs) {
                    AINoises.MakeNoise(strength, noiseStrength.Value, ignoreWalls, center, npc.NpcAI);
                }
            } else {
                Log.Important?.Error("Neither source nor range is assigned to MakeNoiseUnit", flow.stack.self);
            }
        }
    }
}
