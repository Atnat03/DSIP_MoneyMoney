using System;
using UnityEngine;

public class PlayerRayCast : MonoBehaviour
{
    public UIController uiController;

    private void Start()
    {
        uiController = GameObject.Find("Canvas UI").GetComponent<UIController>();
    }

    private void Update()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, gameObject.transform.forward);
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") && hit.distance < 5f)
            {
                uiController.OnInteract();
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Interact.RayInteract(gameObject);
                }
            }
            else
            {
                uiController.OnStopInteract();
            }
        }   
    }
}
