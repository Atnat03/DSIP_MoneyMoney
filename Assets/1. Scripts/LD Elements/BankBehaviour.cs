using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BankBehaviour : MonoBehaviour
{
    [Serializable]
    public struct MoneySpawn
    {
        public GameObject objectSpawned;
        public Transform spawnPoint;
        public int numberOfItem;
    }

    public MoneySpawn[] moneySpawns;
    public NetworkVariable<bool> visited = new NetworkVariable<bool>(false);
    public float delay;
    public string colorBank;

    public Transform spawnPlayer;
    

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && !visited.Value)
        {
            StartCoroutine(Spawnmoney());
        }
    }

    public IEnumerator Spawnmoney()
    {
        if (!NetworkManager.Singleton.IsServer)
            yield break;
            
        visited.Value = true;

        foreach (MoneySpawn spawn in moneySpawns)
        {
            for (int i = 0; i < spawn.numberOfItem; i++)
            {
                yield return new WaitForSeconds(delay);
                GameObject g = Instantiate(spawn.objectSpawned, spawn.spawnPoint.position, spawn.spawnPoint.rotation);
                NetworkObject network = g.GetComponent<NetworkObject>();
                if(network != null)
                    network.Spawn();
            }
        }
    }
}
