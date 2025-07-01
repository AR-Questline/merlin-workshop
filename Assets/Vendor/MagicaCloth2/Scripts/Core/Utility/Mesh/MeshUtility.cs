// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    public static class MeshUtility
    {
        /// <summary>
        /// レンダラーからSharedMeshを取得する
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public static Mesh GetSharedMesh(Renderer ren)
        {
            return default;
        }

        /// <summary>
        /// レンダラーにメッシュを設定する
        /// </summary>
        /// <param name="ren"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static bool SetMesh(Renderer ren, Mesh mesh, Transform[] skinBones = null)
        {
            return default;
        }

        /// <summary>
        /// このレンダラーが利用しているTransformの数を返す
        /// この数は概算であり正確ではないので注意！
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public static int GetTransformCount(Renderer ren)
        {
            return default;
        }
    }
}
