// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// コストの昇順に４つまでデータを格納できる固定SortedList
    /// コストは０以上でなければならない
    /// 必ずコンストラクタでマイナスコストを指定してから利用すること
    /// var dlist = new ExCostSortedList4(-1);
    /// </summary>
    public struct ExCostSortedList4
    {
        internal float4 costs;
        internal int4 data;

        /// <summary>
        /// 必ずマイナス距離で初期化すること
        /// </summary>
        /// <param name="invalidCost"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ExCostSortedList4(float invalidCost) : this()
        {
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (costs[i] >= 0.0f)
                        return i + 1;
                }

                return 0;
            }
        }

        public bool IsValid => costs[0] >= 0.0f;

        public bool Add(float cost, int item)
        {
            return default;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public (float, int) Get(int index)
        //{
        //    return (costs[index], data[index]);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int item)
        {
            return default;
        }

        /// <summary>
        /// データ内のアイテムを検索してその登録インデックスを返す。(-1=なし)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int indexOf(int item)
        {
            return default;
        }

        /// <summary>
        /// データ内のアイテムを削除しデータを１つ詰める
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(int item)
        {
        }

        /// <summary>
        /// データ内の最小のコストを返す
        /// </summary>
        public float MinCost
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return costs[0];
            }
        }

        /// <summary>
        /// データ内の最大のコストを返す
        /// </summary>
        public float MaxCost
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (costs[i] >= 0.0f)
                        return costs[i];
                }
                return 0.0f;
            }
        }

        public override string ToString()
        {
            return default;
        }
    }
}
