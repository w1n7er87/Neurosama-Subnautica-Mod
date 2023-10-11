using Nautilus.Utility;

namespace SCHIZO.Helpers;

public static partial class MaterialHelpers
{
    public static bool IsReady => MaterialUtils.IsReady;

    public static Material GhostMaterial => MaterialUtils.GhostMaterial;
}
