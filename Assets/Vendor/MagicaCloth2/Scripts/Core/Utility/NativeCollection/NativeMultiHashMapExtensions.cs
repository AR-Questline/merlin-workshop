// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeMultiHashMapの拡張メソッド
    /// </summary>
    public static class NativeMultiHashMapExtensions
    {
        /// <summary>
        /// NativeParallelMultiHashMapのキーに指定データが存在するか判定する
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static bool MC2Contains<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static bool MC2Contains<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            return default;
        }

        /// <summary>
        /// NativeParallelMultiHashMapキーに対して重複なしのデータを追加する
        /// すでにキーに同じデータが存在する場合は追加しない。
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
#if MC2_COLLECTIONS_200
        public static void MC2UniqueAdd<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static void MC2UniqueAdd<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
        }

        /// <summary>
        /// NativeMultiHashMapのキーに存在するデータを削除する
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static bool MC2RemoveValue<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static bool MC2RemoveValue<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            return default;
        }

        /// <summary>
        /// 現在のキーのデータをFixedList512Bytesに変換して返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static FixedList512Bytes<TValue> MC2ToFixedList512Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static FixedList512Bytes<TValue> MC2ToFixedList512Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : struct, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#endif
        {
            return default;
        }

        /// <summary>
        /// 現在のキーのデータをFixedList128Bytesに変換して返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static FixedList128Bytes<TValue> MC2ToFixedList128Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static FixedList128Bytes<TValue> MC2ToFixedList128Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : struct, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#endif
        {
            return default;
        }

        /// <summary>
        /// NativeParallelMultiHashMapをKeyとValueの配列に変換します
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static (TKey[], TValue[]) MC2Serialize<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
#else
        public static (TKey[], TValue[]) MC2Serialize<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map) where TKey : struct, IEquatable<TKey> where TValue : struct
#endif
        {
            return default;
        }

        /// <summary>
        /// KeyとValueの配列からNativeParallelMultiHashMapを復元します
        /// 高速化のためBurstを利用
        /// ジェネリック型ジョブは明示的に型を指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <param name="keyArray"></param>
        /// <param name="valueArray"></param>
        /// <returns></returns>
        public static NativeParallelMultiHashMap<int2, ushort> MC2Deserialize(int2[] keyArray, ushort[] valueArray)
        {
            return default;
        }

        [BurstCompile]
#if MC2_COLLECTIONS_200
        struct SetParallelMultiHashMapJob<TKey, TValue> : IJob where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
#else
        struct SetParallelMultiHashMapJob<TKey, TValue> : IJob where TKey : struct, IEquatable<TKey> where TValue : struct
#endif
        {
            public NativeParallelMultiHashMap<TKey, TValue> map;
            [Unity.Collections.ReadOnly]
            public NativeArray<TKey> keyArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TValue> valueArray;

            public void Execute()
            {
            }
        }
    }
}
