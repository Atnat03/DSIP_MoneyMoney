using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public interface IVehicule
{
    public void Die();
}

public class Helicopter : MonoBehaviour, IVehicule
{
    [SerializeField] private Transform rotor;
    [SerializeField] private float rotorRotateSpeed = 5f;
    [SerializeField] private GameObject helicopter;
    [SerializeField] public bool isDead = false;
    
    private void Update()
    { 
        rotor.transform.Rotate(Vector3.up, rotorRotateSpeed * Time.deltaTime);
    }

    public void Die()
    {
        isDead =  true;
        helicopter.AddComponent<Rigidbody>();

        StartCoroutine(DestroyAfter());
    }

    IEnumerator DestroyAfter()
    {
        yield return new WaitUntil(() => helicopter.transform.position.y < 0f);
        
        if (TryGetComponent(out BanditVehicleAI ai))
        {
            GetComponent<NetworkObject>().Despawn();
            Destroy(ai.gameObject);
        }
    }
}
