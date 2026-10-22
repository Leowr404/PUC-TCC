using Unity.Cinemachine;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance; // Singleton simples
    private InputSystem_Actions inputActions;
    CinemachineOrbitalFollow cinemachineOrbitalFollow;

    public Vector2 Move { get; private set; }
    public bool Attack { get; private set; }
    public bool Attack2 { get; private set; }
    public bool Jump { get; private set; }
    public bool Sprint { get; private set; }
    public bool LockOn { get; private set; }
    public bool Pause { get; private set; }
    public bool Interact { get; private set; }
    public bool AdvanceDialogue { get; private set; }
    public bool OpenInventory { get; private set; }
    public bool CloseInventory { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        inputActions = new InputSystem_Actions();
        cinemachineOrbitalFollow = FindAnyObjectByType<CinemachineOrbitalFollow>();
    }

    private void Start()
    {

    }

    private void OnEnable()
    {
        if (inputActions != null) inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    public void SwitchToPlayer()
    {
        inputActions.UI.Disable();
        inputActions.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        if (cinemachineOrbitalFollow != null) cinemachineOrbitalFollow.enabled = true;
    }

    public void SwitchToUI()
    {
        inputActions.Player.Disable();
        inputActions.UI.Enable();
        Cursor.lockState = CursorLockMode.None;
        if (cinemachineOrbitalFollow != null) cinemachineOrbitalFollow.enabled = false;
    }

    private void Update()
    {
        if (inputActions == null) return;

        // Lê valores crus do Input System
        Move = inputActions.Player.Move.ReadValue<Vector2>();
        Jump = inputActions.Player.Jump.WasPressedThisFrame();
        Sprint = inputActions.Player.Sprint.IsPressed();
        Interact = inputActions.Player.Interact.WasPressedThisFrame();

        // Debug.Log("Move: " + Move);

        // Ações não presentes no asset atual de inputs (comentadas para evitar erros de compilação):
        // Attack = inputActions.Player.Attack.WasPressedThisFrame();
        // Attack2 = inputActions.Player.Attack2.WasPressedThisFrame();
        // LockOn = inputActions.Player.LockOn.WasPressedThisFrame();
        // OpenInventory = inputActions.Player.OpenInventory.WasPressedThisFrame();
        //Pause = inputActions.Player.Pause.WasPressedThisFrame();

        //========UI BUTTONS========//
        // AdvanceDialogue = inputActions.UI.AdvanceDialogue.WasPressedThisFrame();
        // CloseInventory = inputActions.UI.CloseInventory.WasPressedThisFrame();
    }
}