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

    public Voiture[] voituresSouthWest, voituresNorthEast;

    public GameObject prefabBandit;
    public bool isSouthEast;

    public bool vertical;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && BanditSpawnManager.instance.hasToSpawnBarrage)
        {
            BanditSpawnManager.instance.hasToSpawnBarrage = false;
            BanditSpawnManager.instance._timeUntilBanditBarrage = BanditSpawnManager.instance.timeUntilBanditBarrage;

            if (vertical)
            {
                isSouthEast = other.transform.position.x > transform.position.x;
            }
            else
            {
                isSouthEast = other.transform.position.z < transform.position.z;
            }
            
            if (isSouthEast)
            {
                foreach (Voiture voiture in voituresSouthWest)
                {
                    GameObject bandit = Instantiate(prefabBandit, voiture.start.position, voiture.start.rotation);
                    bandit.GetComponent<BanditBarrage>().pointA = voiture.start;
                    bandit.GetComponent<BanditBarrage>().pointB = voiture.end;
                    bandit.GetComponent<NetworkObject>().Spawn();
                } 
            }
            else
            {
                foreach (Voiture voiture in voituresNorthEast)
                {
                    GameObject bandit = Instantiate(prefabBandit, voiture.start.position, voiture.start.rotation);
                    bandit.GetComponent<BanditBarrage>().pointA = voiture.start;
                    bandit.GetComponent<BanditBarrage>().pointB = voiture.end;
                    bandit.GetComponent<NetworkObject>().Spawn();
                } 
            }
            
        }
    }
    
    
    
}
