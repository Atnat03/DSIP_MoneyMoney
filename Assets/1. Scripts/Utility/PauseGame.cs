using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PauseGame : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPause;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button debugCamionButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI buttonPauseText;
    [SerializeField] private TextMeshProUGUI PartyCodeTxt;
    [SerializeField] private GameObject pausingInfo;
    
    [Header("Other")]
    [SerializeField] private string[] pauseText;
    private bool isPause;
    FPSControllerMulti fpsController;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            Destroy(pauseButton.gameObject);
            Destroy(debugCamionButton.gameObject);
            Destroy(PartyCodeTxt.gameObject);
        }
        else
        {
            pauseButton.onClick.AddListener(PausingTheGame);
            debugCamionButton.onClick.AddListener(ResetCametar);
        }
        
        quitButton.onClick.AddListener(QuitTheGame);
        
        fpsController = GetComponent<FPSControllerMulti>();
        
        pausingInfo.SetActive(false);
    }

    private void ResetCametar()
    {
        TruckController.instance.ResetCamionToNearPoint();
    }

    private void Update()
    {
        uiPause.SetActive(isPause);
        
        if(!IsServer)
            pausingInfo.SetActive(Time.timeScale == 0);
        
        PartyCodeTxt.text = AutoJoinedLobby.Instance.RelayCode;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPause = !isPause;

            if (isPause)
            {
                fpsController.StartFreeze();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                fpsController.StopFreeze();
                            
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void PausingTheGame()
    {
        Debug.Log("PausingTheGame");

        if (!IsServer)
            return;
        
        if(Time.timeScale == 0)
        {
            buttonPauseText.text = pauseText[0];
        }
        else
        {
            buttonPauseText.text = pauseText[1];
        }
        
        UpdateFreezeTimeClientRpc();
    }

    [ClientRpc]
    private void UpdateFreezeTimeClientRpc()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
        else
        {
            Time.timeScale = 0;
        }
    }
    
    public void QuitTheGame()
    {
        Debug.Log("QuittingTheGame");

        if (IsServer)
        {
            NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
         
            if(IsHost)
                NetworkManager.Singleton.Shutdown();
        }
        
        Application.Quit();
    }
}

