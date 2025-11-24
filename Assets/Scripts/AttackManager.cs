using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("Bullet Objects")]
    [SerializeField] private GameObject FastBall;
    [SerializeField] private GameObject Pellet;
    [SerializeField] private GameObject Slug;
    [SerializeField] private SelectedBulletType currentBulletType = SelectedBulletType.fastball;
    [HideInInspector] public Queue<GameObject> spawnedBalls = new Queue<GameObject>();

    [Header("Bullet Attributes and Misc. Settings")]
    [SerializeField] public int MaxBalls = 10;
    [SerializeField] private float launchForce = 10000f; 
    [HideInInspector] private int currentAmmo = 10;
    [HideInInspector] float ammoExhaustedTimer = 5f;

    [Header("UI Elements and Scripts")]
    [SerializeField] private RoundManager RoundManager;  
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private TextMeshProUGUI selectedObjectText;
    [SerializeField] private Camera MainCamera;
    [HideInInspector] string currentName = "Fastball";

    private bool isEnabled = true;
    public bool isEnabledAccess
    {
        get => isEnabled;
        set => isEnabled = value;
    }
    public enum SelectedBulletType
    {
        fastball,

        scattershot,

        slug,
    } 

    void Start()
    {
        //on start - set ammo to the max
        ResetAmmo();
    }

    void Update()
    {
        //if we're not allowed to be here - kick out 
        if (!isEnabled) return;

        //otherwise - if we're out of bullets we need to decrement out cooldown
        else if(currentAmmo == 0)
        {
            //i do this to give time for round to 'settle' after all round shot 
            ammoExhaustedTimer -= Time.deltaTime;

            //Debug.Log($"Ammo Exhuasted - {currentAmmo}");

            //ensure our defense object isn't mid fall/some other weird situation
            //we need to switch round now 
            if (ammoExhaustedTimer <= 0)
            {
                //Debug.Log("Switching to defender - player out of bullets");
                RoundManager.defenderScore += 1;
                RoundManager.SwitchPhase(RoundManager.Phase.Defender);
            }
        }

        //if we pass above checks, we are valid in this position
        else
        {
            //if user is clicking - shoot the ball we have selected
            if (Input.GetMouseButtonDown(0))
            {
                LaunchBall();
            }

            //if user selects a different ball - update UI
            HandleBulletSelection();

            //Debug.Log($"Ammo left - {currentAmmo}");

            selectedObjectText.text = $"Shooting: {currentName}";
        }
    }

    public void ResetAmmo()
    /*
    Set Ammo count back to the default max - RoundManager needs to use this om phase switch
    */
    {
        currentAmmo = MaxBalls;
        counterText.text = $"Ammo: {currentAmmo} / {MaxBalls}";
    }

    private void HandleBulletSelection()
    /*
    Called from update every frame - if the user clicks any of the 3 designated keys change to the proper bullet
    */
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentBulletType = SelectedBulletType.fastball;
            currentName = "Fastball";
            //selectedObjectText.text = $"Shooting: {currentName}";
        }

        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentBulletType = SelectedBulletType.scattershot;
            currentName = "Scattershot";
            //selectedObjectText.text = $"Shooting: {currentName}";
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentBulletType = SelectedBulletType.slug;
            currentName = "Slug";
            //selectedObjectText.text = $"Shooting: {currentName}";
        }
    } 

    public void LaunchBall()
    /*
    Called from update when user clicks to shoot - will call 3 subfunctions depending on 
    the type of selected projectile (FB, Scatter or Slug) and will decrement the ammo count
    Update shoot --> launch ball --> launch whatever type --> (spawns projectile and rb force operation)
    */
    {
        if (currentAmmo <= 0) return;

        //depending on type of bullet - call a different launch function
        switch (currentBulletType)
        {
            case SelectedBulletType.fastball:
            currentAmmo--;
            LaunchFastball();
            break;

            case SelectedBulletType.scattershot:
            currentAmmo--;
            LaunchScattershot();
            break;

            //slug uses 5 bullet resources so we need to check if valid
            case SelectedBulletType.slug:
            if (currentAmmo >= 5)
            {
                currentAmmo -= 5;
                LaunchSlug();
                break;
            }

            else
            {
                selectedObjectText.text = $"Insufficient ammo for slug!";
                break;
            }
        }
    }

    private GameObject SpawnBullet(GameObject prefab)
    /*
    We need to instantiate the bullet object at the camera position, then
    enqueue it to our spawnedBalls, and finally update our UI element.
    */
    {
        //define the spawn position, slightly in front of the camera
        Vector3 spawnPos = MainCamera.transform.position + MainCamera.transform.forward * 2f;
        
        //instantiate the bullet, use quaternion.identity for 
        GameObject bullet = Instantiate(prefab, spawnPos, Quaternion.identity);

        //save this so we can track our elements for clean up later
        spawnedBalls.Enqueue(bullet);

        //update the UI
        counterText.text = $"Ammo: {currentAmmo} / {MaxBalls}";

        //instantiate the bullet, use quaternion.identity for 
        return bullet;
    }

    private void LaunchFastball()
    {
        float force = launchForce;

        GameObject fastBall = SpawnBullet(FastBall);
        Rigidbody rb = fastBall.GetComponent<Rigidbody>();

        rb.AddForce(MainCamera.transform.forward * force);
    }

    private void LaunchScattershot()
    {
        //these are small and with low mass - less launch force
        float force = launchForce / 5;

        //we want 5 pellets
        for (int i = 0; i < 5; i++)
        {
            GameObject pellet = SpawnBullet(Pellet);
            Rigidbody rb = pellet.GetComponent<Rigidbody>();

            //use random unit sphere to simulate random scatter
            Vector3 spread = MainCamera.transform.forward +
                         Random.insideUnitSphere * 0.2f;

            //spread is just the camera + the random unit sphere
            rb.AddForce(spread.normalized * force);
        }
    }

    private void LaunchSlug()
    {
        //more mass so need more launch force
        float force = launchForce * 5;

        GameObject slug = SpawnBullet(Slug);
        Rigidbody rb = slug.GetComponent<Rigidbody>();

        rb.AddForce(MainCamera.transform.forward * force);
    }
}
