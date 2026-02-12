using System;
using System.Collections;
using UnityEngine;

public class DrunkEffectCamera : MonoBehaviour
{
    public float intensity = 2f;
    public float speed = 2f;
    public float duration = 0.5f;

    private Coroutine drunkCoroutine;
    private Quaternion initialRotation;
    
    DamageEffectController damageEffectController;

    private void Start()
    {
        damageEffectController = GetComponent<DamageEffectController>();
    }

    public void AddDrunk()
    {
        if (drunkCoroutine != null)
            StopCoroutine(drunkCoroutine);

        drunkCoroutine = StartCoroutine(DrunkRoutine());
    }

    private IEnumerator DrunkRoutine()
    {
        damageEffectController.SetDizziness(0.5f);
        damageEffectController.SetColor(Color.darkOliveGreen);
        
        float timer = 0f;
        float transition = 0.5f;
        
        while (timer < transition)
        {
            damageEffectController.SetIntensity(timer / transition);
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitForSeconds(duration);
        
        while (timer > 0)
        {
            damageEffectController.SetIntensity(timer / transition);
            
            timer -= Time.deltaTime;
            yield return null;
        }

        drunkCoroutine = null;
    }
}
