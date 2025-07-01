// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;

namespace MagicaCloth2
{
    /// <summary>
    /// 頂点の属性
    /// </summary>
    [System.Serializable]
    public struct VertexAttribute : IEquatable<VertexAttribute>
    {
        /// <summary>
        /// ビットフラグ
        /// </summary>
        public const byte Flag_Fixed = 0x01; // 固定
        public const byte Flag_Move = 0x02; // 移動
        //public const byte Flag_Ignore = 0x04; // 無視（シミュレーションの対象としない）:一旦オミット!
        public const byte Flag_InvalidMotion = 0x08; // モーション制約無効
        public const byte Flag_DisableCollision = 0x10; // コリジョン無効
        public const byte Flag_Triangle = 0x80; // この頂点はトライアングルに属している

        public static readonly VertexAttribute Invalid = new VertexAttribute();
        public static readonly VertexAttribute Fixed = new VertexAttribute(Flag_Fixed);
        public static readonly VertexAttribute Move = new VertexAttribute(Flag_Move);
        public static readonly VertexAttribute DisableCollision = new VertexAttribute(Flag_DisableCollision);

        //=========================================================================================
        /// <summary>
        /// 属性値（ビットフラグ）
        /// </summary>
        public byte Value;

        //=========================================================================================
        public VertexAttribute(byte initialValue = 0) : this()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
        }

        /// <summary>
        /// フラグ設定
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(byte flag, bool sw)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(VertexAttribute attr, bool sw)
        {
        }

        /// <summary>
        /// フラグ判定
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(byte flag)
        {
            return default;
        }

        /// <summary>
        /// 無効属性判定(Move/Fixed/Ignoreのどれでもない場合)
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInvalid() => IsSet(Flag_Fixed | Flag_Move) == false;
        //public bool IsInvalid() => IsSet(Flag_Fixed | Flag_Move | Flag_Ignore) == false;

        /// <summary>
        /// 固定属性判定
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFixed() => IsSet(Flag_Fixed);

        /// <summary>
        /// 移動属性判定
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMove() => IsSet(Flag_Move);

        /// <summary>
        /// 移動しないパーティクルか判定する
        /// これは固定属性以外にも掴まれたことにより動かなくなったパーティクルも含まれる
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDontMove() => !IsSet(Flag_Move);

        /// <summary>
        /// 無視属性判定
        /// </summary>
        /// <returns></returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool IsIgnore() => IsSet(Flag_Ignore);

        /// <summary>
        /// モーション制約の有効判定
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMotion() => IsSet(Flag_InvalidMotion) == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDisableCollision() => IsSet(Flag_DisableCollision);

        //=========================================================================================
        /// <summary>
        /// ２つの頂点属性を結合して新しい属性を返す
        /// 基本的に属性値が低い方が優先される（無効(0)＞固定(1)＞移動(2)）
        /// </summary>
        /// <param name="attr1"></param>
        /// <param name="attr2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexAttribute JoinAttribute(VertexAttribute attr1, VertexAttribute attr2)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(VertexAttribute other)
        {
            return default;
        }

        public override bool Equals(object obj)
        {
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VertexAttribute lhs, VertexAttribute rhs)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VertexAttribute lhs, VertexAttribute rhs)
        {
            return default;
        }
    }
}
