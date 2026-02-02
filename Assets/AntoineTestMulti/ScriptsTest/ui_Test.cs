using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ui_Test : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField joinCodeInputField;
    public TextMeshProUGUI joinCodeText;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            hostButton.onClick.AddListener(StartHost);
            clientButton.onClick.AddListener(StartClient);
            
            joinCodeText.gameObject.SetActive(false);
        }
    }
    
    async void StartHost()
    {
        try
        {
            await TestRelay.instance.CreateRelay();
            
            joinCodeText.gameObject.SetActive(true);
            joinCodeText.text = TestRelay.LastJoinCode;
            
        }
        catch (Exception e)
        {
            Debug.LogError("StartHost Error: " + e);
        }
    }

    async void StartClient()
    {
        try
        {
            string joinCode = joinCodeInputField.text.Trim();
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("Merci de renseigner le join code !");
                return;
            }
            Debug.Log("Bouton Client cliqué, appel à JoinRelay...");
            await TestRelay.instance.JoinRelay(joinCode);
            Debug.Log("JoinRelay terminé (succès ou échec ? Vérifie les logs ci-dessus).");
        }
        catch (Exception e)
        {
            Debug.LogError("StartClient Error: " + e);
        }
    }
}