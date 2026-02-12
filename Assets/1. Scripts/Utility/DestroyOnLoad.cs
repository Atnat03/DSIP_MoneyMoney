using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DestroyOnLoad : MonoBehaviour
{
    [SerializeField] float timerBeforeDestroy;

    private void OnEnable()
    {
        StartCoroutine(DestroyAfter());
    }

    IEnumerator DestroyAfter()
    {
        yield return new WaitForSeconds(timerBeforeDestroy);
        
        NetworkObject n = GetComponent<NetworkObject>();
        
        if (n != null) n.Despawn();
        
        Destroy(gameObject);
    }
}
