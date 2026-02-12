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
}
