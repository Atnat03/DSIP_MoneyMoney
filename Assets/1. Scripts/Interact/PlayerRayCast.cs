using System;
using System.Collections;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRayCast : NetworkBehaviour
{
    public UIController uiController;
    public float durationRepair;

    public Image circleCD;

    public bool hasMaterial;
    public GameObject materialVisual;

    public float hitDistance = 2f;
    
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
        
        if (Physics.Raycast(ray, out hit))
        {
            if ((hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") && hit.distance < hitDistance))
            {
                if (!hit.collider.CompareTag("TruckPart") || 
                    (hit.collider.CompareTag("TruckPart") && hit.collider.GetComponent<TruckPart>().isBroke.Value && hasMaterial))
                {
                    uiController?.OnInteract();
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
                        Interact.RayInteract(hit.collider.gameObject, gameObject);
                    }
                }
            }
            else
            {
                uiController?.OnStopInteract();
            }
        }

        if (Input.GetButtonDown("Fire2") && hasMaterial && !GetComponent<FPSControllerMulti>().isFreeze)
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
        GetComponent<FPSControllerMulti>().isFreeze = true;
        float count = durationRepair;
        while (count > 0)
        {
            count -= Time.deltaTime;
            yield return null;
            circleCD.fillAmount =  count / durationRepair;
        }
        Interact.RayInteract(truck, gameObject);
        GetComponent<FPSControllerMulti>().isFreeze = false;
        TakeMaterial();
    }
}