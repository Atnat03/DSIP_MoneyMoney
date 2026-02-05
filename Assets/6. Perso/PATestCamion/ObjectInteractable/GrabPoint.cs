using UnityEngine;

public class GrabPoint : MonoBehaviour
{
    public void Grab(GameObject player)
    { 
        transform.SetParent(player.transform);
        transform.localPosition = player.transform.localPosition;
        GetComponent<Rigidbody>().isKinematic = false;
    }
}