using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.VisualScripts.States;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.VisualScripting.StateGraphs {
    [GraphContextExtension(typeof(StateGraphContext))]
    public class ARGraphContextExtension : GraphContextExtension<StateGraphContext> {
        static readonly List<StateItem> _states;
        
        static ARGraphContextExtension() {
            _states = TypeCache.GetTypesDerivedFrom<IARStateUnit>()
                .Where(t => !t.IsAbstract)
                .Select(t => new StateItem(t))
                .ToList();
        }
        
        
        public ARGraphContextExtension(StateGraphContext context) : base(context) { }

        public override IEnumerable<GraphContextMenuItem> contextMenuItems => _states.Select(s => s.AsContextMenuItem(context.canvas));

        struct StateItem {
            Type _type;
            string _label;

            public StateItem(Type type) {
                _type = type;
                _label = $"Create AR State/{type.Name[..^4]}";
            }

            public GraphContextMenuItem AsContextMenuItem(StateCanvas canvas) {
                return new GraphContextMenuItem(UnitSpawner(canvas, _type), _label);
            }
            
            static Action<Vector2> UnitSpawner(StateCanvas canvas, Type type) {
                return position => {
                    var state = (IState) type.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                    canvas.AddState(state, position);
                };
            }
        }
    }
}