//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UNITY_2020_1_OR_NEWER
	#define UNITY_UI_PREMULTIPLIED
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.UIFX
{
	public static class Matrix4x4Helper
	{
		public static Matrix4x4 Rotate(Quaternion rotation)
        {
            return default;
        }
    }

	public static class ObjectHelper
	{
		public static void Destroy<T>(ref T obj) where T : Object
        {
        }

        public static void Destroy<T>(T obj) where T : Object
        {
        }

        public static void Dispose<T>(ref T obj) where T : System.IDisposable
        {
        }

        public static bool ChangeProperty<T>(ref T backing, T value) where T : struct
        {
            return default;
        }

        public static void ChangeProperty<T>(ref T backing, T value, ref bool hasChanged) where T : struct
        {
        }
    }

	public static class RenderTextureHelper
	{
		public static void ReleaseTemporary(ref RenderTexture rt)
        {
        }
    }

	public static class VertexHelperExtensions
	{
		public static void ReplaceUIVertexTriangleStream(this VertexHelper vh, List<UIVertex> vertices)
        {
        }
    }

	public static class MaterialHelper
	{
		public static bool MaterialOutputsPremultipliedAlpha(Material material)
        {
            return default;
        }
    }

    public static class EditorHelper
    {
        public static bool IsInContextPrefabMode()
        {
            return default;
        }
    }

	#if !UNITY_2019_2_OR_NEWER
	/// Prior to 2019.2 [InspectorName] (use to rename enums in the inspector) is internal in UnityEngine, so we just declare the stub here to fix compile issues
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	internal class InspectorNameAttribute : PropertyAttribute
	{
		public InspectorNameAttribute(string displayName) {}
	}
	#endif

	#if !UNITY_2018_3_OR_NEWER
	/// Prior to 2018.3 [ExecuteAlways] didn't exist, so we just declare the stub here to fix compile issues
	public sealed class ExecuteAlways : System.Attribute
	{
		public ExecuteAlways() {}
	}
	#endif
}