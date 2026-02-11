using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EndGame : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiEnd;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI sacScore;
    [SerializeField] private TextMeshProUGUI lingotScore;
    
    [Header("Other")]
    public bool isEnd;
    FPSControllerMulti fpsController;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            Destroy(replayButton.gameObject);
        }
        else
        {
            replayButton.onClick.AddListener(RestartTheGame);
        }
        
        quitButton.onClick.AddListener(QuitTheGame);
        
        fpsController = GetComponent<FPSControllerMulti>();
        isEnd = false;
    }

    public void SetScore(int sac, int lingot)
    {
        sacScore.text = sac.ToString();
        lingotScore.text = lingot.ToString();
    }

    private void Update()
    {
        uiEnd.SetActive(isEnd);
        
        if (isEnd)
        {
            fpsController.StartFreeze();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void RestartTheGame()
    {
        Debug.Log("Restarting the game");

        if (IsServer)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void QuitTheGame()
    {
        Debug.Log("QuittingTheGame");
        
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
        Application.Quit();
    }
}
