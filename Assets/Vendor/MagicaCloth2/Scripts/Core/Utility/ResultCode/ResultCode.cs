// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Diagnostics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 様々な処理の結果
    /// </summary>
    [System.Serializable]
    public struct ResultCode
    {
        [SerializeField]
        volatile Define.Result result;

        /// <summary>
        /// 警告：警告は１つのみ保持
        /// </summary>
        [SerializeField]
        volatile Define.Result warning;

        public Define.Result Result => result;

        public static ResultCode None => new ResultCode(Define.Result.None);
        public static ResultCode Empty => new ResultCode(Define.Result.Empty);
        public static ResultCode Success => new ResultCode(Define.Result.Success);
        public static ResultCode Error => new ResultCode(Define.Result.Error);

        public ResultCode(Define.Result initResult) : this()
        {
        }

        public void Clear()
        {
        }

        public void SetResult(Define.Result code)
        {
        }

        public void SetSuccess()
        {
        }

        public void SetCancel()
        {
        }

        public void SetError(Define.Result code = Define.Result.Error)
        {
        }

        public void SetWarning(Define.Result code = Define.Result.Warning)
        {
        }

        public void Merge(ResultCode src)
        {
        }

        public void SetProcess()
        {
        }

        public bool IsResult(Define.Result code)
        {
            return default;
        }

        public bool IsNone() => result == Define.Result.None;
        public bool IsSuccess() => result == Define.Result.Success;
        public bool IsFaild() => !IsSuccess();
        public bool IsCancel() => result == Define.Result.Cancel;
        public bool IsNormal() => result < Define.Result.Warning;
        public bool IsError() => result >= Define.Result.Error;
        public bool IsProcess() => result == Define.Result.Process;
        public bool IsWarning() => warning != Define.Result.None;

        public string GetResultString()
        {
            return default;
        }

        public string GetWarningString()
        {
            return default;
        }

        /// <summary>
        /// 結果コードに対する追加情報を取得する。ない場合はnullが返る。
        /// </summary>
        /// <returns></returns>
        public string GetResultInformation()
        {
            return default;
        }

        public string GetWarningInformation()
        {
            return default;
        }

        [Conditional("MC2_DEBUG")]
        public void DebugLog(bool error = true, bool warning = true, bool normal = true)
        {
        }
    }
}
