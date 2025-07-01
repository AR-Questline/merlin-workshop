using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.Services.UserReporting.Client
{
    class SerializableException : IEquatable<SerializableException>
    {
        public bool Equals(SerializableException other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return DetailedProblemIdentifier == other.DetailedProblemIdentifier && FullText == other.FullText
            && Equals(InnerException, other.InnerException) && Message == other.Message
            && ProblemIdentifier == other.ProblemIdentifier && Equals(StackTrace, other.StackTrace)
            && Type == other.Type;
        }

        internal SerializableException(Exception exception)
        {
            // Message
            Message = exception.Message;

            // Full Text
            FullText = exception.ToString();

            // Type
            Type exceptionType = exception.GetType();
            Type = exceptionType.FullName;

            // Stack Trace
            StackTrace = new List<SerializableStackFrame>();
            var stackTrace = new StackTrace(exception, true);
            foreach (var stackFrame in stackTrace.GetFrames())
            {
                StackTrace.Add(new SerializableStackFrame(stackFrame));
            }

            // Problem Identifier
            if (StackTrace.Count > 0)
            {
                SerializableStackFrame stackFrame = StackTrace[0];
                ProblemIdentifier =
                    $"{Type} at {stackFrame.DeclaringType}.{stackFrame.MethodName}";
            }
            else
            {
                ProblemIdentifier = Type;
            }

            // Detailed Problem Identifier
            if (StackTrace.Count > 1)
            {
                SerializableStackFrame stackFrame = StackTrace[1];
                DetailedProblemIdentifier = $"{Type} at {ProblemIdentifier} from {stackFrame.DeclaringType}.{stackFrame.MethodName}";
            }

            // Inner Exception
            if (exception.InnerException != null)
            {
                InnerException = new SerializableException(exception.InnerException);
            }
        }

        string DetailedProblemIdentifier { get; set; }

        string FullText { get; set; }

        SerializableException InnerException { get; set; }

        string Message { get; set; }

        string ProblemIdentifier { get; set; }

        List<SerializableStackFrame> StackTrace { get; set; }

        string Type { get; set; }
    }
}
