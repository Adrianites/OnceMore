using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;
using System.Collections;

public class DialogueInteract : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("The Dialogue Runner in the scene")]
    [SerializeField] private DialogueRunner dialogueRunner;
    
    [Tooltip("The Yarn node to start when talking to this NPC")]
    [SerializeField] private string dialogueNode = "Start";

    [Header("Input Settings")]
    [Tooltip("The key to press to start dialogue")]
    [SerializeField] private Key interactionKey = Key.Enter;
    public bool touchToInteract = false;
    public bool canInteractMultipleTimes = true;
    private bool _hasBeenInteractedWith = false;
    public bool inDialogue = false;
    public bool DespawnSomething = false;
    public GameObject thingToDespawn;

    [Header("Character Rotation")]
    [SerializeField] private bool enableCharacterRotation = false;
    [SerializeField] private Transform npcTransform;
    [SerializeField] private float characterRotationDuration = 0.25f;
    private bool _hasRotatedToCharacter = false;
    private Transform playerTransform;

    // State tracking
    private bool playerInRange = false;
    private bool isCurrentlyTalking = false;
    private PlayerInput playerInput;

    private void Start()
    {

        // Validate references
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            if (dialogueRunner == null)
            {
                Debug.LogError($"NPCInteraction on {gameObject.name}: No DialogueRunner found in scene!");
            }
        }

        // Find PlayerInput component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            playerTransform = player.transform;
        }

        // Subscribe to dialogue complete event to know when conversation ends
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        }
    }

    private void Update()
    {
        // Check for interaction input when player is in range
        // Only allow starting dialogue if not already talking and no other dialogue is running
        if (!touchToInteract && playerInRange && !isCurrentlyTalking && !dialogueRunner.IsDialogueRunning)
        {
            if (Keyboard.current[interactionKey].wasPressedThisFrame)
            {
                StartDialogue();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(_hasBeenInteractedWith)
        {
            return;
        }

        if (touchToInteract && other.CompareTag("Player") && !isCurrentlyTalking && !dialogueRunner.IsDialogueRunning)
        {
            StartDialogue();
        }

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Disable jump action to prevent space bar from triggering jump
            // This is crucial because space bar is used for both jumping and dialogue progression
            if (playerInput != null)
            {
                var jumpAction = playerInput.actions.FindAction("Jump");
                if (jumpAction != null)
                {
                    jumpAction.Disable();
                }
            }
            
            Debug.Log($"Player entered range of {gameObject.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            // Re-enable jump action when player leaves the NPC's interaction range
            if (playerInput != null)
            {
                var jumpAction = playerInput.actions.FindAction("Jump");
                if (jumpAction != null)
                {
                    jumpAction.Enable();
                }
            }
            if (!canInteractMultipleTimes)
            {
                _hasBeenInteractedWith = true;
            }
            
            Debug.Log($"Player left range of {gameObject.name}");
        }
    }

    private void StartDialogue()
    {
        if (dialogueRunner == null)
        {
            return;
        }

        if (enableCharacterRotation && !_hasRotatedToCharacter && playerTransform != null && npcTransform != null)
        {
            StartCoroutine(RotateBothToFaceEachOther());
            _hasRotatedToCharacter = true;
        }

        isCurrentlyTalking = true;
        inDialogue = true;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        

        // Start the dialogue
        dialogueRunner.StartDialogue(dialogueNode);
        
        Debug.Log($"Started dialogue: {dialogueNode}");
    }

    private void OnDialogueComplete()
    {
        if(this.gameObject.activeInHierarchy)
        {
            StartCoroutine(IWaitAfterDialogueEnd());
        }
    }

    IEnumerator IWaitAfterDialogueEnd()
    {
        yield return new WaitForSeconds(0.5f);
        isCurrentlyTalking = false;
        inDialogue = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    IEnumerator RotateBothToFaceEachOther()
    {
        if (playerTransform == null || npcTransform == null)
            yield break;

        float duration = Mathf.Max(0.01f, characterRotationDuration);
        float elapsed = 0f;

        Quaternion startPlayerRot = playerTransform.rotation;
        Vector3 playerDir = npcTransform.position - playerTransform.position;
        playerDir.y = 0f;
        Quaternion targetPlayerRot = playerDir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(playerDir.normalized) : startPlayerRot;

        Quaternion startOtherRot = npcTransform.rotation;
        Vector3 otherDir = playerTransform.position - npcTransform.position;
        otherDir.y = 0f;
        Quaternion targetOtherRot = otherDir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(otherDir.normalized) : startOtherRot;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            playerTransform.rotation = Quaternion.Slerp(startPlayerRot, targetPlayerRot, t);
            npcTransform.rotation = Quaternion.Slerp(startOtherRot, targetOtherRot, t);
            yield return null;
        }

        playerTransform.rotation = targetPlayerRot;
        npcTransform.rotation = targetOtherRot;

        if(DespawnSomething && thingToDespawn != null)
        {
            thingToDespawn.SetActive(false);
        }
    }
}
