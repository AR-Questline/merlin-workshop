using System.IO;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    class HeightmapExportPNG : EditorWindow{
        static TerrainData s_terraindata;
 
        //[MenuItem("Window/Terrain to image")]
        static void Init(){
            s_terraindata = null;
            Terrain terrain = null;
 
            if (Selection.activeGameObject)
                terrain = Selection.activeGameObject.GetComponent<Terrain>();
 
            if (!terrain){
                terrain = Terrain.activeTerrain;
            }
            if (terrain){
                s_terraindata = terrain.terrainData;
            }
            if (s_terraindata == null){
                EditorUtility.DisplayDialog("No terrain selected", "Please select a terrain.", "Cancel");
                return;
            }
 
            // get the terrain heights into an array and apply them to a texture2D
            int myIndex = 0;
            Texture2D duplicateHeightMap = new Texture2D(s_terraindata.heightmapResolution, s_terraindata.heightmapResolution, TextureFormat.ARGB32, false);
            float[,] rawHeights = s_terraindata.GetHeights(0, 0, s_terraindata.heightmapResolution, s_terraindata.heightmapResolution);
 
            // run through the array row by row
            for (int y = 0; y < duplicateHeightMap.height; y++){
                for (int x = 0; x < duplicateHeightMap.width; x++){
                    // for wach pixel set RGB to the same so it's gray
                    var color = new Vector4(rawHeights[x,y], rawHeights[x,y], rawHeights[x,y], 1);
                    duplicateHeightMap.SetPixel(x, y, color);
                    myIndex++;
                }
            }
            // Apply all SetPixel calls
            duplicateHeightMap.Apply();
 
            string path = EditorUtility.SaveFilePanel(
                "Save texture as",
                "",
                "Rename Me",
                "png, jpg");
 
            var extension = Path.GetExtension(path);
            byte[] pngData = null;// duplicateHeightMap.EncodeToPNG();
 
            switch(extension){
                case ".jpg":
                    pngData = duplicateHeightMap.EncodeToJPG();
                    break;
 
                case ".png":
                    pngData = duplicateHeightMap.EncodeToPNG();
                    break;
            }
 
            if (pngData != null){
                File.WriteAllBytes(path, pngData);
                EditorUtility.DisplayDialog("Heightmap Duplicated", "Saved as" + extension + " in " + path, "Awesome");
            }else{
                EditorUtility.DisplayDialog("Failed to duplicate height map", "eh something happen hu? lol", "Check Script");
            }
 
            AssetDatabase.Refresh();
        }
    }
}
