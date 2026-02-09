using System;
using System.Collections;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public interface IInteractible
{
    public string InteractionName { get; set; }
}

public class PlayerRayCast : NetworkBehaviour
{
    public UIController uiController;
    public float durationRepair;

    public Image circleCD;

    public bool hasMaterial;
    public GameObject materialVisual;

    public float hitDistance = 2f;

    public string RepearInteractionName = "Réparer";
    
    private void Start()
    {
        uiController = VariableManager.instance.uiController;
        circleCD = VariableManager.instance.circleCD;
        materialVisual.transform.parent = GetComponent<FPSControllerMulti>().MyCamera().transform;
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        if(GetComponent<FPSControllerMulti>().MyCamera() == null)
            return;
        
        RaycastHit hit;
        Ray ray = new Ray(GetComponent<FPSControllerMulti>().MyCamera().transform.position, GetComponent<FPSControllerMulti>().MyCamera().transform.forward);
        Debug.DrawRay(GetComponent<FPSControllerMulti>().MyCamera().transform.position, GetComponent<FPSControllerMulti>().MyCamera().transform.forward, Color.green);
        int layerMask = ~LayerMask.GetMask("IgnoreRaycast");
        if (Physics.Raycast(ray, out hit, hitDistance,layerMask))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                if ((!hit.collider.CompareTag("TruckPart") || 
                    (hit.collider.CompareTag("TruckPart") && hit.collider.GetComponent<TruckPart>().isBroke.Value && hasMaterial)))
                {
                    if(hit.collider.CompareTag("Sangles"))
                    {
                        if (!GetComponent<GrabPoint>().IsSacInHand() && !hit.collider.GetComponent<Sangles>().IsStock())
                            return;
                    }
                        
                    if (hit.collider.TryGetComponent<IInteractible>(out var interactible))
                    {
                        uiController?.OnInteract();
                        uiController?.SetText(interactible.InteractionName);
                    }
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (hit.collider.CompareTag("TruckPart") && hasMaterial)
                    {
                        if (hit.collider.GetComponent<TruckPart>().isBroke.Value)
                        {
                            StartCoroutine(RepairPart(hit.collider.gameObject));
                        }
                    }
                    else if (hit.collider.CompareTag("Material"))
                    {
                        TakeMaterial();
                    }
                    else if(!hit.collider.CompareTag("TruckPart"))
                    {
                        Interact.RayInteract(hit.collider.gameObject, gameObject, RepearInteractionName);
                    }
                }
            }
        }
        else
        {
            uiController?.OnStopInteract();
        }

        if (Input.GetButtonDown("Fire2") && hasMaterial && !GetComponent<FPSControllerMulti>().IsFreeze)
        {
            TakeMaterial();
        }
    }

    public void TakeMaterial()
    {
        hasMaterial = !hasMaterial;
        GetComponent<FPSControllerMulti>().hasSomethingInHand = hasMaterial;
        materialVisual.SetActive(hasMaterial);
    }

    public IEnumerator RepairPart(GameObject truck)
    {
        GetComponent<FPSControllerMulti>().StartFreeze();
        float count = durationRepair;
        while (count > 0)
        {
            count -= Time.deltaTime;
            yield return null;
            circleCD.fillAmount =  count / durationRepair;
        }
        Interact.RayInteract(truck, gameObject, RepearInteractionName);
        GetComponent<FPSControllerMulti>().StopFreeze();
        TakeMaterial();
    }
}