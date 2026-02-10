using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public bool shaking = false;
    
    public void TriggerShake(float duration, float magnitude)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    IEnumerator Shake(float duration, float magnitude)
    {
        shaking = true;
        
        Vector3 originalPosition = transform.localPosition;

        float elasped= 0;

        while (elasped < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            transform.localPosition = new Vector3(x, y, originalPosition.z);
            
            elasped += Time.deltaTime;
            
            yield return null;
        }
        
        transform.localPosition = originalPosition;

        shaking = false;
    }
}
