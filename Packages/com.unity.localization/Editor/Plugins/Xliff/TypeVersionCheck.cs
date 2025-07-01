using System;
namespace UnityEditor.Localization.Plugins.XLIFF.Common
{
    static class TypeVersionCheck
    {
        /// <summary>
        /// Check that value can be cast to TExpected.
        /// </summary>
        /// <typeparam name="TExpected"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TExpected GetConcreteTypeThrowIfTypeVersionMismatch<TExpected>(object value) where TExpected : class
        {
            // Its possible that the wrong version of an xliff class is being used so we can provide special handling/error reporting here.
            if (value == null)
                throw new ArgumentNullException();

            var expected = value as TExpected;
            if (expected == null)
                throw new Exception($"Incorrect file type, expected {typeof(TExpected).FullName} but got {value.GetType().FullName}.");
            return expected;
        }
    }
}
