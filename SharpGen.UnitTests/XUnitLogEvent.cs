using System;
using SharpGen.Logging;

namespace SharpGen.UnitTests
{
    public sealed class XUnitLogEvent : IEquatable<XUnitLogEvent>
    {
        public string Code { get; }
        public string FullMessage { get; }
        public Exception Exception { get; }
        public LogLevel Level { get; }
        
        public XUnitLogEvent(string code, string fullMessage, Exception exception, LogLevel level)
        {
            Code = code;
            FullMessage = fullMessage;
            Exception = exception;
            Level = level;
        }

        public bool Equals(XUnitLogEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Code == other.Code && FullMessage == other.FullMessage && Equals(Exception, other.Exception) && Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is XUnitLogEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Code, FullMessage, Exception, (int) Level);
        }

        public static bool operator ==(XUnitLogEvent left, XUnitLogEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(XUnitLogEvent left, XUnitLogEvent right)
        {
            return !Equals(left, right);
        }
    }
}