using System;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace UnityEngine.Localization.Samples
{
    /// <summary>
    /// This is an example of a Global Variable that can return the current time.
    /// </summary>
    [DisplayName("Current Date Time")]
    public class CurrentTime : IVariable
    {
        public object GetSourceValue(ISelectorInfo _) => DateTime.Now;
    }
}
