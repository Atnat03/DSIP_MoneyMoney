using UnityEngine;

public class GrabPoint : MonoBehaviour
{
    public Vector3 positionObject;
    public void Grab(GameObject player)
    { 
        transform.SetParent(player.transform);
        transform.position = player.transform.position + positionObject;
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<SphereCollider>().isTrigger = true;
    }
}