using UnityEngine;

public class testviteuf : MonoBehaviour
{
    void Update()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 100))
        {
            Debug.Log("Hit : " + hit.collider.name);
        }
    }

}
