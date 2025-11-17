using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class WreckingBallSpawner : MonoBehaviour
{
    [SerializeField] private Camera MainCamera;

    public event System.Action OnCountChanged;

    [SerializeField] private SelectedBulletType currentBulletType = SelectedBulletType.fastball;

    [Header("Bullet Objects")]
    [SerializeField] private List<GameObject> fastballObjects = new List<GameObject>();
    [SerializeField] private List<GameObject>scattershotObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> slugObjects = new List<GameObject>();

    [SerializeField] TextMeshProUGUI counterText;

    [SerializeField] private TextMeshProUGUI selectedObjectText;

    [SerializeField] private RoundManager RoundManager;            

    public Queue<GameObject> spawnedBalls = new Queue<GameObject>();

    private bool isEnabled = true;
    public bool isEnabledAccess
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    [SerializeField] public int MaxBalls = 10;
    public int currentAmmo = 10;
    private float launchForce = 10000f;  

    private int currentProjectileIndex = 0; 

    float ammoExhaustedTimer = 10f;

    void Start()
    {
        //on start - set our currentAmmo counter to the max
        currentAmmo = MaxBalls;
    }

    private void UpdateAmmoCounterUI()
    /**
    On screen UI for ammunition - same as block counterpart
    **/
    {
        counterText.text = $"Ammo: {currentAmmo} / {MaxBalls}";
    }

    private GameObject SpawnBullet(GameObject prefab)
    {
        Vector3 spawnPos = MainCamera.transform.position + MainCamera.transform.forward * 2f;
        GameObject bullet = Instantiate(prefab, spawnPos, Quaternion.identity);

        spawnedBalls.Enqueue(bullet);

        UpdateAmmoCounterUI();
        OnCountChanged?.Invoke();

        return bullet;
    }


    private void LaunchFastball()
    {
        launchForce = 2000f;
        GameObject newBall = SpawnBullet(fastballObjects[currentProjectileIndex]);

        Rigidbody rb = newBall.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(MainCamera.transform.forward * launchForce);

    }

    private void LaunchSlug()
    {
        launchForce = 10000f;

        GameObject slug = SpawnBullet(slugObjects[currentProjectileIndex]);
        Rigidbody rb = slug.GetComponent<Rigidbody>();

        rb.AddForce(MainCamera.transform.forward * launchForce);
    }

    private void LaunchScattershot()
    {
        launchForce = 1000f;

        for (int i = 0; i < 5; i++)
        {
            GameObject pellet = SpawnBullet(scattershotObjects[currentProjectileIndex]);

            Rigidbody rb = pellet.GetComponent<Rigidbody>();
            Vector3 spread = MainCamera.transform.forward +
                         Random.insideUnitSphere * 0.2f;

            rb.AddForce(spread.normalized * launchForce);
        }
    }

    public void LaunchBall()
    {
        if (currentAmmo <= 0) return;

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

            case SelectedBulletType.slug:
                if (currentAmmo >= 5)
                {
                    currentAmmo -= 5;
                    LaunchSlug();
                }

                else
                {
                    selectedObjectText.text = $"Insufficient ammo for slug!";
                }
                
                break;
        }
    }

    public void ResetAmmo()
    {
        currentAmmo = MaxBalls;
        UpdateAmmoCounterUI();
    }

    private void UpdateSelectedBulletUI()
    {
        if (selectedObjectText == null)
            return;

        string currentName = "None";
        GameObject currentPrefab = getCurrentBullet();

        if (currentPrefab != null)
        currentName = currentPrefab.name;

        selectedObjectText.text = $"Shooting: {currentBulletType} ({currentName})";
    }

    private GameObject getCurrentBullet()
    {
        switch (currentBulletType)
        {

            case SelectedBulletType.fastball:
                return fastballObjects[currentProjectileIndex];

            case SelectedBulletType.scattershot:
                return scattershotObjects[currentProjectileIndex];
            
            case SelectedBulletType.slug:
                return slugObjects[currentProjectileIndex];

            default:
                return null;
        }
    }

    private void HandleBulletSelection()
    {
        bool selectionChanged = false;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentBulletType = SelectedBulletType.fastball;
            selectionChanged = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentBulletType = SelectedBulletType.scattershot;
            selectionChanged = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentBulletType = SelectedBulletType.slug;
            selectionChanged = true;
        }

        if (selectionChanged)
        {
            UpdateSelectedBulletUI();
        }
    }


    void Update()
    {
        if (!isEnabled) return;

        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                LaunchBall();
            }

            HandleBulletSelection();
        }

        if(currentAmmo - MaxBalls == 0)
        {
            ammoExhaustedTimer -= Time.deltaTime;

            if (ammoExhaustedTimer == 0)
            {
                RoundManager.SwitchPhase(RoundManager.Phase.Attacker);

            }
        }
    }

    public enum SelectedBulletType
    {
        fastball,

        scattershot,

        slug,
    }   
}
