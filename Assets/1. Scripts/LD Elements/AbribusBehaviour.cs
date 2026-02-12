using Unity.Netcode;
using UnityEngine;

public class AbribusBehaviour : MonoBehaviour
{
    public float timeForTP;
    public NetworkObject rain;
    private NetworkObject currentRain;
    [SerializeField] private Transform posRain;
    [SerializeField]private float _currentTimer;

    public Transform playerWaiting;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            other.GetComponent<BusPassenger>().jaugeGlobale.SetActive(true);
            playerWaiting = other.transform;
            currentRain = Instantiate(rain, posRain.position, posRain.rotation);
            currentRain.Spawn();
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerWaiting.GetComponent<BusPassenger>().jaugeGlobale.SetActive(false);
            playerWaiting = null;
            if(currentRain != null)
            {
                currentRain.Despawn();
                Destroy(currentRain.gameObject);
            }
        }
    }
    
    void Update()
    {
        if (playerWaiting != null)
        {
            _currentTimer -= Time.deltaTime;
            playerWaiting.GetComponent<BusPassenger>().jaugeTime.fillAmount = _currentTimer / timeForTP;
            
            if (_currentTimer <= 0)
            {
                FPSControllerMulti fps = playerWaiting.GetComponent<FPSControllerMulti>();
                if(fps.isSitting)
                    fps.StandUp();
                playerWaiting.GetComponent<BusPassenger>().OpenMenu();
                _currentTimer = 100000;
            }
        }
        else
        {
            _currentTimer = timeForTP;
        }
    }
}
