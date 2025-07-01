using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Debugging {
    public class MissSetupValidator : OdinEditorWindow {
        [OnValueChanged(nameof(CalculateErrors))]
        public SetupMode mode = SetupMode.None;

        [ShowIf(nameof(IsNpcFighterMode))]
        [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, IsReadOnly = true)]
        [ReadOnly]
        public List<ErrorWithTargets<NpcTemplate>> npsFighterTemplatesErrors = new();

        protected override void OnEnable() {
            base.OnEnable();
            CalculateErrors();
        }

        void CalculateErrors() {
            if (IsNpcFighterMode()) {
                CalculateNpcFighterTemplates();
            }
        }

        // === Errors calculating
        void CalculateNpcFighterTemplates() {
            npsFighterTemplatesErrors.Clear();
            
            var allTemplates = TemplatesSearcher.FindAllOfType<NpcTemplate>();

            var emptyDifficulty = new ErrorWithTargets<NpcTemplate>("Has empty difficulty tag");
            npsFighterTemplatesErrors.Add(emptyDifficulty);
            
            foreach (var fighterTemplate in allTemplates) {
                // Difficulty tag is required
                if (string.IsNullOrWhiteSpace(fighterTemplate.DifficultyTag)) {
                    emptyDifficulty.Add(fighterTemplate);
                }
            }
        }

        // === Mode checking
        bool IsNpcFighterMode() => mode.HasFlagFast(SetupMode.NpcFighter);
        
        [Flags]
        public enum SetupMode {
            None = 0,
            NpcFighter = 1 << 0,
            
            All = -1,
        }

        // === Helper structures
        public class ErrorWithTargets<T> where T : Object {
            [ShowInInspector]
            public string Message { get; }
            [ShowInInspector]
            public List<T> Targets { get; }

            public ErrorWithTargets(string message) {
                Targets = new List<T>();
                this.Message = message;
            }

            public void Add(T target) {
                Targets.Add(target);
            }
        }
        
        #region Show window
        [MenuItem("TG/Assets/Miss setup", false, 2000)]
        static void OpenWindow() {
            var window = GetWindow<MissSetupValidator>();
            window.Show();
        }
        #endregion Show window
    }
}