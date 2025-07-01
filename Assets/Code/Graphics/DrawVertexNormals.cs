using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Graphics {
#if UNITY_EDITOR
    [RequireComponent(typeof(MeshFilter))]
    public class DrawVertexNormals : MonoBehaviour {
        [SerializeField] float normalLength = 2f;
        [SerializeField] Color normalColor = Color.cyan;
        [SerializeField] bool drawNormals = true;

        Vector3[] _vertices;
        Vector3[] _normals;

        void OnValidate() {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            if (mesh) {
                _vertices = mesh.vertices;
                _normals = mesh.normals;
            } else {
                _vertices = Array.Empty<Vector3>();
                _normals = Array.Empty<Vector3>();
            }
        }

        void OnDrawGizmos() {
            if (!drawNormals) return;

            Handles.color = normalColor;
            Handles.matrix = transform.localToWorldMatrix;
            for (int i = 0; i < _vertices.Length; i++) {
                Handles.DrawLine(_vertices[i], _vertices[i] + _normals[i] * normalLength);
            }
        }
    }
#else
    public class DrawVertexNormals : MonoBehaviour { }
#endif
}