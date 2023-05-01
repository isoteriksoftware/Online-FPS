using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform viewPoint;
    [SerializeField] Transform groundCheckPoint;
    [SerializeField] float mouseSensitivity = 1f;
    [SerializeField] bool invertLook;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float gravityMod = 2.5f;
    [SerializeField] float maxHeat = 10f;
    [SerializeField] float coolRate = 4f;
    [SerializeField] float overheatCoolRate = 5f;
    [SerializeField] float muzzleDisplayTime;
    [SerializeField] int maxHealth = 100;
    [SerializeField] CharacterController characterController;
    [SerializeField] LayerMask groundLayers;
    [SerializeField] GameObject bulletImpact;
    [SerializeField] Gun[] guns;
    [SerializeField] GameObject playerHitImpact;
    [SerializeField] Animator animator;
    [SerializeField] GameObject playerModel;
    [SerializeField] Transform modelGunpoint;
    [SerializeField] Transform gunHolder;

    public Material[] skins;
    public float adsSpeed = 5f;
    public Transform adsOutPoint, adsInPoint;
    public AudioSource footstepSlow, footstepFast;
    public Camera minimapCamera;
    public GameObject minimapIndicator;
    public GameObject minimapImage;
    public Material blueMaterial, redMaterial;

    float verticalRotationStore;
    float activeMoveSpeed;
    float shotCounter;
    float heatCounter;
    float muzzleCounter;
    Vector2 mouseInput;
    Vector3 moveDirection, movement;
    Camera mainCamera;
    bool isGrounded;
    bool isOverheated;
    int selectedGun;
    int currentHealth;
    bool isPaused;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mainCamera = Camera.main;

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;

        if (photonView.IsMine)
        {
            playerModel.SetActive(false);

            UIController.instance.getWeaponTempSlider().maxValue = maxHeat;
            UIController.instance.getHealthSlider().maxValue = maxHealth;
            minimapIndicator.GetComponent<Renderer>().material = blueMaterial;
        }
        else
        {
            gunHolder.parent = modelGunpoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
            minimapIndicator.GetComponent<Renderer>().material = redMaterial;
            minimapCamera.gameObject.SetActive(false);
        }

        playerModel.GetComponent<Renderer>().material = skins[photonView.Owner.ActorNumber % skins.Length];
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    UIController.instance.pauseScreen.SetActive(false);
                    isPaused = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    UIController.instance.pauseScreen.SetActive(true);
                    isPaused = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (UIController.instance.mapScreen.activeInHierarchy)
                {
                    UIController.instance.mapScreen.SetActive(false);
                    minimapCamera.fieldOfView = 60f;
                    minimapImage.SetActive(true);
                }
                else
                {
                    UIController.instance.mapScreen.SetActive(true);
                    minimapCamera.fieldOfView = 106f;
                    minimapImage.SetActive(false);
                }
            }

            if (!isPaused)
            {
                mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,
                    transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

                verticalRotationStore += mouseInput.y;
                verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60f, 60f);

                float lookDirection = 1f;
                if (!invertLook)
                    lookDirection = -1f;

                viewPoint.rotation = Quaternion.Euler(verticalRotationStore * lookDirection, viewPoint.rotation.eulerAngles.y,
                    viewPoint.rotation.eulerAngles.z);

                moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    activeMoveSpeed = runSpeed;

                    if (!footstepFast.isPlaying && moveDirection != Vector3.zero)
                    {
                        footstepFast.Play();
                        footstepSlow.Stop();
                    }
                }
                else
                {
                    activeMoveSpeed = moveSpeed;

                    if (!footstepSlow.isPlaying && moveDirection != Vector3.zero)
                    {
                        footstepFast.Stop();
                        footstepSlow.Play();
                    }
                }

                if (moveDirection == Vector3.zero || !isGrounded)
                {
                    footstepFast.Stop();
                    footstepSlow.Stop();
                }

                float yVelocity = movement.y;
                movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
                movement.y = yVelocity;

                isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

                if (isGrounded)
                {
                    movement.y = 0f;
                }

                if (Input.GetButtonDown("Jump") && isGrounded)
                {
                    movement.y = jumpForce;
                }

                movement.y += Physics.gravity.y * gravityMod * Time.deltaTime;

                characterController.Move(movement * Time.deltaTime);

                if (guns[selectedGun].muzzleFlash.activeInHierarchy)
                {
                    muzzleCounter -= Time.deltaTime;

                    if (muzzleCounter <= 0)
                    {
                        guns[selectedGun].muzzleFlash.SetActive(false);
                    }
                }

                if (!isOverheated)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Shoot();
                    }

                    if (Input.GetMouseButton(0) && guns[selectedGun].isAutomatic)
                    {
                        shotCounter -= Time.deltaTime;

                        if (shotCounter <= 0)
                        {
                            Shoot();
                        }
                    }

                    heatCounter -= coolRate * Time.deltaTime;
                }
                else
                {
                    heatCounter -= overheatCoolRate * Time.deltaTime;

                    if (heatCounter <= 0)
                    {
                        isOverheated = false;
                        UIController.instance.getOverheatedText().gameObject.SetActive(false);
                    }
                }

                if (heatCounter < 0)
                {
                    heatCounter = 0f;
                }

                UIController.instance.getWeaponTempSlider().value = heatCounter;
                UIController.instance.getHealthSlider().value = currentHealth;

                if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                {
                    selectedGun++;

                    if (selectedGun >= guns.Length)
                    {
                        selectedGun = 0;
                    }

                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
                else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                {
                    selectedGun--;

                    if (selectedGun < 0)
                    {
                        selectedGun = guns.Length - 1;
                    }

                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }

                for (int i = 0; i < guns.Length; i++)
                {
                    if (Input.GetKeyDown((i + 1).ToString()))
                    {
                        selectedGun = i;
                        photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                    }
                }

                animator.SetBool("grounded", isGrounded);
                animator.SetFloat("speed", moveDirection.magnitude);

                if (Input.GetMouseButton(1))
                {
                    mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, guns[selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                    gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
                }
                else
                {
                    mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                    gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                mainCamera.transform.position = viewPoint.position;
                mainCamera.transform.rotation = viewPoint.rotation;
            }
            else
            {
                mainCamera.transform.position = MatchManager.instance.mapCamPoint.position;
                mainCamera.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }
        }
    }

    void Shoot()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = mainCamera.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.tag.Equals("Player"))
            {
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName,
                    guns[selectedGun].shotDamage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject impact = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(impact, 10f);
            }
        }

        Gun gun = guns[selectedGun];

        shotCounter = gun.timeBetweenShots;

        heatCounter += gun.heatPerShot;

        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            isOverheated = true;

            UIController.instance.getOverheatedText().gameObject.SetActive(true);
        }

        guns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        guns[selectedGun].shotSound.Stop();
        guns[selectedGun].shotSound.Play();
    }

    [PunRPC]
    public void DealDamage(string damager, int damage, int actor)
    {
        TakeDamage(damager, damage, actor);
    }

    public void TakeDamage(string damager, int damage, int actor)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damage;

            if (currentHealth < 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
                MatchManager.instance.UpdateStatSend(actor, 0, 1);
            }
        }
    }

    void SwitchGun()
    {
        foreach (Gun gun in guns)
        {
            gun.gameObject.SetActive(false);
        }

        guns[selectedGun].gameObject.SetActive(true);
        guns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunIndex)
    {
        if (gunIndex < guns.Length)
        {
            selectedGun = gunIndex;
            SwitchGun();
        }
    }
}
