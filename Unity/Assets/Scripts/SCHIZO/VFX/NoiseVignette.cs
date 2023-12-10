using SCHIZO.VFX;
using TriInspector;
using UnityEditor;
using UnityEngine;

public class NoiseVignette : VFXComponent
{
    public Texture2D noiseTexture;

    [ValidateInput(nameof(ValidateNormalMap))]
    public Texture2D displacementNormal;

    [Range(0f, 1f)]
    public float displacementStrength = 0.5f;
    [Range(0f, 1f)]
    public float strength = 1f;

    private TriValidationResult ValidateNormalMap()
    { 
        TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(displacementNormal));
        if(importer.textureType != TextureImporterType.NormalMap) { return TriValidationResult.Error("Normal map is required"); }
        return TriValidationResult.Valid;
    }
    public override void SetProperties()
    {
        base.SetProperties();
        matPassID.SetTexture("_Image", noiseTexture);
        matPassID.SetTexture("_Displacement", displacementNormal);
        matPassID.SetFloat("_DispStrength", displacementStrength);
        matPassID.SetFloat("_Strength", strength);
    }
}
