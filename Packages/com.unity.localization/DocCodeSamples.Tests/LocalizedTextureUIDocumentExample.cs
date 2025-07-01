#if MODULE_UITK && UNITY_2023_3_OR_NEWER

#region example
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public class LocalizedTextureUIDocumentExample : MonoBehaviour
{
    void Start()
    {
        var document = GetComponent<UIDocument>();
        var label = new Label();
        label.text = "Some localized image";
        document.rootVisualElement.Add(label);

        // Add binding to the background style property of the label.
        var localizedTexture = new LocalizedTexture { TableReference = "My asset table", TableEntryReference = "My Texture" };
        label.SetBinding("style.backgroundImage", localizedTexture);
    }
}
#endregion

#endif
