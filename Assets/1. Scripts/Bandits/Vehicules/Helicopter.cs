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
    [SerializeField] public GameObject explosionParticle;
    public GameObject tourelle;


    public void Start()
    {
        tourelle.GetComponent<NetworkObject>().Spawn();
    }
    private void Update()
    {
        if (!isDead)
        {
            rotor.transform.Rotate(Vector3.up, rotorRotateSpeed * Time.deltaTime);
        }
    }

    public void Die()
    {
        isDead =  true;
        Rigidbody rb = helicopter.AddComponent<Rigidbody>();
        rb.AddTorque(300,0,0);

        StartCoroutine(DestroyAfter());
    }

    IEnumerator DestroyAfter()
    {
        print(helicopter.transform.position.y);
        yield return new WaitUntil(() => helicopter.transform.position.y < 10f);
        
        if (TryGetComponent(out HelicopterVehicleAI ai))
        {
            NetworkObject explosionParticleIntance = Instantiate(explosionParticle, transform.position, transform.rotation).GetComponent<NetworkObject>();
            explosionParticleIntance.Spawn();
            
            GetComponent<NetworkObject>().Despawn();
            BanditSpawnManager.instance.canSpawnHelico = true;
            if (tourelle.TryGetComponent<NetworkObject>(out var netObj2))
            {
                netObj2.Despawn();
            }
            Destroy(ai.gameObject);
        }
    }
}
