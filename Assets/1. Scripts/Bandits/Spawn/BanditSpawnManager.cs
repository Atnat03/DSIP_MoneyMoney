using UnityEngine;

public class BanditSpawnManager : MonoBehaviour
{
    public static BanditSpawnManager instance;
    
    public float timeUntilBanditFollow;
    public float timeUntilBanditBarrage;
    
    
    public float _timeUntilBanditFollow;
    public float _timeUntilBanditBarrage;

    public Vector3 detectionRadius;
    public LayerMask spawnPointLayer;

    public Transform boxCenter;
    
    public GameObject banditFollowPrefab;
    public GameObject truck;
    
    public 


    void Awake()
    {
        instance = this;
        _timeUntilBanditFollow = timeUntilBanditFollow;
        _timeUntilBanditBarrage = timeUntilBanditBarrage;
    }

    public void Update()
    {
        if (_timeUntilBanditBarrage > 0)
        {
            _timeUntilBanditBarrage -= Time.deltaTime;
            if (_timeUntilBanditFollow <= 0) { _timeUntilBanditFollow = timeUntilBanditFollow; SpawnBanditFollow(); }
        }

        if (_timeUntilBanditFollow > 0)
        {
            _timeUntilBanditFollow -= Time.deltaTime;
            if (_timeUntilBanditBarrage <= 0) { SpawnBanditBarrage(); }
        }
        
        
    }


    public void SpawnBanditFollow()
    {
        Collider[] hits = Physics.OverlapBox(boxCenter.position, detectionRadius,  Quaternion.identity, spawnPointLayer);
        
        Transform closest = null;
        float minDist = Mathf.Infinity;
        
        foreach (var hit in hits)
        {
            float dist = (hit.transform.position - transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        if (closest != null)
        {
            GameObject bandit = Instantiate(banditFollowPrefab, closest.position, Quaternion.identity);
            
            bandit.GetComponent<BanditVehicleAI>().truck = truck.transform;
            bandit.GetComponent<BanditVehicleAI>().lookAtTarget.target = truck.transform;
            
            int rdm = Random.Range(0, 2);
            bool goRight = (rdm == 0);      
            bandit.GetComponent<BanditVehicleAI>().goRight = goRight;
        }
    }

    public bool hasToSpawnBarrage;

    public void SpawnBanditBarrage()
    {
        hasToSpawnBarrage = true;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCenter.position, detectionRadius);
    }
    
    
}
