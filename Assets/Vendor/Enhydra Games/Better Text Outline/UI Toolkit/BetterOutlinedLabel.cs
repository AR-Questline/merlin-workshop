using UnityEngine.UIElements;

namespace EnhydraGames.BetterTextOutline
{
    public class BetterOutlinedLabel : TextElement
    {
        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<BetterOutlinedLabel, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription text_Value = new UxmlStringAttributeDescription { name = "text", defaultValue = "Sample Text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as BetterOutlinedLabel;

                ate.Clear();

                ate.text = text_Value.GetValueFromBag(bag, cc);

                ate.Setup();
            }
        }

        /// <summary>
        /// Text that is displayed.
        /// </summary>
        public override string text
        {
            get { return displayedText; }
            set
            {
                displayedText = value;
                if (innerLabel != null && borderLabel != null)
                {
                    innerLabel.text = value;
                    borderLabel.text = value;
                }
            }
        }

        /// <summary>
        /// The text is saved in this variable to ensure functionality in the UI Builder.
        /// </summary>
        private string displayedText;

        private Label innerLabel;
        private Label borderLabel;

        /// <summary>
        /// Standard constructor for BorderedTextLabel.
        /// </summary>
        public BetterOutlinedLabel()
        {
            Setup();
        }

        /// <summary>
        /// Constructor for BorderedTextLabel with a text parameter.
        /// </summary>
        /// <param name="displayText">Text to be displayed in the label.</param>
        public BetterOutlinedLabel(string displayText)
        {
            text = displayText;
            Setup();
        }

        /// <summary>
        /// Sets the BetterOutlinedLabel up.
        /// </summary>
        public void Setup()
        {
            Clear();
            innerLabel = new Label(text);
            innerLabel.name = "InnerLabel";
            innerLabel.style.unityTextOutlineWidth = 0;
            innerLabel.style.color = this.style.color;
            innerLabel.style.marginBottom = 0;
            innerLabel.style.marginLeft = 0;
            innerLabel.style.marginRight = 0;
            innerLabel.style.marginTop = 0;
            innerLabel.style.paddingBottom = 0;
            innerLabel.style.paddingLeft = 0;
            innerLabel.style.paddingRight = 0;
            innerLabel.style.paddingTop = 0;
            innerLabel.style.fontSize = this.style.fontSize;

            borderLabel = new Label(text);
            borderLabel.name = "BorderLabel";
            borderLabel.Add(innerLabel);
            borderLabel.style.unityTextOutlineWidth = this.style.unityTextOutlineWidth;
            borderLabel.style.unityTextOutlineColor = this.style.unityTextOutlineColor;
            borderLabel.style.fontSize = this.style.fontSize;

            this.Add(borderLabel);
        }
    }
}