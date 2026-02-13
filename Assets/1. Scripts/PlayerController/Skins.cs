using UnityEngine;

public class Skins : MonoBehaviour
{
    public Ragdoll[] skinList;

    public Animator GetAnimator(int id)
    {
        return skinList[id].GetComponent<Animator>();
    }

    public Ragdoll GetRagdoll(int id)
    {
        return skinList[id].GetComponent<Ragdoll>();
    }

    public GameObject[] GetSkinnedMeshRenderers(int id)
    {
        Transform currentT = skinList[id].transform;
        
        GameObject[] skinnedMeshRenderers = new GameObject[2];
        skinnedMeshRenderers[0] = currentT.GetChild(1).gameObject;
        skinnedMeshRenderers[1] = currentT.GetChild(2).gameObject;
        
        return skinnedMeshRenderers;
    }

    public void SetSkin(int id)
    {
        for (int i = 0; i < skinList.Length; i++)
        {
            if(i == id)
            {
                print($"Skin {i} active");
                skinList[i].gameObject.SetActive(true);
            }
            else
            {
                skinList[i].gameObject.SetActive(false);
            }
        }
    }

    // ✅ NOUVELLE MÉTHODE : Masquer les renderers d'un skin pour le joueur local
    public void HideSkinnedMeshRenderersForOwner(int id)
    {
        GameObject[] meshes = GetSkinnedMeshRenderers(id);
        
        if (meshes == null || meshes.Length < 2)
        {
            Debug.LogWarning($"Impossible de récupérer les meshes pour le skin {id}");
            return;
        }

        foreach (var mesh in meshes)
        {
            var renderer = mesh.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    public void ShowSkinnedMeshRenderersForOthers(int id)
    {
        GameObject[] meshes = GetSkinnedMeshRenderers(id);
        
        if (meshes == null || meshes.Length < 2)
        {
            Debug.LogWarning($"Impossible de récupérer les meshes pour le skin {id}");
            return;
        }

        foreach (var mesh in meshes)
        {
            var renderer = mesh.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }
}