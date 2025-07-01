using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments
{
    public interface ILocationNameModifier : IElement {
        int ModificationOrder { get; }
        string ModifyName(string original);
    }
}
