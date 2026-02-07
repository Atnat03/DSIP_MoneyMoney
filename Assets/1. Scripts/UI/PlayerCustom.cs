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


    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnNameChanged;

        OnNameChanged("", PlayerName.Value);

        if (IsOwner)
        {
            string name = AutoJoinedLobby.Instance.LocalPlayerName;
            SubmitNameServerRpc(name);
        }
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

}
