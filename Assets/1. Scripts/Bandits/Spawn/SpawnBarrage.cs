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
            BanditSpawnManager.instance._timeUntilBanditBarrage.Value = BanditSpawnManager.instance.timeUntilBanditBarrage[BanditSpawnManager.instance.bankVisited-1];
            Debug.Log("1");
            if (vertical)
            {
                isSouthEast = other.transform.position.x > transform.position.x;
            }
            else
            {
                isSouthEast = other.transform.position.z > transform.position.z;
            }
            
            Debug.Log("2");
            
            if (isSouthEast)
            {
                foreach (Voiture voiture in voituresSouthWest)
                {
                    Debug.Log("3");
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
                    Debug.Log("4");
                    GameObject bandit = Instantiate(prefabBandit, voiture.start.position, voiture.start.rotation);
                    bandit.GetComponent<BanditBarrage>().pointA = voiture.start;
                    bandit.GetComponent<BanditBarrage>().pointB = voiture.end;
                    bandit.GetComponent<NetworkObject>().Spawn();
                    Debug.Log("bandit : " + bandit.gameObject.name);
                } 
            }
            
        }
    }
    
    
    
}
