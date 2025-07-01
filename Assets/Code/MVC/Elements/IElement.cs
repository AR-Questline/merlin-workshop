namespace Awaken.TG.MVC.Elements
{
    public interface IElement : IModel {
        IModel GenericParentModel { get; }
        bool IsFullyInitializedWithParents();
    }

    public interface IElement<out TParent> : IElement where TParent : IModel
    {
        /// <summary>
        /// The parent model of this element.
        /// </summary>
        TParent ParentModel { get; }
    }
}
