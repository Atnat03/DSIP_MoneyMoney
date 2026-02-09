using System;
using Unity.Netcode;
using UnityEngine;

public class SpawnBarrage : MonoBehaviour
{
    [Serializable]
    public struct Voiture
    {
        public Transform start;
        public Transform end;
    }

    public Voiture[] voitures;

    public GameObject prefabBandit;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && BanditSpawnManager.instance.hasToSpawnBarrage)
        {
            BanditSpawnManager.instance.hasToSpawnBarrage = false;
            BanditSpawnManager.instance._timeUntilBanditBarrage = BanditSpawnManager.instance.timeUntilBanditBarrage;
            
            foreach (Voiture voiture in voitures)
            {
                GameObject bandit = Instantiate(prefabBandit, voiture.start.position, voiture.start.rotation);
                bandit.GetComponent<BanditBarrage>().pointA = voiture.start;
                bandit.GetComponent<BanditBarrage>().pointB = voiture.end;
                bandit.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
    
    
    
}
