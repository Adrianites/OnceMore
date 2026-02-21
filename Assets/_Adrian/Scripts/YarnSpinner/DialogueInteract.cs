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

    public bool inDialogue = false;

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
        if (playerInRange && !isCurrentlyTalking && !dialogueRunner.IsDialogueRunning)
        {
            if (Keyboard.current[interactionKey].wasPressedThisFrame)
            {
                StartDialogue();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
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
            
            
            Debug.Log($"Player left range of {gameObject.name}");
        }
    }

    private void StartDialogue()
    {
        if (dialogueRunner == null) return;
        
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
        StartCoroutine(IWaitAfterDialogueEnd());
    }

    IEnumerator IWaitAfterDialogueEnd()
    {
        yield return new WaitForSeconds(0.5f);
        isCurrentlyTalking = false;
        inDialogue = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
