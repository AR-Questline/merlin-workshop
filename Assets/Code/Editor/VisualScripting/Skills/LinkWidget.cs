using System;
using System.Collections.Generic;
using Awaken.TG.VisualScripts.Units.Links;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Skills {
    [Widget(typeof(IGraphLink))]
    public class LinkWidget : UnitWidget<IGraphLink> {
        public LinkWidget(FlowCanvas canvas, IGraphLink unit) : base(canvas, unit) { }

        protected override NodeColorMix baseColor => NodeColor.Green;

        protected override IEnumerable<DropdownOption> contextOptions {
            get {
                if (item is ControlLinkEnter controlEnter) {
                    yield return Option(() => GoTo<ControlLinkExit>(controlEnter), "Go to Exit");
                    yield return Option(() => GoToNext(controlEnter), "Go to next Enter");
                }
                if (item is ControlLinkExit controlExit) {
                    yield return Option(() => GoTo<ControlLinkEnter>(controlExit), "Go to Enter");
                    yield return Option(() => GoToNext(controlExit), "Go to next Exit");
                }
                if (item is ValueLinkEnter valueEnter) {
                    yield return Option(() => GoTo<ValueLinkExit>(valueEnter), "Go to Exit");
                    yield return Option(() => GoToNext(valueEnter), "Go to next Enter");
                }
                if (item is ValueLinkExit valueExit) {
                    yield return Option(() => GoTo<ValueLinkEnter>(valueExit), "Go to Enter");
                    yield return Option(() => GoToNext(valueExit), "Go to next Exit");
                }
                foreach(var option in base.contextOptions) {
                    yield return option;
                }
            }
        }
        
        static DropdownOption Option(Action action, string label) {
            return new DropdownOption(action, label);
        }

        static void GoTo<TOther>(IGraphLink me) where TOther : IGraphLink {
            var units = Units<TOther>(me, out _);
            if (units.Count > 0) {
                Focus(units[0]);
            }
        }
        
        static void GoToNext<TMe>(TMe me) where TMe : IGraphLink {
            var units = Units<TMe>(me, out var index);
            if (units.Count > 0) {
                Focus(units[(index + 1) % units.Count]);
            }
        }

        static List<T> Units<T>(IGraphLink unit, out int index) where T : IGraphLink {
            var list = new List<T>();
            index = -1;
            foreach (var u in unit.graph.units) {
                if (u is T t && t.Label == unit.Label) {
                    if (ReferenceEquals(t, unit)) {
                        index = list.Count;
                    }
                    list.Add(t);
                }
            }
            return list;
        }
        
        static void Focus(IUnit unit) {
            GraphWindow.active.context.graph.zoom = 1f;
            GraphWindow.active.context.graph.pan = unit.position;
        }
    }
}