using DecoratR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Decorators;
using PlaywrightAutomation.Interfaces;
using PlaywrightAutomation.Services;
using System.ComponentModel;
using System.Reflection;

namespace PlaywrightAutomation.Extensions;

public static class CommonExtensions
{

    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (string.IsNullOrWhiteSpace(name))
            return value.ToString();

        var field = type.GetField(name);
        var des = field?.GetCustomAttribute<DescriptionAttribute>();
        if (des == null)
            return value.ToString();

        return des.Description;
    }
}
