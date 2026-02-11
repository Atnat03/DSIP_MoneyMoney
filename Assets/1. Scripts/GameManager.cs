using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    
    public NetworkVariable<float> currentTime = new NetworkVariable<float>();
    public float MaxTimer = 10;
    public TextMeshProUGUI textTimer;
    public bool isTimerPause = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        instance = this;

        currentTime.Value = MaxTimer;
        currentTime.OnValueChanged += OnTimerChanged;
        StopTimer();
    }

    private void OnTimerChanged(float previousValue, float newValue)
    {
        UpdateTimerClientRpc(newValue);
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(float newValue)
    {
        textTimer.text = ((int)newValue).ToString();
    }
    
    public void StopTimer() => isTimerPause = true;
    public void StartTimer() => isTimerPause = false;

    private void Update()
    {
        if (!IsServer) return;

        if (currentTime.Value > 0 && !isTimerPause)
        {
            currentTime.Value -= Time.deltaTime;
        }
        else
        {
            EndGameClientRpc();
        }
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
        Debug.Log("END GAME");
    }
}
