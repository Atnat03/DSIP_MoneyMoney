using UnityEngine;

public class AbribusBehaviour : MonoBehaviour
{
    public float timeForTP;
    [SerializeField]private float _currentTimer;

    public Transform playerWaiting;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            other.GetComponent<BusPassenger>().jaugeGlobale.SetActive(true);
            playerWaiting = other.transform;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerWaiting.GetComponent<BusPassenger>().jaugeGlobale.SetActive(false);
            playerWaiting = null;
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
