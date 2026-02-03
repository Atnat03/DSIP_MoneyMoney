using UnityEngine;
using Unity.Netcode;

public class PlayerControllerTest : NetworkBehaviour
{
    public float speed = 5f;
    
    void Start()
    {
        if(IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
    }
    
    void Update()
    {
        if(!IsOwner) return;
        
        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.Translate(direction * speed * Time.deltaTime);
    }
}