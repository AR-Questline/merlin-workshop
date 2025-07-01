using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit {
    public class UIDocumentProvider : MonoBehaviour, IService {
        [SerializeField] List<UIDocumentData> documents;
        
        public UIDocument TryGetDocument(UIDocumentType type) {
            return documents.FirstOrDefault(documentData => documentData.TryGetDocument(type) is not null).UIDocument;
        }
        
        [Serializable]
        struct UIDocumentData {
            [SerializeField] UIDocumentType type;
            [field: SerializeField] public UIDocument UIDocument { get; private set; }
        
            public UIDocument TryGetDocument(UIDocumentType targetType) {
                return this.type == targetType ? UIDocument : null;
            }
        }
    }
    
    /// <summary>
    /// The type of document to use for UI presenters
    /// Used to determine where the UI is displayed
    /// Each UIDocument could have a different settings 
    /// </summary>
    public enum UIDocumentType {
        /// <summary>
        /// For elements which are managed externally by other objects to determine where they are displayed
        /// </summary>
        [UnityEngine.Scripting.Preserve] Inherit,
        /// <summary>
        /// For full screen elements
        /// </summary>
        [UnityEngine.Scripting.Preserve] Default,
        /// <summary>
        /// For elements that do not cover the whole display
        /// </summary>
        [UnityEngine.Scripting.Preserve] HUD,
        [UnityEngine.Scripting.Preserve] Marvin
    }
}