using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class KnockOut : NetworkBehaviour, IInteractible
{
    public NetworkVariable<bool> isKnockedOut = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isMateRevive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> currentValueToRevive = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField] private MonoBehaviour[] componentsToDisables;
    [SerializeField] private float koForce = 1f;
    [SerializeField] private float soloReviveTime = 10f;
    [SerializeField] private float mateReviveTime = 2f;
    private float soloElapsed = 0;
    private float mateElapsed = 0;

    [SerializeField] private Image reviveImage;
    
    
    public override void OnNetworkSpawn()
    {
        isKnockedOut.OnValueChanged += OnKOStateChange;
        
        reviveImage.transform.parent.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && isKnockedOut.Value)
        {
            ReviveServerRpc();
        }

        if (isKnockedOut.Value)
        {
            if (soloElapsed > 0)
            {
                soloElapsed -= Time.deltaTime;
            }
            else
            {
                ReviveServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void KOServerRpc()
    {
        isKnockedOut.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReviveServerRpc()
    {
        isKnockedOut.Value = false;
    }

    public void OnKOStateChange(bool oldValue, bool newValue)
    {
        foreach (MonoBehaviour component in componentsToDisables)
        {
            component.enabled = !newValue;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (newValue) // KO
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(Random.insideUnitSphere * koForce, ForceMode.Impulse);

            soloElapsed = soloReviveTime;
        }
        else // Revive
        {
            GetComponent<HealthComponent>().Heal();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            soloElapsed = 0;

            if (IsServer)
            {
                isMateRevive.Value = false;
                currentValueToRevive.Value = 0f;
            }


            rb.isKinematic = true;
            rb.useGravity = false;

            Vector3 euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMateReviveServerRpc(ulong reviverClientId)
    {
        if (isMateRevive.Value)
            return;
        
        isMateRevive.Value = true;
        currentValueToRevive.Value = 0f;

        StartCoroutine(MateReviveCoroutine(reviverClientId));
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopMateReviveServerRpc()
    {
        if (!isMateRevive.Value)
            return;
        
        isMateRevive.Value = false;
        currentValueToRevive.Value = 0f;
    }

    private IEnumerator MateReviveCoroutine(ulong reviverClientId)
    {
        ClientRpcParams koParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        ClientRpcParams reviverParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { reviverClientId }
            }
        };

        UpdateReviveUIClientRpc(0f, true, koParams);
        NotifyReviverUIClientRpc(0f, true, reviverParams);

        while (isMateRevive.Value)
        {
            currentValueToRevive.Value += Time.deltaTime / mateReviveTime;
            float value = Mathf.Clamp01(currentValueToRevive.Value);

            UpdateReviveUIClientRpc(value, true, koParams);
            NotifyReviverUIClientRpc(value, true, reviverParams);

            if (value >= 1f)
            {
                ReviveServerRpc();

                isMateRevive.Value = false;
                currentValueToRevive.Value = 0f;

                UpdateReviveUIClientRpc(0f, false, koParams);
                NotifyReviverUIClientRpc(0f, false, reviverParams);
                yield break;
            }

            yield return null;
        }

        UpdateReviveUIClientRpc(0f, false, koParams);
        NotifyReviverUIClientRpc(0f, false, reviverParams);
    }


    [ClientRpc]
    private void NotifyReviverUIClientRpc(
        float value,
        bool isActive,
        ClientRpcParams rpcParams = default)
    {
        if (PlayerRayCast.LocalInstance == null)
            return;

        PlayerRayCast.LocalInstance.UpdateReviveUI(value, isActive);
    }

    
    [ClientRpc]
    private void UpdateReviveUIClientRpc(float value, bool isActive, ClientRpcParams rpcParams = default)
    {
        if (reviveImage == null)
            return;

        reviveImage.transform.parent.gameObject.SetActive(isActive);
        reviveImage.fillAmount = value;
    }


    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }

    public string interactionName;}
