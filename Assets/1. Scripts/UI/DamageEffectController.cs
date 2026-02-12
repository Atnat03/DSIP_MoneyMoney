using UnityEngine;

public class DamageEffectController : MonoBehaviour
{
    #region Properties

    public Material ControlledMaterial => _fullscreenEffect;

    #endregion


    #region Fields

    [SerializeField] private Material _fullscreenEffect;


    #endregion


    #region Methods

    public void SetIntensity(float intensity) => SetFloat("_GlobalIntensity", intensity, true);
    public void SetColor(Color color) => SetColor("_Color", color);


    protected void SetFloat(string reference, float value, bool clamp01=false)
    {
        if (_fullscreenEffect == null)
        {
            Debug.LogWarning("[DamageEffectController] : Cannot apply the effect because no material is referenced");
            return;
        }
        
        if (!_fullscreenEffect.HasFloat(reference))
        {
            Debug.LogWarning("[DamageEffectController] : Cannot modify the value of '" + reference + "' because no such property exists on the target material's shader");
            return;
        }

        if (clamp01)
            value = Mathf.Clamp01(value);

        _fullscreenEffect.SetFloat(reference, value);
    }
    protected void SetColor(string reference, Color value)
    {
        if (_fullscreenEffect == null)
        {
            Debug.LogWarning("[DamageEffectController] : Cannot apply the effect because no material is referenced");
            return;
        }
        
        if (!_fullscreenEffect.HasColor(reference))
        {
            Debug.LogWarning("[DamageEffectController] : Cannot modify the value of '" + reference + "' because no such property exists on the target material's shader");
            return;
        }

        _fullscreenEffect.SetColor(reference, value);
    }

    #endregion
}
