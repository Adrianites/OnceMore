using UnityEngine;
using Unity.Cinemachine;
using Yarn.Unity;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravityMultiplier = 1f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera mainCam;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    public CinemachineImpulseSource impulseSource;

    private DialogueRunner dialogueRunner;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1);

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update()
    {
        bool inDialogueActive = false;

        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
        {
            inDialogueActive = true;
        }
        else
        {
            var interacts = FindObjectsByType<DialogueInteract>(FindObjectsSortMode.None);
            for (int i = 0; i < interacts.Length; i++)
            {
                if (interacts[i] != null && interacts[i].inDialogue)
                {
                    inDialogueActive = true;
                    break;
                }
            }
        }

        if (!inDialogueActive)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    public void FixedUpdate()
    {
        CameraShakeManager.instance.CameraShake(impulseSource); 
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleJumping()
    {
        if(characterController.isGrounded)
        {
            currentMovement.y = -0.5f;
            
            if(playerInputHandler.JumpTriggered)
            {
                currentMovement.y = jumpForce;
            }
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        HandleJumping();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCam.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }
}