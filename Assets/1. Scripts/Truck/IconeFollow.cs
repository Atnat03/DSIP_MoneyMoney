using UnityEngine;

public class IconeFollow : MonoBehaviour
{
    public Transform targetFollow;
    void Update()
    {
        transform.position = new Vector3(targetFollow.transform.position.x, transform.position.y, targetFollow.transform.position.z);
    }
}
