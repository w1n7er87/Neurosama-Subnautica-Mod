﻿
namespace SCHIZO.Unity.Materials;

[CreateAssetMenu(menuName = "SCHIZO/Materials/Material Remap Config")]
public sealed class MaterialRemapConfig : ScriptableObject
{
    public Material[] original;
    [Expandable]
    public MaterialRemapOverride[] remappings;
}
