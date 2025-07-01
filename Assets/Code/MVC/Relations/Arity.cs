namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Designates an arity for one side of a relation pair. Combining two such enums allows
    /// for 1:1, 1:N and M:N relations.
    /// </summary>
    public enum Arity 
    {
        One, Many
    }
}