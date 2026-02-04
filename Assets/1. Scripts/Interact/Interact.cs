using System;
using UnityEngine;

public static class Interact
{
    public static event Action<GameObject> OnInteract;

    
    public static void RayInteract(GameObject obj)
    {
        OnInteract?.Invoke(obj);
    }
}
