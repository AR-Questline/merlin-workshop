using System;

namespace QFSW.QC
{
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string aliasOverride, string description) { }
        public CommandAttribute(string aliasOverride, string description, bool allowWhiteSpaces) { }
    }
}