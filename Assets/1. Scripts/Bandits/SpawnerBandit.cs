using UnityEngine;

public class SpawnerBandit : MonoBehaviour
{
    public Transform spawnPoint;
    public GameObject prefabBandit;

    public bool hasSpawned;

    public BanditVehicleAI.RelativePosition relativePos;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && !hasSpawned)
        {
            hasSpawned = true;
            GameObject bandit = Instantiate(prefabBandit, spawnPoint.position, spawnPoint.rotation);
            bandit.GetComponent<BanditVehicleAI>().truck = other.transform;
            bandit.GetComponent<BanditVehicleAI>().lookAtTarget.target = other.transform;
            bandit.GetComponent<BanditVehicleAI>().position = relativePos;
        }
    }
}
