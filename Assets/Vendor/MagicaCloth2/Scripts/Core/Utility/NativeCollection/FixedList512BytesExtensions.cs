// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    //[BurstCompatible]
    public static class FixedList512BytesExtensions
    {
        //=====================================================================
        // Common
        //=====================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MC2IsCapacity<T>(ref this FixedList512Bytes<T> fixedList) where T : unmanaged, IEquatable<T>
        {
            return default;
        }

        //=====================================================================
        // Set
        //=====================================================================
        /// <summary>
        /// データが存在しない場合のみ追加する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixedList"></param>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2Set<T>(ref this FixedList512Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
        }

        /// <summary>
        /// データが存在しない場合のみ追加する
        /// すでに容量が一杯の場合は警告を表示し追加しない。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixedList"></param>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2SetLimit<T>(ref this FixedList512Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
        }

        /// <summary>
        /// リストからデータを検索して削除する
        /// 削除領域にはリストの最後のデータが移動する(SwapBack)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixedList"></param>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2RemoveItemAtSwapBack<T>(ref this FixedList512Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
        }

        //=====================================================================
        // Stack
        //=====================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2Push<T>(ref this FixedList512Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MC2Pop<T>(ref this FixedList512Bytes<T> fixedList) where T : unmanaged, IEquatable<T>
        {
            return default;
        }

        //=====================================================================
        // Queue
        //=====================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2Enqueue<T>(ref this FixedList512Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MC2Dequque<T>(ref this FixedList512Bytes<T> fixedList) where T : unmanaged, IEquatable<T>
        {
            return default;
        }
    }
}
