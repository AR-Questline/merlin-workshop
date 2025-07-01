using System.Collections.Generic;
using XNode;

namespace Awaken.TG.Editor.Main.Stories.AutoGen {
    public abstract class NodeDataElement {
        const float ApproximateNodeHeight = 168;

        protected NodeDataElement(int id = -1, int depth = 0) {
            ID = id;
            Depth = depth;
        }

        public int ID { get; }
        public int Depth { get; }
        public virtual float ApproximateGuiHeight => ApproximateNodeHeight;
    }

    public class BookmarkNodeDataElement : NodeDataElement {
        const float ApproximateBookmarkHeight = 450;
        
        public BookmarkNodeDataElement(int id, int depth, string bookmarkName, int quitNodeID) : base(id, depth) {
            BookmarkName = bookmarkName;
            QuitNodeID = quitNodeID;
        }
        
        public string BookmarkName { get; set; }
        public int QuitNodeID { get; set; }

        public override float ApproximateGuiHeight => ApproximateBookmarkHeight;
    }
    
    
    public class TextNodeDataElement : NodeDataElement {
        const float ApproximateMessageHeight = 200;

        public TextNodeDataElement(int id, int depth, int quitNodeID = -1, bool isSeparate = false, bool isRandomized = false) : base(id, depth) {
            QuitNodeID = quitNodeID;
            IsSeparate = isSeparate;
            IsRandomized = isRandomized;
        }

        public int QuitNodeID { get; set; }
        public bool IsSeparate { get; set; }
        public bool IsRandomized { get; set; }
        public List<Message> Message { get; } = new();

        public override float ApproximateGuiHeight =>
            base.ApproximateGuiHeight + ApproximateMessageHeight * Message.Count;
    }

    public class ChoiceNodeDataElement : NodeDataElement {
        const float ApproximateChoiceHeight = 110;

        public ChoiceNodeDataElement(int id, int depth) : base(id, depth) { }

        public List<ChoiceData> Choices { get; } = new();

        public override float ApproximateGuiHeight =>
            base.ApproximateGuiHeight + ApproximateChoiceHeight * Choices.Count;
    }

    public class ChoiceData {
        public ChoiceData(string message, int quitNodeID = -1) {
            Message = message;
            QuitNodeID = quitNodeID;
        }

        public string Message { get; }
        public int QuitNodeID { get; set; }
    }

    public struct Message {
        public Message(string text, string speaker, string listener) {
            Text = text;
            Speaker = speaker;
            Listener = listener;
        }

        public string Text { get;  }
        public string Speaker { get; }
        public string Listener { get; }
    }
}