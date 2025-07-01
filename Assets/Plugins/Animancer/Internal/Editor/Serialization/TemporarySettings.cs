// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// Stores data which needs to survive assembly reloading (such as from script compilation), but can be discarded
    /// when the Unity Editor is closed.
    /// </summary>
    internal class TemporarySettings : ScriptableObject
    {
        /************************************************************************************************************************/
        #region Instance
        /************************************************************************************************************************/

        private static TemporarySettings _Instance;

        /// <summary>Finds an existing instance of this class or creates a new one.</summary>
        private static TemporarySettings Instance
        {
            get
            {
                if (_Instance == null)
                {
                    var instances = Resources.FindObjectsOfTypeAll<TemporarySettings>();
                    if (instances.Length > 0)
                    {
                        _Instance = instances[0];
                    }
                    else
                    {
                        _Instance = CreateInstance<TemporarySettings>();
                        _Instance.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                    }
                }

                return _Instance;
            }
        }

        /************************************************************************************************************************/

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Event Selection
        /************************************************************************************************************************/

        private readonly Dictionary<Object, Dictionary<string, int>>
            ObjectToPropertyPathToSelectedEvent = new Dictionary<Object, Dictionary<string, int>>();

        /************************************************************************************************************************/

        public static int GetSelectedEvent(SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/

        public static void SetSelectedEvent(SerializedProperty property, int eventIndex)
        {
        }

        /************************************************************************************************************************/

        private static Dictionary<string, int> GetOrCreatePathToSelection(Object obj)
        {
            return default;
        }

        /************************************************************************************************************************/

        [SerializeField] private Serialization.ObjectReference[] _EventSelectionObjects;
        [SerializeField] private string[] _EventSelectionPropertyPaths;
        [SerializeField] private int[] _EventSelectionIndices;

        /************************************************************************************************************************/

        private void OnDisableSelection()
        {
        }

        /************************************************************************************************************************/

        private void OnEnableSelection()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Preview Models
        /************************************************************************************************************************/

        [SerializeField]
        private List<GameObject> _PreviewModels;
        public static List<GameObject> PreviewModels
        {
            get
            {
                var instance = Instance;
                AnimancerEditorUtilities.RemoveMissingAndDuplicates(ref instance._PreviewModels);
                return instance._PreviewModels;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
