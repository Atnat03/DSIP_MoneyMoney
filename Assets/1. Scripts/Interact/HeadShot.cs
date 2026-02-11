using UnityEngine;

public class HeadShot : MonoBehaviour
{
    public float MiniVelocityToTakeDamageFromThune = 8;
    public GrabPoint grabPoint;
    public HealthComponent healthComponent;
    
    public void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Treasure") && (grabPoint.GetCurrentObjectInHand() == null && grabPoint.GetCurrentObjectInHand() != other.gameObject))
        {
            Rigidbody rb = other.collider.GetComponent<Rigidbody>();
            print(rb.linearVelocity.magnitude);
            if (rb.linearVelocity.magnitude >= MiniVelocityToTakeDamageFromThune)
            {
                healthComponent.TryTakeDamage(1000);
            }
        }
    }
}
