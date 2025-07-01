using UnityEngine;
using UnityEngine.Localization;

// To support Edit mode we need to use ExecuteAlways.
[ExecuteAlways]
[RequireComponent(typeof(TextMesh))]
public class EditModeSupportExample : MonoBehaviour
{
    public LocalizedString localizedString;
    TextMesh m_TextMesh;

    void Start()
    {
        localizedString.StringChanged += UpdateTextMesh;
        m_TextMesh = GetComponent<TextMesh>();
    }

    void UpdateTextMesh(string text)
    {
        // This will let us make temporary changes to a serialized property.
        // When the Locale is changed back to None the changes will be reverted
        // back to the original value. This must be called before we make any changes.
        // Calling this in a player build will do nothing.
        EditorPropertyDriver.RegisterProperty(m_TextMesh, "m_Text");

        m_TextMesh.text = text;
    }
}
