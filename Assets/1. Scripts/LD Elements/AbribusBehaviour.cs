using UnityEngine;

public class AbribusBehaviour : MonoBehaviour
{
    public float timeForTP;
    [SerializeField]private float _currentTimer;

    public Transform playerWaiting;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("please");
            playerWaiting = other.transform;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerWaiting = null;
        }
    }
    
    void Update()
    {
        if (playerWaiting != null)
        {
            _currentTimer -= Time.deltaTime;
            if (_currentTimer <= 0)
            {
                BankManager.instance.TeleportNextBank(playerWaiting);
            }
        }
        else
        {
            _currentTimer = timeForTP;
        }
    }
}
