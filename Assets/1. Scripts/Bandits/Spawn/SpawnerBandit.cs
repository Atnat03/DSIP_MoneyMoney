using Unity.Netcode;
using UnityEngine;

public class SpawnerBandit : MonoBehaviour
{
    public Transform spawnPoint;
    public GameObject prefabBandit;

    public bool hasSpawned = false;

    public bool goRight;

    [Header ("Barrage")]
    public bool isBarrage;
    public Transform spawnPos, targetPos;

    public void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        Debug.Log("pleaaase");
        
        print(other.CompareTag("Truck") + " / "   + !hasSpawned + " / "   + !isBarrage);
        
        if (other.CompareTag("Truck") && !hasSpawned && !isBarrage)
        {
            print("spwna zoieujhiz uhiu");
            hasSpawned = true;
            GameObject bandit = Instantiate(prefabBandit, spawnPoint.position, spawnPoint.rotation);
            bandit.GetComponent<BanditVehicleAI>().truck = other.transform;
            bandit.GetComponent<BanditVehicleAI>().lookAtTarget.target = other.transform;
            bandit.GetComponent<BanditVehicleAI>().goRight = goRight;
            
            bandit.GetComponent<NetworkObject>().Spawn();
        }
        else if(other.CompareTag("Truck") && !hasSpawned && isBarrage)
        {
            Debug.Log("wsh");
            GameObject bandit = Instantiate(prefabBandit, spawnPos.position, spawnPos.rotation);
            bandit.GetComponent<BanditBarrage>().pointA = spawnPos;
            bandit.GetComponent<BanditBarrage>().pointB = targetPos;
            
            bandit.GetComponent<NetworkObject>().Spawn();
        }
    }
}
