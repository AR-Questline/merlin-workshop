using XNode;

namespace Awaken.TG.Main.Stories.Extensions {
    public static class NodePortExtensions {
        public static Node ConnectedNode(this NodePort port) {
            return port.Connection?.node;
        }
    }
}