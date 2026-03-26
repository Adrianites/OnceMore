using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int flowersCollected = 0;
    public int totalFlowers;
    bool collectedAllFlowers = false;
    public bool isNightTime = false;
    public int currentDay = 1;
    public int cycleCount = 0;
    public Transform[] flowerSpawns;
    public GameObject flowerPrefab;
    public LightingManager lightingManager;
    public DialogueRunner dialogueRunner;
    public LookAt lookAtScript;
    public Slider flowerProgressSlider;
    public Animator GUIAnimator;
    [SerializeField] private PlayerInputHandler[] playerInputHandler;

    [Header("Day 1")]
    public GameObject[] dialogueDay1;

    [Header("Day 2")]
    public GameObject[] dialogueDay2;

    [Header("Day 3")]
    public GameObject[] dialogueDay3;
    public GameObject flowerAiPrefab;

    [Header("Room")]
    public GameObject PlayerFlower;
    public GameObject PlayerRoom;
    public GameObject pressFCanvas;
    public bool isInRoom = false;
    public bool canSwitchToRoom = false;

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
        pressFCanvas.SetActive(false);
        totalFlowers = flowerSpawns.Length;
        SpawnFlowers();

        DayOneSetup();
    }

    public void OnSwitchToRoom()
    {
        if( playerInputHandler[0].SwitchToRoomTriggered || playerInputHandler[1].SwitchToRoomTriggered)
        {
            if(isInRoom)
            {
                pressFCanvas.SetActive(false);
                PlayerFlower.SetActive(false);
                PlayerRoom.SetActive(true);
                playerInputHandler[0].enabled = false;
                playerInputHandler[1].enabled = true;
                isInRoom = false;
            }
            else
            {
                pressFCanvas.SetActive(true);
                PlayerFlower.SetActive(true);
                PlayerRoom.SetActive(false);
                playerInputHandler[0].enabled = true;
                playerInputHandler[1].enabled = false;
                isInRoom = true;
            }
        }
    }

    public void CollectFlower()
    {
        flowersCollected++;
        flowerProgressSlider.value = totalFlowers - flowersCollected;
    }

    public void FixedUpdate()
    {
        if (flowersCollected >= totalFlowers)
        {
            if(!collectedAllFlowers)
            {
                collectedAllFlowers = true;
                lightingManager.TriggerNightTransition();
                GUIAnimator.SetTrigger("TurnOff");
                isNightTime = true;
                
                switch (currentDay)
                {
                    case 1:
                        dialogueRunner.StartDialogue("NightOneDialogue");
                        break;
                    case 2:
                        dialogueRunner.StartDialogue("NightTwoDialogue");
                        break;
                    case 3:
                        dialogueRunner.StartDialogue("NightThreeDialogue");
                        break;
                    default:
                        dialogueRunner.StartDialogue("NightOneDialogue");
                        break;
                }
            }
        }

        if(cycleCount >= 1 && canSwitchToRoom)
        {
            if(playerInputHandler[0].SwitchToRoomTriggered || playerInputHandler[1].SwitchToRoomTriggered)
            {
                OnSwitchToRoom();
            }
        }

    }

    public void ResetGame()
    {
        if(currentDay <= 3)
        {
            if(currentDay != 3)
            {
                currentDay++;
            }
            else
            {
                currentDay = 1;
                cycleCount++;
                canSwitchToRoom = true;
                pressFCanvas.SetActive(true);
                dialogueRunner.StartDialogue("DayThreeEndDialogue");
                StartCoroutine(lookAtScript.RotateBothToFaceEachOther());
            }
            Debug.Log("Current Day: " + currentDay);
            flowersCollected = 0;
            totalFlowers = flowerSpawns.Length;
            collectedAllFlowers = false;
            isNightTime = false;
            lightingManager.TriggerDayTransition();
            DaySwitch();
            SpawnFlowers();

            switch (currentDay - 1)
            {
                case 1:
                    dialogueRunner.StartDialogue("DayOneEndDialogue");
                    StartCoroutine(lookAtScript.RotateBothToFaceEachOther());
                    break;
                case 2:
                    dialogueRunner.StartDialogue("DayTwoEndDialogue");
                    StartCoroutine(lookAtScript.RotateBothToFaceEachOther());
                    break;
            }
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
        GUIAnimator.SetTrigger("TurnOn");
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
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null)
        {
            dialogueRunner.VariableStorage.Clear();

            if (dialogueRunner.YarnProject != null)
            {
                dialogueRunner.VariableStorage.Program = dialogueRunner.YarnProject.Program;
            }
        }

        foreach (GameObject dialogue in dialogueDay1)
        {
            DialogueInteract interact = dialogue.GetComponent<DialogueInteract>();
            if (interact != null)
            {
                interact.ResetInteraction();
            }
            dialogue.SetActive(false);
        }

        foreach (GameObject dialogue in dialogueDay2)
        {
            DialogueInteract interact = dialogue.GetComponent<DialogueInteract>();
            if (interact != null)
            {
                interact.ResetInteraction();
            }
            dialogue.SetActive(false);
        }

        foreach (GameObject dialogue in dialogueDay3)
        {
            DialogueInteract interact = dialogue.GetComponent<DialogueInteract>();
            if (interact != null)
            {
                interact.ResetInteraction();
            }
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

        flowerProgressSlider.maxValue = totalFlowers;
        flowerProgressSlider.value = totalFlowers;
    }

    public void DayTwoSetup()
    {
        ResetAllDialogues();
        foreach (GameObject dialogue in dialogueDay2)
        {
            dialogue.SetActive(true);
        }
        flowerProgressSlider.maxValue = totalFlowers;
        flowerProgressSlider.value = totalFlowers;
    }

    public void DayThreeSetup()
    {
        ResetAllDialogues();
        foreach (GameObject dialogue in dialogueDay3)
        {
            dialogue.SetActive(true);
        }
        flowerProgressSlider.maxValue = totalFlowers;
        flowerProgressSlider.value = totalFlowers;
    }

}
