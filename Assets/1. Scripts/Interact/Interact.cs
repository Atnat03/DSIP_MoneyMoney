using System;
using UnityEngine;

public static class Interact
{
    public static event Action<GameObject, GameObject> OnInteract;

    
    public static void RayInteract(GameObject obj,  GameObject player)
    {
        OnInteract?.Invoke(obj, player);
    }
}
