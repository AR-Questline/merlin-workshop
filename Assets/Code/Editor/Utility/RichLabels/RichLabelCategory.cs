using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    [Serializable, HideLabel]
    public class RichLabelCategory {
        [SerializeField, HideLabel] string name;
        [SerializeField, HideInInspector] string guid;

        [LabelText("Labels"), SerializeField, LabelWidth(120), ListDrawerSettings(ShowFoldout = false)]
        List<RichLabel> labels;

        [field: SerializeField, LabelWidth(120)]
        public bool SingleChoice { get; protected set; }

        [field: SerializeField, LabelWidth(120)]
        public bool Immutable { get; protected set; }

        public string Name => name;
        public string Guid => guid;
        public List<RichLabel> Labels => labels;

        public RichLabelCategory() {
            this.name = "New Category";
            this.guid = System.Guid.NewGuid().ToString();
            this.labels = new List<RichLabel>();
        }

        public RichLabelCategory(string name, string guid = null, bool singleChoice = false, bool immutable = false, List<RichLabel> entries = null) {
            this.name = name;
            this.guid = guid ?? System.Guid.NewGuid().ToString();
            this.SingleChoice = singleChoice;
            this.Immutable = immutable;
            this.labels = entries ?? new List<RichLabel>();
        }
        
        public RichLabel FindLabel(string richLabelName) {
            return Labels.FirstOrDefault(label => label.Name == richLabelName);
        }
    }
}