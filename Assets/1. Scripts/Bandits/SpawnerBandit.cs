using UnityEngine;

public class SpawnerBandit : MonoBehaviour
{
    public Transform spawnPoint;
    public GameObject prefabBandit;

    public bool hasSpawned;

    public BanditVehicleAI.RelativePosition relativePos;

    [Header ("Barrage")]
    public bool isBarrage;
    public Transform spawnPos, targetPos;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && !hasSpawned && !isBarrage)
        {
            hasSpawned = true;
            GameObject bandit = Instantiate(prefabBandit, spawnPoint.position, spawnPoint.rotation);
            bandit.GetComponent<BanditVehicleAI>().truck = other.transform;
            bandit.GetComponent<BanditVehicleAI>().lookAtTarget.target = other.transform;
            bandit.GetComponent<BanditVehicleAI>().position = relativePos;
        }
        else if(other.CompareTag("Truck") && !hasSpawned && isBarrage)
        {
            GameObject bandit = Instantiate(prefabBandit, spawnPos.position, spawnPoint.rotation);
            bandit.GetComponent<BanditBarrage>().pointA = spawnPos;
            bandit.GetComponent<BanditBarrage>().pointB = targetPos;
        }
    }
}
