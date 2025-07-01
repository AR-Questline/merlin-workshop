using System;
using Awaken.TG.Main.Skills.Units.Masters;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Skills {
    public class VSkillMachine {
        ScriptMachineWithSkill _machine;
        public SkillMasterUnit MasterUnit { get; private set; }
        
        public ScriptMachineWithSkill Machine => _machine;
        public FlowGraph Graph => Machine.graph;

        public void Initialize(Skill skill, GameObject machineGO) {
            var graph = World.Services.Get<StreamedSkillGraphs>().Get(skill.Graph.VisualScriptGuid);
            if (graph == null) {
                Log.Critical?.Error($"No graph found for {skill.Graph.name}", skill.Graph.gameObject);
            }
            
            _machine = machineGO.AddComponent<ScriptMachineWithSkill>();
            _machine.Owner = skill;
            _machine.nest.source = GraphSource.Macro;
            _machine.nest.macro = graph;
            
            MasterUnit = _machine.graph.Unit<SkillMasterUnit>();
            
            _machine.Initialize();

            if (MasterUnit == null) {
                Debug.LogException(new Exception($"No SkillMasterUnit in graph in {skill.Graph}"), skill.Graph.gameObject);
            }
        }

        public void Discard(Skill skill) {
            World.Services.Get<StreamedSkillGraphs>().Release(skill.Graph.VisualScriptGuid);
            _machine.Discard();
            Object.Destroy(_machine);
        }
    }
}