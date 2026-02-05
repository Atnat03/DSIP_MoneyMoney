using System;
using System.Collections;
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
    public bool moneySpawned;
    

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && !moneySpawned)
        {
            StartCoroutine(Spawnmoney());
        }
    }

    public IEnumerator Spawnmoney()
    {
        moneySpawned = true;

        foreach (MoneySpawn spawn in moneySpawns)
        {
            for (int i = 0; i < spawn.numberOfItem; i++)
            {
                yield return new WaitForSeconds(1f);
                Instantiate(spawn.objectSpawned, spawn.spawnPoint.position, spawn.spawnPoint.rotation);
            }
        }
    }
}
