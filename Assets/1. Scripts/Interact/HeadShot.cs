using Unity.Netcode;
using UnityEngine;

public class HeadShot : MonoBehaviour
{
    public float MiniVelocityToTakeDamageFromThune = 8;
    public GrabPoint grabPoint;
    public HealthComponent healthComponent;
    
    public void OnCollisionEnter(Collision other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        if (!other.collider.CompareTag("Treasure"))
            return;

        GameObject objectInHand = grabPoint.GetCurrentObjectInHand();

        if (objectInHand == other.gameObject)
            return;

        Rigidbody rb = other.collider.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        print(rb.linearVelocity.magnitude);

        if (rb.linearVelocity.magnitude >= MiniVelocityToTakeDamageFromThune)
        {
            healthComponent.TryTakeDamage(1000);
        }
    }

}
