using UnityEngine;
using Unity.Netcode;
using UnityEngine;

public class BanditSpawnManager : MonoBehaviour
{
    public static BanditSpawnManager instance;
    
    
    public NetworkVariable<float> _timeUntilBanditFollow = new(0);
    public NetworkVariable<float> _timeUntilBanditBarrage = new(0);
    
    
    public float timeUntilBanditFollow;
    public float timeUntilBanditBarrage;

    public Vector3 detectionRadius;
    public LayerMask spawnPointLayer;

    public Transform boxCenter;
    
    public GameObject banditFollowPrefab;
    public GameObject truck;
    
    public 


    void Awake()
    {
        instance = this;
        _timeUntilBanditFollow.Value = timeUntilBanditFollow;
        _timeUntilBanditBarrage.Value = timeUntilBanditBarrage;
        
        _timeUntilBanditFollow.OnValueChanged += OnTimeUntilBFChaned;
        _timeUntilBanditBarrage.OnValueChanged += OnTimeUntilBBChaned;
    }

    private void OnTimeUntilBFChaned(float previousValue, float newValue)
    {
        if (_timeUntilBanditFollow.Value <= 0) { _timeUntilBanditFollow.Value = timeUntilBanditFollow; SpawnBanditFollowServerRpc(); }
    }
    private void OnTimeUntilBBChaned(float previousValue, float newValue)
    {
        if (_timeUntilBanditBarrage.Value <= 0) { SpawnBanditBarrageServerRpc(); }
    }

    public void Update()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        if (_timeUntilBanditBarrage.Value > 0)
        {
            _timeUntilBanditBarrage.Value-= Time.deltaTime;
            
        }

        if (_timeUntilBanditFollow.Value > 0)
        {
            _timeUntilBanditFollow.Value -= Time.deltaTime;
            
        }
    }


    [ServerRpc]
    public void SpawnBanditFollowServerRpc()
    {
        Collider[] hits = Physics.OverlapBox(boxCenter.position, detectionRadius,  Quaternion.identity, spawnPointLayer);
        
        Transform closest = null;
        float minDist = 0;
        
        foreach (var hit in hits)
        {
            float dist = (hit.transform.position - transform.position).sqrMagnitude;
            if (dist > minDist)
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
            
            bandit.GetComponent<NetworkObject>().Spawn();
        }
    }

    public bool hasToSpawnBarrage;

    [ServerRpc]
    public void SpawnBanditBarrageServerRpc()
    {
        hasToSpawnBarrage = true;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCenter.position, detectionRadius);
    }
    
    
}
