using UnityEngine;

public class BanditSpawnManager : MonoBehaviour
{
    public static BanditSpawnManager instance;
    
    public float timeUntilBanditFollow;
    public float timeUntilBanditBarrage;
    
    
    [SerializeField]private float _timeUntilBanditFollow;
    [SerializeField]private float _timeUntilBanditBarrage;

    public Vector3 detectionRadius;
    public LayerMask spawnPointLayer;

    public Transform boxCenter;
    
    public GameObject banditFollowPrefab;


    void Awake()
    {
        instance = this;
        _timeUntilBanditFollow = timeUntilBanditFollow;
        _timeUntilBanditBarrage = timeUntilBanditBarrage;
    }

    public void Update()
    {
        //_timeUntilBanditBarrage -= Time.deltaTime;
        _timeUntilBanditFollow -= Time.deltaTime;

        if (_timeUntilBanditFollow <= 0) { _timeUntilBanditFollow = timeUntilBanditFollow; SpawnBanditFollow(); }
        
        if (_timeUntilBanditBarrage <= 0) { _timeUntilBanditBarrage = timeUntilBanditBarrage; SpawnBanditBarrage(); }
    }


    public void SpawnBanditFollow()
    {
        Collider[] hits = Physics.OverlapBox(boxCenter.position, detectionRadius,  Quaternion.identity, spawnPointLayer);
        
        Transform closest = null;
        float minDist = Mathf.Infinity;
        
        Debug.Log("on appelle la fonction");
        foreach (var hit in hits)
        {
            Debug.Log("au moins un ? ");
            float dist = (hit.transform.position - transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        if (closest != null)
        {
            Instantiate(banditFollowPrefab, closest.position, Quaternion.identity);
        }
    }

    public void SpawnBanditBarrage()
    {
        
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCenter.position, detectionRadius);
    }
    
    
}
