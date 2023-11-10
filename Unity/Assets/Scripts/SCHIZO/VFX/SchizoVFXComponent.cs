using TriInspector;
using UnityEngine;

namespace SCHIZO.VFX
{
    public class SchizoVFXComponent : MonoBehaviour
    {
        [Required ( Message = "Material that is used to render effect on main camera. Always applied if the object, this component is attached to, present in scene.")]
        public Material material;

        [PropertyTooltip(tooltip:"Force each instance of effect in scene to be rendered.")]
        public bool forceUniqueInstance = false;

        private MatPassID mat;

        private void Awake()
        {
            mat = new MatPassID(forceUniqueInstance ? new Material(material) : material);
            _ = SchizoVFXStack.VFXStack;
        }

        public void Update()
        {
            SchizoVFXStack.RenderEffect(mat);
        }
    }
}
