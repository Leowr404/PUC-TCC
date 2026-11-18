using Unity.Cinemachine;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance; // Singleton simples
    private InputSystem_Actions inputActions;
    CinemachineOrbitalFollow cinemachineOrbitalFollow;

    public Vector2 Move { get; private set; }
    public bool Jump { get; private set; }
    public bool Sprint { get; private set; }
    public bool Interact { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        inputActions = new InputSystem_Actions();
        cinemachineOrbitalFollow = FindAnyObjectByType<CinemachineOrbitalFollow>();
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
    }
}