using System;
using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustom : NetworkBehaviour
{
    public FPSControllerMulti controllerScript;
    
    public TextMeshProUGUI nameText;

    public NetworkVariable<FixedString32Bytes> PlayerName =
        new NetworkVariable<FixedString32Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    
    public MeshRenderer playerRenderer;
    public NetworkVariable<Color> colorPlayer = new NetworkVariable<Color>();

    [Header("EMOTES")] 
    public KeyCode emoteKey = KeyCode.Tab;
    public GameObject EmoteParent;
    public Image emoteImage;
    public float timeEmoteVisible = 3f;
    public NetworkVariable<bool> isEmoting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public EmoteManager emoteManager;
    public NetworkVariable<int> currentEmoteIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [HideInInspector]
    public int localSelectedEmoteIndex = 0;
    
    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnNameChanged;
        colorPlayer.OnValueChanged += OnColorChanged;
        isEmoting.OnValueChanged += OnEmotingChanged;
        currentEmoteIndex.OnValueChanged += OnEmoteIndexChanged;

        OnNameChanged("", PlayerName.Value);

        if (IsOwner)
        {
            string name = AutoJoinedLobby.Instance.LocalPlayerName;
            SubmitNameServerRpc(name);
            
            Color localColor = AutoJoinedLobby.Instance.LocalPlayerColor;
            SubmitColorServerRpc(localColor);
        }
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
        colorPlayer.OnValueChanged -= OnColorChanged;
        isEmoting.OnValueChanged -= OnEmotingChanged;
    }

    private void OnColorChanged(Color previousValue, Color newValue)
    {
        playerRenderer.GetComponent<MeshRenderer>().material.color = newValue;
    }

    private void OnNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        if (nameText != null)
            nameText.text = newName.ToString();
    }

    [ServerRpc]
    void SubmitNameServerRpc(string name)
    {
        PlayerName.Value = name;
    }
    
    [ServerRpc]
    void SubmitColorServerRpc(Color color)
    {
        colorPlayer.Value = color;
    }
    
    
    #region Emotes

    private void Update()
    {
        if (!IsOwner) return;
        
        if(isEmoting.Value)
            return;

        if (Input.GetKeyDown(emoteKey))
        {
            controllerScript.isFreeze = true;

            emoteManager.gameObject.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyUp(emoteKey))
        {
            controllerScript.isFreeze = false;

            int finalEmoteIndex = localSelectedEmoteIndex;
            
            emoteManager.gameObject.SetActive(false);

            SubmitEmoteServerRpc(finalEmoteIndex);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SubmitEmoteServerRpc(int emoteIndex)
    {
        currentEmoteIndex.Value = emoteIndex;
    }
    
    private void OnEmotingChanged(bool previousValue, bool newValue)
    {
        EmoteParent.SetActive(newValue);
    }
    
    private void OnEmoteIndexChanged(int previous, int newValue)
    {
        Sprite sprite = emoteManager.GetEmoteByIndex(newValue);

        if(sprite != null)
        {
            emoteImage.sprite = sprite;
            EmoteParent.SetActive(true);
            isEmoting.Value = true;
            StartCoroutine(DisableEmoteAfterTime());
        }
    }
    IEnumerator DisableEmoteAfterTime()
    {
        yield return new WaitForSeconds(timeEmoteVisible);
        EmoteParent.SetActive(false);
        isEmoting.Value = false;
    }

    #endregion

}
