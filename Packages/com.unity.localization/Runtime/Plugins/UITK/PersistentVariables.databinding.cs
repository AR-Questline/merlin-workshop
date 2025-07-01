#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using UnityEngine.UIElements;

namespace UnityEngine.Localization.SmartFormat.PersistentVariables
{
    [UxmlObject]
    public partial class Variable<T>
    {
        [UxmlAttribute("value")]
        public T ValueUXML
        {
            get => Value;
            set => Value = value;
        }
    }

    [UxmlObject]
    public partial class BoolVariable : Variable<bool> {}

    [UxmlObject]
    public partial class SByteVariable : Variable<sbyte> {}

    [UxmlObject]
    public partial class ByteVariable : Variable<byte> {}

    [UxmlObject]
    public partial class ShortVariable : Variable<short> {}

    [UxmlObject]
    public partial class UShortVariable : Variable<ushort> {}

    [UxmlObject]
    public partial class IntVariable : Variable<int> {}

    [UxmlObject]
    public partial class UIntVariable : Variable<uint> {}

    [UxmlObject]
    public partial class LongVariable : Variable<long> {}

    [UxmlObject]
    public partial class ULongVariable : Variable<ulong> {}

    [UxmlObject]
    public partial class StringVariable : Variable<string> {}

    [UxmlObject]
    public partial class FloatVariable : Variable<float> {}

    [UxmlObject]
    public partial class DoubleVariable : Variable<double> {}

    [UxmlObject]
    public partial class ObjectVariable : Variable<Object> {}
}

#endif
