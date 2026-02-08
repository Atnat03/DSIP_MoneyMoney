using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustom : NetworkBehaviour
{
    public FPSControllerMulti controllerScript;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public MeshRenderer playerRenderer;

    [Header("EMOTES")]
    public KeyCode emoteKey = KeyCode.Tab;
    public GameObject EmoteParent;
    public Image emoteImage;
    public float timeEmoteVisible = 3f;
    public EmoteManager emoteManager;

    // Networked variables
    public NetworkVariable<FixedString32Bytes> PlayerName =
        new NetworkVariable<FixedString32Bytes>("",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<Color> colorPlayer =
        new NetworkVariable<Color>(Color.white,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> currentEmoteIndex =
        new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    [HideInInspector]
    public int localSelectedEmoteIndex = 0;

    // Local only
    private bool isEmoting = false;

    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnNameChanged;
        colorPlayer.OnValueChanged += OnColorChanged;
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
        currentEmoteIndex.OnValueChanged -= OnEmoteIndexChanged;
    }

    private void OnColorChanged(Color previousValue, Color newValue)
    {
        playerRenderer.material.color = newValue;
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
        if (isEmoting) return;

        // Ouvrir le menu emotes
        if (Input.GetKeyDown(emoteKey))
        {
            controllerScript.isFreeze = true;
            emoteManager.gameObject.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Relâchement : lancer l'emote sélectionnée
        if (Input.GetKeyUp(emoteKey))
        {
            controllerScript.isFreeze = false;

            int finalEmoteIndex = localSelectedEmoteIndex;

            // Hide menu
            emoteManager.gameObject.SetActive(false);

            // Submit emote au serveur pour synchronisation
            SubmitEmoteServerRpc(finalEmoteIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitEmoteServerRpc(int emoteIndex)
    {
        currentEmoteIndex.Value = emoteIndex;
    }

    private void OnEmoteIndexChanged(int previous, int newValue)
    {
        Sprite sprite = emoteManager.GetEmoteByIndex(newValue);
        if (sprite != null)
        {
            emoteImage.sprite = sprite;
            EmoteParent.SetActive(true);
            StartCoroutine(DisableEmoteAfterTime());
        }
    }

    private IEnumerator DisableEmoteAfterTime()
    {
        isEmoting = true;
        yield return new WaitForSeconds(timeEmoteVisible);
        EmoteParent.SetActive(false);
        isEmoting = false;
    }

    #endregion
}
