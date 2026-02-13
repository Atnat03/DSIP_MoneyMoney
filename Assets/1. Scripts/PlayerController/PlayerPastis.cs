using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerPastis : NetworkBehaviour
{
    [Header("Hand Bottle")]
    public GameObject handBottle;
    public Animator animator;
    public Transform liquide;
    private Vector3 liquideScale;

    public float timeToDrink = 0.5f;
    float elapsedTime = 0;
    private bool canDrink;
    
    public bool hasBottleInHand = false;
    public int numberGorgerToEndBottle = 3;
    private int currentGorgerBottle;

    public AudioClip drinkSFX;
    public AudioClip rotSFX;
    
    [Header("Throw Bottle")]
    public NetworkObject throwBottlePrefab;
    public Transform throwPos;
    public float throwBottleForce = 10;
    public Vector3 throwRotateTroqueForce;
    
    [Header("UI")]
    public GameObject uiClique;

    public DamageEffectController effet;

    private FPSControllerMulti fps;
    private AudioSource audioSource;

    public override void OnNetworkSpawn()
    {
        fps = GetComponent<FPSControllerMulti>();
        audioSource = GetComponent<AudioSource>();
        handBottle.SetActive(false);
    }

    public void TakeABottle()
    {
        handBottle.SetActive(true);
        hasBottleInHand = true;
        currentGorgerBottle = numberGorgerToEndBottle;
        
        uiClique.SetActive(false);
        
        liquide.localScale = Vector3.one;

        fps.hasSomethingInHand = true;
    }
    
    private void ReposeLaBouteille()
    {
        handBottle.SetActive(false);
        hasBottleInHand = false;

        fps.hasSomethingInHand = false;
        
        liquide.localScale = Vector3.one;
        
        uiClique.SetActive(false);
    }

    private void TakeASip()
    {
        if(!canDrink)
            return;
        
        currentGorgerBottle--;

        float t = (float)currentGorgerBottle / numberGorgerToEndBottle;
        float yScale = Mathf.Lerp(0.05f, 1f, t);
        liquide.localScale = new Vector3(liquide.localScale.x, yScale, liquide.localScale.z);
        
        AudioClip c = currentGorgerBottle != 0 ?  drinkSFX : rotSFX;

        audioSource.PlayOneShot(c);
        
        if (currentGorgerBottle == 0)
        {
            StartCoroutine(EffetBourrer());
        }

        animator.SetTrigger("Drink");
        
        elapsedTime = timeToDrink;
        canDrink = false;
    }

    IEnumerator EffetBourrer()
    {
        float duration = 0.25f;
        float elapsed = 0;

        effet.SetColor(Color.darkOliveGreen);
        
        effet.SetDizziness(0.5f);
        
        while (elapsed <  duration)
        {
            effet.SetIntensity(elapsed / (duration + 0.25f));
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        effet.SetIntensity(0.5f);
        
        yield return new WaitForSeconds(3f);
        
        while (elapsed > 0)
        {
            effet.SetIntensity(elapsed / (duration + 0.25f));
            
            elapsed -= Time.deltaTime;
            yield return null;
        }
        
        effet.SetDizziness(0f);
        
        effet.SetIntensity(0);
    }

    [ServerRpc]
    private void ThrowBottleServerRpc()
    {
        NetworkObject nt = Instantiate(throwBottlePrefab, throwPos.position, throwPos.rotation);
        nt.Spawn();

        Rigidbody rb = nt.GetComponent<Rigidbody>();
        
        Vector3 forceDir = fps.MyCamera().transform.forward * throwBottleForce;
        rb.AddForce(forceDir, ForceMode.Impulse);
        rb.AddTorque(throwRotateTroqueForce);
        
        ReposeLaBouteilleClientRpc();
    }
    
    [ClientRpc]
    private void ReposeLaBouteilleClientRpc()
    {
        ReposeLaBouteille();
    }

    public void Update()
    {
        if(!IsOwner)return;
        
        if (!hasBottleInHand)
            return;
        
        uiClique.SetActive(currentGorgerBottle == 0);

        if (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
        }
        else
        {
            canDrink = true;
        }

        if (Input.GetButtonDown("Fire2"))
        {
            if (currentGorgerBottle > 0)
            {
                TakeASip();
            }
            else
            {
                ThrowBottleServerRpc();
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            ReposeLaBouteille();
        }
    }
}
