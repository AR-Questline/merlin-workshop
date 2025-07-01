using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Skills {
    public class VSkillMachineParent : MonoBehaviour, IService {
        public void Initialize(Skill skill, VSkillMachine machine) {
#if UNITY_EDITOR
            if (DebugReferences.SkillMachinesOnSeparateObjects) {
                var machineGO = new GameObject(skill.DebugName);
                machineGO.transform.SetParent(this.transform);
                machine.Initialize(skill, machineGO);
            } else {
                machine.Initialize(skill, this.gameObject);
            }
#else
            machine.Initialize(skill, this.gameObject);
#endif
        }

        public void Discard(Skill skill, VSkillMachine machine) {
#if UNITY_EDITOR
            var machineGO = machine.Machine.gameObject;
            machine.Discard(skill);
            if (machineGO != this.gameObject) {
                Object.Destroy(machineGO);
            }
#else 
            machine.Discard(skill);
#endif
        }
    }
}