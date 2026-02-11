using System;
using UnityEngine;

public class BreakableStuff : MonoBehaviour
{


    public float ejectionForce;
    public bool isBandit;
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Truck"))
        {
            if (isBandit) GetComponent<BanditBarrage>().enabled = false;
            GetComponent<Rigidbody>().AddForce(ejectionForce * other.transform.forward + new Vector3(0,ejectionForce/3,0),  ForceMode.Impulse);
        }
    }
}