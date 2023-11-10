using SCHIZO.VFX;
using TriInspector;
using UnityEngine;

public class ColorTint : MonoBehaviour
{
    public Material material;
    public Color color;
    [InfoBox("Blend modes are: 0 - ADD, 1 - REPLACE, 2 - MULT, 3 - SCREEN")]
    public int blendMode;

    private MatPassID mat;

    private void Awake()
    {
        mat = new MatPassID(material, blendMode);
        _ = SchizoVFXStack.VFXStack;
    }
    private void Update()
    {
        SchizoVFXStack.RenderEffect(mat);
    }
}
