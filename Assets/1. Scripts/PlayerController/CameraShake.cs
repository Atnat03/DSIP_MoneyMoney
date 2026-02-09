using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public void TriggerShake(float duration, float magnitude)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPosition = transform.parent.localPosition;

        float elasped= 0;

        while (elasped < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            transform.parent.localPosition = new Vector3(x, y, originalPosition.z);
            
            elasped += Time.deltaTime;
            
            yield return null;
        }
        
        transform.parent.localPosition = originalPosition;
    }
}
