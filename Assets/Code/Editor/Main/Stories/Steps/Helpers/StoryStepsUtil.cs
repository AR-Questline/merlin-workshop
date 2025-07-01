using System;
using System.Linq;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps.Helpers;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Steps.Helpers {
    public class StoryStepsUtil {
        public static bool DuplicatedName(NodeElement target, string name) {
            int count = 0;
            
            foreach (var node in target.genericParent.graph.nodes.OfType<StoryNode>()) {
                foreach (var element in node.elements) {
                    if (element is IOncePer timeSpanned) {
                        if (timeSpanned.SpanFlag == name) {
                            count++;
                        }
                    }
                }
            }
            return count > 1;
        }
    
        public static void AssignName(NodeElement target, Action<string> assign) {
            int max = 0;

            foreach (var node in target.genericParent.graph.nodes.OfType<StoryNode>()) {
                foreach (var element in node.elements) {
                    if (element is IOncePer timeSpanned) {
                        string lastNumberString = new string(timeSpanned.SpanFlag?
                            .Reverse()
                            .SkipWhile(c => !char.IsDigit(c))
                            .TakeWhile(char.IsDigit)
                            .Reverse().ToArray());

                        int.TryParse(lastNumberString, out int number);
                        if (number >= max) {
                            max = number;
                        }
                    }
                }
            }

            assign($"once.{max + 1}");
            EditorUtility.SetDirty(target);
        }
    }
}