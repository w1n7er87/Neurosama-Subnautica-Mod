using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nautilus.Handlers;

namespace SCHIZO.Commands.Attributes;

[AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
internal sealed class RegisterCommandsAttribute(string category = "Uncategorized") : Attribute
{
    public string Category { get; set; } = category;
    public static void RegisterAll()
    {
        PLUGIN_ASSEMBLY.GetTypes()
            .Where(t => t.GetCustomAttribute<RegisterCommandsAttribute>() != null)
            .ForEach(ConsoleCommandsHandler.RegisterConsoleCommands);
    }
}
