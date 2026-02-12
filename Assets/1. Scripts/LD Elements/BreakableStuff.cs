using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BreakableStuff : MonoBehaviour
{
    public float ejectionForce;
    public bool isBandit;
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        if (other.gameObject.layer == LayerMask.NameToLayer("Truck"))
        {
            if (isBandit) GetComponent<BanditBarrage>().enabled = false;
            
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            
            NetworkObject n = gameObject.AddComponent<NetworkObject>();
            n.Spawn();
            gameObject.AddComponent<NetworkTransform>();
            
            rb.AddForce(ejectionForce * other.transform.forward + new Vector3(0,ejectionForce/3,0),  ForceMode.Impulse);
            rb.AddTorque(30,0,0);
            StartCoroutine(DespawnAfter(n, 10f));
        }
    }
    private IEnumerator DespawnAfter(NetworkObject netObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn();
    }
    
    
}