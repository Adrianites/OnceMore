using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int flowersCollected = 0;
    public int totalFlowers;
    bool collectedAllFlowers = false;
    public bool isNightTime = false;
    public int currentDay = 1;
    public Transform[] flowerSpawns;
    public GameObject flowerPrefab;
    public LightingManager lightingManager;

    [Header("Day 1")]
    public GameObject[] dialogueDay1;

    [Header("Day 2")]
    public GameObject[] dialogueDay2;

    [Header("Day 3")]
    public GameObject[] dialogueDay3;
    public GameObject flowerAiPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Start()
    {
        totalFlowers = flowerSpawns.Length;
        SpawnFlowers();

        DayOneSetup();
    }

    public void CollectFlower()
    {
        flowersCollected++;
    }

    public void FixedUpdate()
    {
        if (flowersCollected >= totalFlowers)
        {
            if(!collectedAllFlowers)
            {
                collectedAllFlowers = true;
                lightingManager.TriggerNightTransition();
                isNightTime = true;
                Debug.Log("All flowers collected! You win!");
            }
        }
    }

    public void ResetGame()
    {
        if(currentDay <= 3)
        {
            currentDay++;
            flowersCollected = 0;
            totalFlowers = flowerSpawns.Length;
            collectedAllFlowers = false;
            isNightTime = false;
            lightingManager.TriggerDayTransition();
            DaySwitch();
            SpawnFlowers();
        }
        else
        {
            currentDay = 1;
            Debug.Log("Game Over! You've completed all days!");
        }
        
    }

    public void SpawnFlowers()
    {
        if (currentDay == 3)
        {
            foreach (Transform spawnPoint in flowerSpawns)
            {
                Instantiate(flowerAiPrefab, spawnPoint.position, Quaternion.identity);
            }
        }
        else
        {
            foreach (Transform spawnPoint in flowerSpawns)
            {
                Instantiate(flowerPrefab, spawnPoint.position, Quaternion.identity);
            }
        }
    }

    public void DaySwitch()
    {
        switch (currentDay)
        {
            case 1:
                DayOneSetup();
                break;
            case 2:
                DayTwoSetup();
                break;
            case 3:
                DayThreeSetup();
                break;
            default:
                DayOneSetup();
                break;
        }
    }

    private void ResetAllDialogues()
    {
        foreach (GameObject dialogue in dialogueDay1)
        {
            DialogueInteract interact = dialogue.GetComponent<DialogueInteract>();
            if (interact != null) interact.ResetInteraction();
            dialogue.SetActive(false);
        }

        foreach (GameObject dialogue in dialogueDay2)
        {
            DialogueInteract interact = dialogue.GetComponent<DialogueInteract>();
            if (interact != null) interact.ResetInteraction();
            dialogue.SetActive(false);
        }

        foreach (GameObject dialogue in dialogueDay3)
        {
            DialogueInteract interact = dialogue.GetComponent<DialogueInteract>();
            if (interact != null) interact.ResetInteraction();
            dialogue.SetActive(false);
        }
    }

    public void DayOneSetup()
    {
        ResetAllDialogues();
        foreach (GameObject dialogue in dialogueDay1)
        {
            dialogue.SetActive(true);
        }
    }

    public void DayTwoSetup()
    {
        ResetAllDialogues();
        foreach (GameObject dialogue in dialogueDay2)
        {
            dialogue.SetActive(true);
        }
    }

    public void DayThreeSetup()
    {
        ResetAllDialogues();
        foreach (GameObject dialogue in dialogueDay3)
        {
            dialogue.SetActive(true);
        }
    }

}
