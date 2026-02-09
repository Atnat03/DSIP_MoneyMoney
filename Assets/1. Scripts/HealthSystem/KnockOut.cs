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
    public NetworkVariable<bool> isMateRevive = new NetworkVariable<bool>(false);
    public NetworkVariable<float> currentValueToRevive = new NetworkVariable<float>();
    
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
        currentValueToRevive.OnValueChanged += OnCurrentValueChange;
        
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

    [ServerRpc]
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
            isMateRevive.Value = false;
            currentValueToRevive.Value = 0f;


            rb.isKinematic = true;
            rb.useGravity = false;

            Vector3 euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
    }


    IEnumerator StandUp(Rigidbody rb)
    {
        NetworkTransform transformN = GetComponent<NetworkTransform>();
        transformN.enabled = false;
        
        rb.isKinematic = true;
        rb.useGravity = false;

        Quaternion start = transform.rotation;
        Quaternion target = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        float t = 0f;
        while (t < 1f)
        {
            transform.rotation = Quaternion.Slerp(start, target, t);
            t += Time.deltaTime;
            yield return null;
        }

        transform.rotation = target;
        transformN.enabled = true;
    }
    
    
    private void OnCurrentValueChange(float previous, float current)
    {
        if (reviveImage != null)
            reviveImage.fillAmount = current;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMateReviveServerRpc(ulong reviverClientId)
    {
        isMateRevive.Value = true;
        currentValueToRevive.Value = 0f;

        StartCoroutine(MateReviveCoroutine(reviverClientId));
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopMateReviveServerRpc()
    {
        isMateRevive.Value = false;
        currentValueToRevive.Value = 0f;
    }

    private IEnumerator MateReviveCoroutine(ulong reviverClientId)
    {
        ulong[] targets = new ulong[] { reviverClientId, OwnerClientId };

        while (isMateRevive.Value)
        {
            currentValueToRevive.Value += Time.deltaTime / mateReviveTime;

            UpdateReviveUIClientRpc(targets, currentValueToRevive.Value, true);

            if (currentValueToRevive.Value >= 1f)
            {
                ReviveServerRpc();
                isMateRevive.Value = false;
                currentValueToRevive.Value = 0f;
                UpdateReviveUIClientRpc(targets, 0f, false);
                yield break;
            }

            yield return null;
        }

        UpdateReviveUIClientRpc(targets, 0f, false);
    }


    
    [ClientRpc]
    private void UpdateReviveUIClientRpc(ulong[] targetClientIds, float value, bool isEnding)
    {
        if (!targetClientIds.Contains(NetworkManager.Singleton.LocalClientId))
            return;

        if (reviveImage != null)
        {
            reviveImage.transform.parent.gameObject.SetActive(isEnding);
            reviveImage.fillAmount = value;
        }
    }

    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }

    public string interactionName;}
