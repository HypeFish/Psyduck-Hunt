using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    [SerializeField] [Tooltip("Insert Character Controller")]
    private CharacterController controller;

    [SerializeField] [Tooltip("Insert Main Camera")]
    private Camera mainCamera;

    [SerializeField] [Tooltip("Insert Animator Controller")]
    private Animator playerAnimator;

    [SerializeField] [Tooltip("Insert Pokeball Prefab")]
    private GameObject pokeBallPf;

    [SerializeField] [Tooltip("Insert Pokeball Bone Transform")]
    private Transform pokeBallBone;

    public float speed = 2f;
    public float runSpeed = 6f;
    public float jumpHeight = 20f;

    private bool throwing;
    public float throwStrength = 10f;
    private GameObject instantiatedPokeBall;

    private Vector3 velocity;
    private readonly float gravity = -9.8f;
    private bool grounded;
    private readonly float groundCastDist = 0.05f;

    [SerializeField] private AudioClip[] grassSounds;
    [SerializeField] private AudioClip[] sandSounds;
    private AudioSource trainerAudioSource;
    private static readonly int IsThrowing = Animator.StringToHash("IsThrowing");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int Jumping = Animator.StringToHash("Jumping");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsStrafing = Animator.StringToHash("IsStrafing");
    private static readonly int IsStrafingRight = Animator.StringToHash("IsStrafingRight");

    // Start is called before the first frame update
    void Start()
    {
        trainerAudioSource = GetComponents<AudioSource>()[0];
        trainerAudioSource.volume = 0.3f;
        trainerAudioSource.spatialBlend = 1f;
    }


    // Update is called once per frame
    private void Update()
    {
        //Grab transforms
        Transform playerTransform = transform;
        Transform cameraTransform = mainCamera.transform;

        //Grounded
        grounded = Physics.Raycast(playerTransform.position, Vector3.down, groundCastDist);

        // Ground Movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movement = (playerTransform.right * x) + (playerTransform.forward * z);
        //Vector3 movement = (playerTransform.forward * z);

        //Throw
        if (Input.GetButtonDown("Fire1") && grounded)
        {
            throwing = true;
            playerAnimator.SetBool(IsThrowing, true);
            SpawnPokeballToBone();
        }

        //Apply movement
        if (!throwing)
        {
            //Regular movement and jumping
            if (Input.GetKey(KeyCode.LeftShift))
            {
                controller.Move(movement * (runSpeed * Time.deltaTime));
                playerAnimator.SetBool(IsRunning, true);
                trainerAudioSource.volume = 0.75f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                controller.Move(movement * (speed * Time.deltaTime));
                playerAnimator.SetBool(IsRunning, false);
                playerAnimator.SetBool(IsStrafing, true);
                trainerAudioSource.volume = 0.40f;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                controller.Move(movement * (speed * Time.deltaTime));
                playerAnimator.SetBool(IsRunning, false);
                playerAnimator.SetBool(IsStrafingRight, true);
                trainerAudioSource.volume = 0.40f;
            }
            else 
            {
                controller.Move(movement * (speed * Time.deltaTime));
                playerAnimator.SetBool(IsRunning, false);
                playerAnimator.SetBool(IsStrafing, false);
                playerAnimator.SetBool(IsStrafingRight, false);
                trainerAudioSource.volume = 0.40f;
            }


            //Gravity and Jumping
            velocity.y += gravity * Time.deltaTime;
            if (Input.GetButtonDown("Jump") && grounded)
            {
                velocity.y = MathF.Sqrt(jumpHeight);
            }

            controller.Move(velocity * Time.deltaTime);
            playerAnimator.SetBool(Jumping, !grounded);
        }
        
        playerAnimator.SetBool(IsWalking, movement.magnitude > 0);


        //Rotate alongside camera
        playerTransform.rotation = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y, Vector3.up);
        // playerTransform.rotation = Quaternion.AngleAxis(playerTransform.rotation.eulerAngles.y + (x*2), 
        //     Vector3.up);
    }

    public void ReleasePokeball()
    {
        if (instantiatedPokeBall != null)
        {
            instantiatedPokeBall.transform.parent = null;
            instantiatedPokeBall.GetComponent<SphereCollider>().enabled = true;
            instantiatedPokeBall.GetComponent<Rigidbody>().useGravity = true;
            Transform cameraTransform = mainCamera.transform;
            Vector3 throwAdjustment = new Vector3(0f, 0.5f, 0f);
            Vector3 throwVector = (cameraTransform.forward + throwAdjustment) * throwStrength;
            instantiatedPokeBall.GetComponent<Rigidbody>().AddForce(throwVector, ForceMode.Impulse);
            instantiatedPokeBall = null;
        }
    }

    public void ThrowEnded()
    {
        throwing = false;
        playerAnimator.SetBool(IsThrowing, false);
    }

    private void SpawnPokeballToBone()
    {
        if (instantiatedPokeBall == null)
        {
            instantiatedPokeBall = Instantiate(pokeBallPf, pokeBallBone, false);
        }
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private float[] GetTextureMix(Vector3 position, Terrain terrain)
    {
        Vector3 terrainPosition = terrain.transform.position;
        TerrainData terrainData = terrain.terrainData;

        //Position of player in relation to terrain alphamap
        int mapPositionX = Mathf.RoundToInt((position.x - terrainPosition.x) /
            terrainData.size.x * terrainData.alphamapWidth);
        int mapPositionZ = Mathf.RoundToInt((position.z - terrainPosition.z) /
            terrainData.size.z * terrainData.alphamapHeight);

        float[,,] splatMapData = terrainData.GetAlphamaps(mapPositionX, mapPositionZ, 1, 1);
        float[] cellMix = new float[splatMapData.GetUpperBound(2) + 1];
        for (int i = 0; i < cellMix.Length; i++)
        {
            cellMix[i] = splatMapData[0, 0, i];
        }

        return cellMix;
    }

    private string FootStepLayerName(Vector3 pokemonPostion, Terrain terrain)
    {
        float[] cellMix = GetTextureMix(pokemonPostion, terrain);
        float strongestTexture = 0f;
        int maxIndex = 0;

        for (int i = 0; i < cellMix.Length; i++)
        {
            if (cellMix[i] > strongestTexture)
            {
                strongestTexture = cellMix[i];
                maxIndex = i;
            }
        }

        return terrain.terrainData.terrainLayers[maxIndex].name;
    }

    public void footStep()
    {
        trainerAudioSource.clip = grassSounds[Random.Range(0, grassSounds.Length)];
        if (FootStepLayerName(transform.position, Terrain.activeTerrain) == "TL_Sand")
        {
            trainerAudioSource.clip = sandSounds[Random.Range(0, sandSounds.Length)];
        }

        trainerAudioSource.Play();
    }
}