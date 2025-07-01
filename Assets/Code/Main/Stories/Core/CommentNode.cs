using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Awaken.TG.Main.Stories.Core {
    public class CommentNode : StoryNode {
        public override Type GenericType => typeof(CommentNode);

        public int width = 512;
        public int height = 64;
        [TextArea(1, 10)] [UnityEngine.Scripting.Preserve]
        public string comment;

        public IEnumerable<Node> ContainedNodes() {
            List<Node> result = new List<Node>();
            foreach (Node node in graph.nodes) {
                if (node == this) {
                    continue;
                }
                if (node.position.x < this.position.x) {
                    continue;
                }
                if (node.position.y < this.position.y) {
                    continue;
                }
                if (node.position.x > this.position.x + width) {
                    continue;
                }
                if (node.position.y > this.position.y + height + 30) {
                    continue;
                }
                result.Add(node);
            }
            return result;
        }
    }
}