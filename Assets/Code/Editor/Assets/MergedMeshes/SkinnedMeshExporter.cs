using Awaken.TG.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.MergedMeshes {
    public static class SkinnedMeshExporter {
        public static Mesh Export(string directory, string name, Mesh mesh, Transform[] allBones, Transform[] usedBones) {
            // var fbxGO = new GameObject(name);
            // var fbxTransform = fbxGO.transform;
            // using var fbxGODestroyScope = fbxGO.DestroyImmediateScope();
            //
            // var copiedAllBones = new Transform[allBones.Length];
            // copiedAllBones[0] = new GameObject(allBones[0].name).transform;
            // copiedAllBones[0].SetTransform(fbxTransform, allBones[0]);
            // for (int i = 1; i < allBones.Length; i++) {
            //     var parent = Find(copiedAllBones, allBones[i].parent, i);
            //     copiedAllBones[i] = new GameObject(allBones[i].name).transform;
            //     copiedAllBones[i].SetTransform(parent, allBones[i]);
            // }
            //
            // var copiedUsedBones = new Transform[usedBones.Length];
            // for (int i = 0; i < usedBones.Length; i++) {
            //     copiedUsedBones[i] = Find(copiedAllBones, usedBones[i], copiedAllBones.Length);
            // }
            //
            // var materials = new Material[mesh.subMeshCount];
            // var shader = Shader.Find("HDRP/Lit");
            // for (int i = 0; i < materials.Length; i++) {
            //     materials[i] = new Material(shader);
            // }
            //
            // var renderer = fbxGO.AddComponent<SkinnedMeshRenderer>();
            // renderer.sharedMesh = mesh;
            // renderer.sharedMaterials = materials;
            // renderer.bones = copiedUsedBones;
            //
            // ModelExporter.ExportObject($"{Application.dataPath}/{directory}/{name}.fbx", fbxGO);
            // var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/{directory}/{name}.fbx");
            // return fbx.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            return null;
        }

        static void SetTransform(this Transform me, Transform parent, Transform other) {
            other.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            me.SetTransform(parent, localPosition, localRotation, other.localScale);
        }
        
        static void SetTransform(this Transform me, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale) {
            me.SetParent(parent);
            me.SetLocalPositionAndRotation(localPosition, localRotation);
            me.localScale = localScale;
        }

        static Transform Find(Transform[] array, Transform toFind, int count) {
            string name = toFind.name;
            for (int i = 0; i < count; i++) {
                if (array[i].name == name) {
                    return array[i];
                }
            }
            return null;
        }
    }
}