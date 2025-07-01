// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// グリッドマップとユーティリティ関数群
    /// Jobで利用するために最低限の管理データのみ
    /// そのためGridSizeなどのデータはこのクラスでは保持しない
    /// GridSize>0である必要あり!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GridMap<T> : IDisposable where T : unmanaged, IEquatable<T>
    {
        private NativeParallelMultiHashMap<int3, T> gridMap;

        //=========================================================================================
        public GridMap(int capacity = 0)
        {
        }

        public void Dispose()
        {
        }

        public NativeParallelMultiHashMap<int3, T> GetMultiHashMap() => gridMap;

        public int DataCount => gridMap.Count();

        //=========================================================================================
        /// <summary>
        /// グリッド範囲を走査するEnumeratorを返す
        /// </summary>
        /// <param name="startGrid"></param>
        /// <param name="endGrid"></param>
        /// <returns></returns>
        public static GridEnumerator GetArea(int3 startGrid, int3 endGrid, NativeParallelMultiHashMap<int3, T> gridMap)
        {
            return default;
        }

        /// <summary>
        /// 球範囲を走査するEnumeratorを返す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static GridEnumerator GetArea(float3 pos, float radius, NativeParallelMultiHashMap<int3, T> gridMap, float gridSize)
        {
            return default;
        }

        /// <summary>
        /// グリッド走査用Enumerator
        /// </summary>
        public struct GridEnumerator : IEnumerator<int3>
        {
            internal NativeParallelMultiHashMap<int3, T> gridMap;
            internal int3 startGrid;
            internal int3 endGrid;
            internal int3 currentGrid;
            internal bool isFirst;

            public int3 Current => currentGrid;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return default;
            }

            public void Reset()
            {
            }

            public GridEnumerator GetEnumerator()
            {
                return default;
            }
        }


        //=========================================================================================
        /// <summary>
        /// 座標から３次元グリッド座標を割り出す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 GetGrid(float3 pos, float gridSize)
        {
            return default;
        }

        /// <summary>
        /// グリッドマップにデータを追加する
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddGrid(int3 grid, T data, NativeParallelMultiHashMap<int3, T> gridMap)
        {
        }

        /// <summary>
        /// 座標からグリッドマップにデータを追加する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        /// <param name="gridSize"></param>
        /// <param name="aabbRef"></param>
        /// <returns>追加されたグリッドを返す</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 AddGrid(float3 pos, T data, NativeParallelMultiHashMap<int3, T> gridMap, float gridSize)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 AddGrid(float3 pos, T data, NativeParallelMultiHashMap<int3, T>.ParallelWriter gridMap, float gridSize)
        {
            return default;
        }

        /// <summary>
        /// グリッドマップからデータを削除する
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        /// <returns>削除に成功した場合はtrue</returns>
        public static bool RemoveGrid(int3 grid, T data, NativeParallelMultiHashMap<int3, T> gridMap)
        {
            return default;
        }

        /// <summary>
        /// グリッドマップからデータを移動させる
        /// </summary>
        /// <param name="fromGrid"></param>
        /// <param name="toGrid"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        /// <returns>データが移動された場合true, 移動の必要がない場合はfalse</returns>
        public static bool MoveGrid(int3 fromGrid, int3 toGrid, T data, NativeParallelMultiHashMap<int3, T> gridMap)
        {
            return default;
        }
    }
}
