using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerCustom : NetworkBehaviour
{
    public TextMeshProUGUI nameText;

    public NetworkVariable<FixedString32Bytes> PlayerName =
        new NetworkVariable<FixedString32Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    
    public MeshRenderer playerRenderer;
    public NetworkVariable<Color> colorPlayer = new NetworkVariable<Color>();

    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnNameChanged;
        colorPlayer.OnValueChanged += OnColorChanged;

        OnNameChanged("", PlayerName.Value);

        if (IsOwner)
        {
            string name = AutoJoinedLobby.Instance.LocalPlayerName;
            SubmitNameServerRpc(name);
            
            Color localColor = AutoJoinedLobby.Instance.LocalPlayerColor;
            SubmitColorServerRpc(localColor);
        }
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

}
