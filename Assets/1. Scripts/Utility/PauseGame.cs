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
    [SerializeField] private Button continueButton;
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
        
        continueButton.onClick.AddListener(ContinueTheGame);
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
        
        PartyCodeTxt.text = AutoJoinedLobby.Instance.CodeLobby;
        
        if (Input.GetKeyDown(KeyCode.Escape) && !isPause)
        {
            fpsController.StartFreeze();
            isPause = !isPause;
            
            Cursor.lockState = isPause ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPause;
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
            continueButton.interactable = true;
        }
        else
        {
            buttonPauseText.text = pauseText[1];
            continueButton.interactable = false;
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
    
    private void ContinueTheGame()
    {
        Debug.Log("ContinueTheGame");

        if (isPause)
        {
            fpsController.StopFreeze();
            isPause = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    public void QuitTheGame()
    {
        Debug.Log("QuittingTheGame");
        
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
        Application.Quit();
    }
}

