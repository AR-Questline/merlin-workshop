using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Awaken.TG.Utility.Reflections;

namespace Awaken.TG.Debugging.ModelsDebugs {
    public class MembersListItem {
        public MemberInfo MemberInfo { get; }
        public Type Type { get; }
        public string Name { get; private set; }
        public bool Writeable { get; }
        public bool CanObtainValue { get; }

        public MembersListItem(MemberInfo memberInfo) {
            MemberInfo = memberInfo;
            Type = MemberInfo.PointType();
            ExtractName(MemberInfo.Name);
            CanObtainValue = MemberInfo.CanObtainValue();
            Writeable = CanObtainValue && MemberInfo.IsWriteable();
        }

        public object Value(object relatedObject) {
            return MemberInfo.MemberValue(relatedObject);
        }
        
        public void SetValue(object relatedObject, object value) {
            try {
                MemberInfo.SetMemberValue(relatedObject, value);
            }catch{}
        }
        
        static readonly Regex BackingName = new Regex(@"<(.+)>", RegexOptions.Compiled);
        static readonly Regex GetterName = new Regex(@"get_(.+)", RegexOptions.Compiled);
        static readonly Regex PrivateName = new Regex(@"_(.+)", RegexOptions.Compiled);
        
        void ExtractName(string memberInfoName) {
            Match match = GetterName.Match(memberInfoName);
            if (match.Success) {
                Name = match.Groups[1].Value;
                return;
            }
            
            match = BackingName.Match(memberInfoName);
            if (match.Success) {
                Name = match.Groups[1].Value;
                return;
            }

            match = PrivateName.Match(memberInfoName);
            if (match.Success) {
                Name = UppercaseFirst(match.Groups[1].Value);
            } else {
                Name = UppercaseFirst(memberInfoName);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string UppercaseFirst(string text) {
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}