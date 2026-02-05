using System;
using UnityEngine;

public class PlayerRayCast : MonoBehaviour
{
    public UIController uiController;

    private void Start()
    {
        uiController = GameObject.Find("Canvas UI")?.GetComponent<UIController>();
    }

    private void Update()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, gameObject.transform.forward);
        Debug.DrawRay(transform.position, gameObject.transform.forward, Color.green);
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") && hit.distance < 5f)
            {
                Debug.Log("hit");
                uiController?.OnInteract();
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Interact.RayInteract(hit.collider.gameObject, gameObject);
                }
            }
            else
            {
                uiController?.OnStopInteract();
            }
        }
    }
}