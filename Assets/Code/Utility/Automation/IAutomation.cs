using Cysharp.Threading.Tasks;

namespace Awaken.Utility.Automation {
    public interface IAutomation {
        public const string Prefix = "auto";
        public const string Separator0 = "::";
        public const string Separator1 = ":";
        public const string Separator2 = ".";
        
        UniTask Run(string[] parameters);
    }
}