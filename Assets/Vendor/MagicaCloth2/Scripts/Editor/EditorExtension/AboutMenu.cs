// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Aboutダイアログ
    /// </summary>
    public class AboutMenu : EditorWindow
    {
        [SerializeField]
        private Texture2D image = null;

        public const string MagicaClothVersion = "2.8.0";

        public static AboutMenu AboutWindow { get; set; }
        private const float windowWidth = 300;
        private const float windowHeight = 220;

        private const string webUrl = "https://magicasoft.jp/en/magica-cloth-2-2/";

        //=========================================================================================
        [MenuItem("Tools/Magica Cloth2/About", false)]
        static void InitWindow()
        {
        }

        //=========================================================================================
        private void Awake()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
        }

        private void OnGUI()
        {
        }

        private void OnInspectorUpdate()
        {
        }
    }
}
