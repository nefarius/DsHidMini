using System.ComponentModel;
using System.Resources;

namespace Nefarius.DsHidMini.ControlApp.Helpers;

public class LocalizedDescriptionAttribute : DescriptionAttribute
{
    private readonly string _resourceKey;
    private readonly ResourceManager _resourceManager;

    public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
    {
        _resourceManager = new ResourceManager(resourceType);
        _resourceKey = resourceKey;
    }

    public override string Description
    {
        get
        {
            string description = _resourceManager.GetString(_resourceKey);
            return string.IsNullOrWhiteSpace(description) ? string.Format("[[{0}]]", _resourceKey) : description;
        }
    }
}