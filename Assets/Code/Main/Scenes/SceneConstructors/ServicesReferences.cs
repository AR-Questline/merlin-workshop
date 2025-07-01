using Awaken.TG.Graphics.FloatingTexts;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.Executions;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Skills;
using UnityEngine;
using Cursor = Awaken.TG.Main.UI.Cursors.Cursor;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    /// <summary>
    /// Prefab that contains references to services that should exist through entire flow of the game
    /// </summary>
    public class ServicesReferences : MonoBehaviour {
        public Cursor cursor;
        public FloatingTextService floatingText;
        public TransitionService transitionService;
        public FactionProvider factionProvider;
        public MitigatedExecution mitigatedExecution;
        public IdleBehavioursRefresher idleRefresher;
        public VSkillMachineParent vSkillMachineParent;
    }
}
