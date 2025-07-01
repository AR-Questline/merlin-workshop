using System;
using System.Diagnostics;
using System.Reflection;

namespace Unity.Services.UserReporting.Client
{
    class SerializableStackFrame
    {
        internal SerializableStackFrame(StackFrame stackFrame)
        {
            MethodBase method = stackFrame.GetMethod();
            Type declaringType = method.DeclaringType;
            DeclaringType = declaringType != null ? declaringType.FullName : null;
            Method = method.ToString();
            MethodName = method.Name;
            FileName = stackFrame.GetFileName();
            FileLine = stackFrame.GetFileLineNumber();
            FileColumn = stackFrame.GetFileColumnNumber();
        }

        internal string DeclaringType { get; set; }

        int FileColumn { get; set; }

        int FileLine { get; set; }

        string FileName { get; set; }

        string Method { get; set; }

        internal string MethodName { get; set; }
    }
}
