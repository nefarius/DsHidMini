using System.Windows.Markup;

namespace Nefarius.DsHidMini.ControlApp.Helpers;

public class EnumBindingSourceExtension : MarkupExtension
{
    private Type _enumType;

    public EnumBindingSourceExtension()
    {
    }

    public EnumBindingSourceExtension(Type enumType)
    {
        EnumType = enumType;
    }

    public Type EnumType
    {
        get => _enumType;
        set
        {
            if (value != _enumType)
            {
                if (null != value)
                {
                    Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                    if (!enumType.IsEnum)
                    {
                        throw new ArgumentException("Type must be for an Enum.");
                    }
                }

                _enumType = value;
            }
        }
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (null == _enumType)
        {
            throw new InvalidOperationException("The EnumType must be specified.");
        }

        Type actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
        Array enumValues = Enum.GetValues(actualEnumType);

        if (actualEnumType == _enumType)
        {
            return enumValues;
        }

        Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
        enumValues.CopyTo(tempArray, 1);
        return tempArray;
    }
}