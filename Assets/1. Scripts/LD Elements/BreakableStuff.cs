using System;
using UnityEngine;

public class BreakableStuff : MonoBehaviour
{
    public float ejectionForce;
    public bool isBandit;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Truck"))
        {
            if (isBandit) GetComponent<BanditBarrage>().enabled = false;
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.AddForce(ejectionForce * other.transform.forward + new Vector3(0,ejectionForce/3,0),  ForceMode.Impulse);
            rb.AddTorque(30,0,0);
            Destroy(gameObject,10);
        }
    }
    
    
}