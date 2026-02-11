using UnityEngine;

public class HeadShot : MonoBehaviour
{
    public float MiniVelocityToTakeDamageFromThune = 8;
    public GrabPoint grabPoint;
    public HealthComponent healthComponent;
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Treasure") && (grabPoint.GetCurrentObjectInHand() == null && grabPoint.GetCurrentObjectInHand() != other.gameObject))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            print(rb.linearVelocity.magnitude);
            if (rb.linearVelocity.magnitude >= MiniVelocityToTakeDamageFromThune)
            {
                healthComponent.TryTakeDamage(1000);
            }
        }
    }
}
