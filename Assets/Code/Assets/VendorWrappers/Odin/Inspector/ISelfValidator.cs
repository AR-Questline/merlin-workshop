namespace Sirenix.OdinInspector
{
    public interface ISelfValidator
    {
        void Validate(SelfValidationResult result);
    }

    public class SelfValidationResult
    {
        public void AddError(string error) { }
    }
}