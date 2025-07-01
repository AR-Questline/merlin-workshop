using System;

namespace Awaken.TG.MVC.Attributes
{
    /// <summary>
    /// Attribute for use with View classes - allows the class to specify which
    /// prefab should be instantiated when the view is spawned. The prefab has
    /// to already contain a View component of the same class to be functional.
    /// If no attribute is specified, it is assumed the name of the prefab is
    /// the same as the name of the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UsesPrefab : Attribute {
        public string prefabName;

        public UsesPrefab(string prefabName) {
            this.prefabName = prefabName;
        }
    }
}
