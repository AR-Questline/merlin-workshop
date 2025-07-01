using System;
using System.Text;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Saving {
    public static class JsonTypeName {
        static readonly OnDemandCache<Type, string> Cache = new(static type => RemoveAssemblyDetails(type.AssemblyQualifiedName));

        public static string Get(Type type) => Cache[type];
        
        // From Newtonsoft.Json.Utilities.ReflectionUtils
        static string RemoveAssemblyDetails(string fullyQualifiedTypeName) {
            var builder = new StringBuilder();

            // loop through the type name and filter out qualified assembly details from nested type names
            bool writingAssemblyName = false;
            bool skippingAssemblyDetails = false;
            bool followBrackets = false;
            for (int i = 0; i < fullyQualifiedTypeName.Length; i++) {
                char current = fullyQualifiedTypeName[i];
                switch (current) {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        followBrackets = true;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        followBrackets = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (followBrackets) {
                            builder.Append(current);
                        } else if (!writingAssemblyName) {
                            writingAssemblyName = true;
                            builder.Append(current);
                        } else {
                            skippingAssemblyDetails = true;
                        }
                        break;
                    default:
                        followBrackets = false;
                        if (!skippingAssemblyDetails) {
                            builder.Append(current);
                        }
                        break;
                }
            }

            return builder.ToString();
        }
    }
}