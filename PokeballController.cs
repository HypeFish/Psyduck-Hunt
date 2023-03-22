using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

public class PokeballController : MonoBehaviour
{
    private bool didOnce;
    private GameObject pokemon;
    private GameObject tree;
    private GameObject terrain;
    private int animationStage;
    private Transform trainer;
    private bool escaped;
    private bool checkForEscape = true;
    private LevelManager levelManager;

    public Animator pokeballAnimator;

    // ReSharper disable once IdentifierTypo
    public ParticleSystem pokeflashPF;
    public GameObject fracture;

    private AudioSource ballAudioSource;
    [SerializeField] private AudioClip clip_hit;
    [SerializeField] private AudioClip clip_Collision;
    [SerializeField] private AudioClip clip_Wiggle;
    [SerializeField] private AudioClip clip_Success;
    [SerializeField] private AudioClip clip_Escape;

    private bool disableCollisionSounds;


    private bool shutUp;
    private static readonly int State = Animator.StringToHash("State");


    // Start is called before the first frame update
    void Start()
    {
        pokeballAnimator.speed = 0;
        trainer = GameObject.Find("Trainer").transform.Find("CameraFocus");
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
        ballAudioSource = GetComponents<AudioSource>()[0];
        ballAudioSource.volume = 0.75f;
        ballAudioSource.spatialBlend = 1f;
        ballAudioSource.maxDistance = 5f;
    }

    private void FixedUpdate()
    {
        Rigidbody component = GetComponent<Rigidbody>();

        if (pokemon != null)
        {
            switch (animationStage)
            {
                case 0:
                    component.AddForce(Vector3.up * 2, ForceMode.Impulse);
                    animationStage = 1;
                    break;

                case 1:
                    if (component.velocity.y < 0)
                    {
                        animationStage = 2;
                    }

                    break;

                case 2:
                    component.isKinematic = true; // Hang in thin air
                    Quaternion rotationTowardsPokemon =
                        Quaternion.LookRotation(pokemon.transform.position - transform.position);
                    transform.rotation = Quaternion.Lerp(transform.rotation, rotationTowardsPokemon,
                        Time.fixedDeltaTime * 3); //Rotate towards
                    pokeballAnimator.speed = 4; // speed up when opening
                    if (!didOnce)
                    {
                        Instantiate(pokeflashPF, pokemon.transform.position, Quaternion.identity);
                        didOnce = true;
                    }

                    pokemon.SetActive(false);

                    if (pokeballAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f &&
                        pokeballAnimator.GetCurrentAnimatorStateInfo(0).IsName("Armature__Open"))
                    {
                        animationStage = 3;
                    }

                    break;

                case 3:
                    pokeballAnimator.SetInteger(State, 1);
                    if (pokeballAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f &&
                        pokeballAnimator.GetCurrentAnimatorStateInfo(0).IsName("Armature_Close"))
                    {
                        animationStage = 4;
                        terrain = null;
                    }

                    break;

                case 4:
                    transform.LookAt(trainer, Vector3.up);
                    component.isKinematic = false;
                    if (terrain != null)
                    {
                        animationStage = 5;
                    }

                    break;

                case 5:
                    component.isKinematic = true;
                    pokeballAnimator.SetInteger(State, 2);
                    pokeballAnimator.speed = 1.5f;

                    if (checkForEscape)
                    {
                        int r = Random.Range(1, 10);
                        if (r == 1)
                        {
                            escaped = true;
                            pokeballAnimator.speed = 0;
                            didOnce = false;
                            animationStage = 6;
                        }

                        StartCoroutine(WaitCheckForSeconds(1));
                        checkForEscape = false;
                    }

                    if (pokeballAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 3.0f &&
                        pokeballAnimator.GetCurrentAnimatorStateInfo(0).IsName("Armature_Wiggle"))
                    {
                        pokeballAnimator.speed = 0;
                        didOnce = false;
                        animationStage = 6;
                    }

                    break;

                case 6:
                    if (escaped)
                    {
                        if (!didOnce)
                        {
                            Instantiate(pokeflashPF, pokemon.transform.position, Quaternion.identity);
                            ballAudioSource.clip = clip_Escape;
                            ballAudioSource.Play();
                            didOnce = true;
                        }

                        Destroy(gameObject);
                        pokemon.SetActive(true);
                    }
                    else
                    {
                        if (!didOnce)
                        {
                            ballAudioSource.clip = clip_Success;
                            ballAudioSource.Play();
                            didOnce = true;
                        }
                        levelManager.RemovePokemon(pokemon, true);
                    }

                    disableCollisionSounds = false;
                    break;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag($"Pokemon") && pokemon == null)
        {
            pokemon = collision.gameObject;
            ballAudioSource.clip = clip_hit;
            ballAudioSource.Play();
            disableCollisionSounds = true;
        }

        if (collision.gameObject.name == "Terrain")
        {
            terrain = collision.gameObject;
            if (!disableCollisionSounds)
            {
                ballAudioSource.clip = clip_Collision;
                ballAudioSource.Play();
            }

        }
    }

    public void Wiggle()
    {
        ballAudioSource.clip = clip_Wiggle;
        ballAudioSource.Play();
    }
    

    IEnumerator WaitCheckForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        checkForEscape = true;
    }
}