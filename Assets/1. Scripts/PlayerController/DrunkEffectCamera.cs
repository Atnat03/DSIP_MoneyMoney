using UnityEngine;

public class DrunkEffectCamera : MonoBehaviour
{
    public float intensity = 2f;
    public float speed = 1f;

    private float drunkAmount = 0f;
    
    void Update()
    {
        if (drunkAmount > 0f)
        {
            float angle = Mathf.Sin(Time.time * speed) * intensity * drunkAmount;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void AddDrunk(float amount)
    {
        drunkAmount += amount;
        drunkAmount = Mathf.Clamp01(drunkAmount);
    }
}
