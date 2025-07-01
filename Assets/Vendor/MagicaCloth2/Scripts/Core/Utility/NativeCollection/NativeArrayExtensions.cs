// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeArrayの拡張メソッド
    /// </summary>
    public static class NativeArrayExtensions
    {
        /// <summary>
        /// NativeArrayが確保されている場合のみDispose()する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        public static void MC2DisposeSafe<T>(ref this NativeArray<T> array) where T : unmanaged
        {
        }

        /// <summary>
        /// NativeArrayをリサイズする
        /// 指定サイズ未満の場合にメモリを解放して新しいサイズで確保し直す
        /// リサイズ時に内容はコピーしない
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <param name="allocator"></param>
        /// <param name="options"></param>
        public static void MC2Resize<T>(ref this NativeArray<T> array, int size, Allocator allocator = Allocator.Persistent, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
        {
        }

        /// <summary>
        /// NativeArrayをbyte[]に変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] MC2ToRawBytes<T>(ref this NativeArray<T> array) where T : unmanaged
        {
            return default;
        }

        /// <summary>
        /// byte[]からNativeArrayを作成する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static NativeArray<T> MC2FromRawBytes<T>(byte[] bytes, Allocator allocator = Allocator.Persistent) where T : unmanaged
        {
            return default;
        }
    }
}
