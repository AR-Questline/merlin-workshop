/*
Copyright (C) 2016 Dmitry Denisov

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using Awaken.TG.Main.Heroes.FootSteps;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Graphics {
    public class TextureArrayCreator : EditorWindow {

        int ArraySize = 1;
        int Width=128;
        int Height=128;

        Texture2D[] Textures = new Texture2D[1];
        Vector2 scrollPos ;

        string Path = "Assets/";
        string FileName = "NewTextureArray";
    
        bool groupEnabled;

        TextureFormat TexFormat = TextureFormat.DXT1;
        bool UseMips = true;
        bool IsLinear = false;
        FilterMode FiltMod = FilterMode.Trilinear;
        int aniso = 8;
        
        [MenuItem("ArtTools/Texture Array Creator")]
        static void Init() {
            TextureArrayCreator window = (TextureArrayCreator)EditorWindow.GetWindow(typeof(TextureArrayCreator));
            window.Show();
        }

        void OnGUI() {
            GUILayout.Label("File Settings", EditorStyles.boldLabel);
            FileName = EditorGUILayout.TextField("File Name: ", FileName);
            Path = EditorGUILayout.TextField("File Path: ", Path);
            GUILayout.Label("Texture Settings", EditorStyles.boldLabel);
        
            ArraySize = Mathf.Clamp(EditorGUILayout.IntField("Size", ArraySize),1,1023);

            groupEnabled = EditorGUILayout.BeginToggleGroup("Manual Settings", groupEnabled);
            Width = Mathf.Clamp(EditorGUILayout.IntField("Width", Width), 1, 8192);
            Height = Mathf.Clamp(EditorGUILayout.IntField("Height", Height), 1, 8192);
            FiltMod =(FilterMode)EditorGUILayout.EnumPopup("Texture Filtering", FiltMod);
            if (FiltMod != FilterMode.Point) {
                aniso = Mathf.Clamp(EditorGUILayout.IntField("Aniso Level", aniso), 0, 16);
            }
            UseMips = EditorGUILayout.Toggle("Generate Mip maps", UseMips);
            IsLinear = EditorGUILayout.Toggle("Is Linear", IsLinear);
            TexFormat = (TextureFormat)EditorGUILayout.EnumPopup("Texture Format", TexFormat);
            if (GUILayout.Button("Auto Setup by first texture")) {
                AutoAdjustByFirstTex();
            }

            EditorGUILayout.EndToggleGroup(); 

            if (ArraySize != Textures.Length) {
                Array.Resize<Texture2D>(ref Textures, ArraySize);
            }
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < ArraySize; i++) {
                Textures[i] = (Texture2D)EditorGUILayout.ObjectField("Element " +i+":", Textures[i], typeof(Texture2D),false);
            }
            EditorGUILayout.EndScrollView();

            if (CheckArray()) {
                if (GUILayout.Button("Create Texture Array"))
                {
                    CreateTextureArray();
                }
            }
            else {
                GUILayout.Label("Textures for array are not match each other", EditorStyles.boldLabel);
            }
        }
        void AutoAdjustByFirstTex() {
            if (Textures[0] != null)
            {
                Width = Textures[0].width;
                Height = Textures[0].height;
                FiltMod = Textures[0].filterMode;
                UseMips = Textures[0].mipmapCount != 1;
                TexFormat = Textures[0].format;
                aniso = Textures[0].anisoLevel;
            }
        }
        bool CheckArray() {
            for (int i = 0; i < Textures.Length; i++) {
                if (Textures[i] != null&& Textures[i].width== Textures[0].width&& Textures[i].height== Textures[0].height && Textures[i].format== Textures[0].format&& Textures[i].mipmapCount== Textures[0].mipmapCount)
                
                    continue;
                else {
                    return false;
                }
            }
            return true;
        }
        void CreateTextureArray() {
            if (!groupEnabled) {
                AutoAdjustByFirstTex();
            }
            
            Texture2DArray textureArray = new Texture2DArray(Width, Height, ArraySize, TexFormat, UseMips, IsLinear);
            for (int i = 0; i < ArraySize; i++) {
                for (int j = 0; j < Textures[i].mipmapCount; j++) {
                    UnityEngine.Graphics.CopyTexture(Textures[i], 0, j, textureArray, i, j);
                }
                Log.Important?.Info((i + 1) + " of " + ArraySize + " textures are moved to array");
            }
            textureArray.filterMode = FiltMod;
            textureArray.anisoLevel = aniso;
            textureArray.Apply();
            AssetDatabase.CreateAsset(textureArray, Path+"/"+ FileName+".asset");
            Log.Important?.Info("Texture Array " + FileName + ".asset" + " has been successfully created in: " + Path + "/");
        }
    }
}