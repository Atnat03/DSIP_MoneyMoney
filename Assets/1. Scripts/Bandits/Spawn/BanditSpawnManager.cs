using UnityEngine;
using Unity.Netcode;
using UnityEngine;

public class BanditSpawnManager : MonoBehaviour
{
    public static BanditSpawnManager instance;
    
    
    public NetworkVariable<float> _timeUntilBanditFollow = new(0);
    public NetworkVariable<float> _timeUntilBanditBarrage = new(0);
    public NetworkVariable<float> _timeUntilHelicopter = new(0);
    
    
    public float[] timeUntilBanditFollow;
    public float[] timeUntilBanditBarrage;
    public float[] timeUntilHelicopter;

    public Vector3 detectionRadius;
    public LayerMask spawnPointLayer;

    public Transform boxCenter;
    
    public GameObject banditFollowPrefab;
    public GameObject helicopterPrefab;
    public GameObject truck;

    public int bankVisited;
    public bool canSpawnHelico = true;
    


    void Awake()
    {
        instance = this;
        _timeUntilBanditFollow.Value = timeUntilBanditFollow[bankVisited-1]/3;
        _timeUntilBanditBarrage.Value = timeUntilBanditBarrage[bankVisited-1];
        _timeUntilHelicopter.Value = timeUntilHelicopter[bankVisited-1]/2;
        
        _timeUntilBanditFollow.OnValueChanged += OnTimeUntilBFChaned;
        _timeUntilBanditBarrage.OnValueChanged += OnTimeUntilBBChaned;
        _timeUntilHelicopter.OnValueChanged += OnTimeUntilBHChaned;
    }

    private void OnTimeUntilBFChaned(float previousValue, float newValue)
    {
        if (_timeUntilBanditFollow.Value <= 0) { _timeUntilBanditFollow.Value = timeUntilBanditFollow[bankVisited-1]; SpawnBanditFollowServerRpc(); }
    }
    
    private void OnTimeUntilBHChaned(float previousValue, float newValue)
    {
        if (_timeUntilHelicopter.Value <= 0 && canSpawnHelico) { _timeUntilHelicopter.Value = timeUntilHelicopter[bankVisited-1]; SpawnBanditHelicoServerRpc(); }
    }
    private void OnTimeUntilBBChaned(float previousValue, float newValue)
    {
        if (_timeUntilBanditBarrage.Value <= 0) { SpawnBanditBarrageServerRpc(); }
    }

    public void Update()
    {
        bankVisited = BankManager.instance.BankVisited();
        
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (truck.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude < 3)
            return;
        
        if (_timeUntilBanditBarrage.Value > 0) { _timeUntilBanditBarrage.Value-= Time.deltaTime; }
        if (_timeUntilBanditFollow.Value > 0) { _timeUntilBanditFollow.Value -= Time.deltaTime; }
        if (_timeUntilHelicopter.Value > 0) { _timeUntilHelicopter.Value -= Time.deltaTime; }
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
    
    [ServerRpc]
    public void SpawnBanditHelicoServerRpc()
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
           
            GameObject bandit = Instantiate(helicopterPrefab, new Vector3(closest.position.x, 30, closest.position.z), Quaternion.identity);

            bandit.GetComponent<HelicopterVehicleAI>().truck = truck.transform;
            
            int rdm = Random.Range(0, 2);
            bool goRight = (rdm == 0);

            if (!goRight)
            {
                bandit.GetComponent<HelicopterVehicleAI>().sideOffset *= -1;
            }
            bandit.GetComponent<NetworkObject>().Spawn();
            canSpawnHelico = false;
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
