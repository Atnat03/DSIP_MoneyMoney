using UnityEngine;

public class LookAtTarget : MonoBehaviour
{
    [Header("Target to look at")]
    public Transform target; // La cible que l'objet doit regarder

    [Header("Optional Settings")]
    public bool onlyY = false; // Si vrai, l'objet ne tourne que sur l'axe Y

    void Update()
    {
        if (target == null) return; // Si aucune cible n'est assignée, on sort

        if (onlyY)
        {
            // On ne tourne que sur Y
            Vector3 direction = target.position - transform.position;
            direction.y = 0; // ignore la différence de hauteur
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        else
        {
            // Rotation complète vers la cible
            transform.LookAt(target);
        }
    }
}