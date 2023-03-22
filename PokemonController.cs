using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class PokemonController : MonoBehaviour
{
    private enum State
    {
        Chill,
        Saunter,
        Flee,
        Dig,
        Shocked
    }

    [SerializeField] private State currentState;
    private bool transitionActive;

    [SerializeField] private Vector3 currentDestination;

    [SerializeField] private float runSpeed;
    private float walkingSpeed;

    private readonly float viewAngle = 0.25f;
    private readonly float viewDistance = 5;

    private GameObject trainer;
    private Animator pokemonAnimator;
    private LevelManager levelManager;

    [SerializeField] private AudioClip[] psySounds;
    [SerializeField] private AudioClip[] panicSounds;
    [SerializeField] private AudioClip[] grassSounds;
    [SerializeField] private AudioClip[] sandSounds;
    private AudioSource pokemonAS1;
    private AudioSource pokemonAS2;

    private bool shutUp;
    private static readonly int Saunter = Animator.StringToHash("Saunter");
    private static readonly int Flee = Animator.StringToHash("Flee");
    private static readonly int Dig = Animator.StringToHash("Dig");
    private static readonly int Shocked = Animator.StringToHash("Shocked");


    // Start is called before the first frame update
    void Start()
    {
        trainer = GameObject.Find("Trainer");
        walkingSpeed = GetComponent<NavMeshAgent>().speed;
        pokemonAnimator = GetComponent<Animator>();
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
        SwitchToState(State.Chill);

        pokemonAS1 = GetComponents<AudioSource>()[0];
        pokemonAS1.volume = 1f;
        pokemonAS1.spatialBlend = 1f;
        pokemonAS1.maxDistance = 5f;

        pokemonAS2 = GetComponents<AudioSource>()[1];
        pokemonAS2.volume = 0.55f;
        pokemonAS2.spatialBlend = 1f;
        pokemonAS2.maxDistance = 5f;

        shutUp = true;
        Invoke(nameof(resetShutUp), Random.Range(5f, 20f));
    }

    void resetShutUp()
    {
        shutUp = false;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case State.Chill:
                if (transitionActive)
                {
                    currentDestination = transform.position;
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    Invoke(nameof(SwitchToSaunter), Random.Range(5.0f, 6.0f));
                    UpdatePokemonAnimtor(false, false, false, false);
                    GetComponent<NavMeshAgent>().speed = 0f;
                    transitionActive = false;
                }

                if (InView(trainer, viewAngle, viewDistance))
                {
                    SwitchToState(State.Shocked);
                }

                PlaySound(State.Chill);
                break;
            case State.Saunter:
                if (transitionActive)
                {
                    currentDestination = ValidDestination(false);
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    UpdatePokemonAnimtor(true, false, false, false);
                    GetComponent<NavMeshAgent>().speed = walkingSpeed;
                    transitionActive = false;
                }

                if ((transform.position - currentDestination).magnitude < 2.5f)
                {
                    transitionActive = false;
                }

                if (InView(trainer, viewAngle, viewDistance))
                {
                    SwitchToState(State.Shocked);
                }

                PlaySound(State.Saunter);
                break;

            case State.Flee:
                PlaySound(State.Flee);

                if (transitionActive)
                {
                    CancelInvoke(nameof(SwitchToSaunter));
                    Invoke(nameof(CheckForDig), 10f);
                    currentDestination = ValidDestination(true);
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    GetComponent<NavMeshAgent>().speed = runSpeed;
                    UpdatePokemonAnimtor(false, true, false, false);
                    transitionActive = false;
                }

                if ((transform.position - currentDestination).magnitude < 2.5f)
                {
                    CancelInvoke(nameof(CheckForDig));
                    CheckForDig();
                }

                break;
            case State.Dig:
                if (transitionActive)
                {
                    currentDestination = transform.position;
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    GetComponent<NavMeshAgent>().speed = 0f;
                    UpdatePokemonAnimtor(false, false, true, false);
                    transitionActive = false;
                }

                break;

            case State.Shocked:
                if (transitionActive)
                {
                    currentDestination = transform.position;
                    CancelInvoke(nameof(SwitchToSaunter));
                    Invoke(nameof(CheckForDig), 10f);
                    GetComponent<NavMeshAgent>().speed = 0f;
                    UpdatePokemonAnimtor(false, false, false, true);
                    transitionActive = false;
                }

                break;
        }
    }

    public void DigCompleted()
    {
        levelManager.RemovePokemon(gameObject, false);
    }
    
    private void OnDisable()
    {
        CancelInvoke(nameof(SwitchToSaunter));
        CancelInvoke(nameof(CheckForDig));
        SwitchToState(State.Shocked);
    }

    void SwitchToState(State newState)
    {
        transitionActive = true;
        currentState = newState;
    }

    Vector3 ValidDestination(bool avoidTrainer)
    {
        float[,] boundaries = { { 55f, 171f }, { 97f, 194f } };
        float x = Random.Range(boundaries[0, 0], boundaries[0, 1]);
        float z = Random.Range(boundaries[1, 0], boundaries[1, 1]);
        if (avoidTrainer)
        {
            var position = trainer.transform.position;
            x = position.x - boundaries[0, 0] >= boundaries[0, 1] - position.x ? boundaries[0, 0] : boundaries[0, 1];
            var position1 = trainer.transform.position;
            z = position1.z - boundaries[1, 0] >= boundaries[1, 0] - position1.z ? boundaries[1, 0] : boundaries[1, 1];
        }

        Vector3 destination = new Vector3(x, Terrain.activeTerrain.SampleHeight(
            new Vector3(x, 0.0f, z)), z);
        return destination;
    }

    void SwitchToSaunter()
    {
        SwitchToState(State.Saunter);
    }

    public void ShockEnd()
    {
        SwitchToState(State.Flee);
    }

    void CheckForDig()
    {
        SwitchToState((transform.position - trainer.transform.position).magnitude > 25f ? State.Chill : State.Dig);
    }

    void UpdatePokemonAnimtor(bool saunter, bool flee, bool dig, bool shock)
    {
        pokemonAnimator.SetBool(Saunter, saunter);
        pokemonAnimator.SetBool(Flee, flee);
        pokemonAnimator.SetBool(Dig, dig);
        pokemonAnimator.SetBool(Shocked, shock);
    }

    void PlaySound(State currentStateNowState)
    {
        if (currentStateNowState == State.Chill || currentStateNowState == State.Saunter)
        {
            pokemonAS1.loop = false;
            if (!shutUp)
            {
                if (Random.Range(1, 10) == 1)
                {
                    pokemonAS1.clip = psySounds[Random.Range(0, psySounds.Length)];
                    pokemonAS1.Play();
                    shutUp = true;
                    Invoke(nameof(resetShutUp), Random.Range(5f, 20f));
                }
            }
        }

        if (currentStateNowState == State.Flee)
        {
            if (transitionActive)
            {
                pokemonAS1.clip = panicSounds[Random.Range(0, panicSounds.Length)];
                pokemonAS1.loop = true;
                pokemonAS1.Play();
            }
        }
    }

    bool InView(GameObject target, float viewingAngle, float viewingDistance)
    {
        var transform1 = transform;
        var position = transform1.position;
        var position1 = target.transform.position;
        
        float dotproduct = Vector3.Dot(transform1.forward, Vector3.Normalize(
            position1 - position));
        float view = 1.0f - viewingAngle;
        float distance = (position - position1).magnitude;
        return dotproduct >= view && distance < viewingDistance;
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
        pokemonAS2.clip = grassSounds[Random.Range(0, grassSounds.Length)];
        if (FootStepLayerName(transform.position, Terrain.activeTerrain) == "TL_Sand")
        {
            pokemonAS2.clip = sandSounds[Random.Range(0, sandSounds.Length)];
        }

        pokemonAS2.Play();
    }
}