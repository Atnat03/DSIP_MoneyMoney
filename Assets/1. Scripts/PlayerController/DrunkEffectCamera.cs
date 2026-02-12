using System.Collections;
using UnityEngine;

public class DrunkEffectCamera : MonoBehaviour
{
    public float intensity = 2f;
    public float speed = 2f;
    public float duration = 0.5f;

    private Coroutine drunkCoroutine;
    private Quaternion initialRotation;

    public FPSControllerMulti fps;

    public void AddDrunk(float amount)
    {
        if (drunkCoroutine != null)
            StopCoroutine(drunkCoroutine);

        drunkCoroutine = StartCoroutine(DrunkRoutine());
    }

    private IEnumerator DrunkRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            float angle = Mathf.Sin(Time.time * speed) * intensity;

            fps.SetDrunkOffset(angle);

            timer += Time.deltaTime;
            yield return null;
        }

        fps.SetDrunkOffset(0f);
        drunkCoroutine = null;
    }
}
