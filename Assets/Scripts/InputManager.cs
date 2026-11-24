using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Centraliza a leitura de entradas via Input System e alterna entre os mapas Player e UI
public class InputManager : MonoBehaviour
{
    public static InputManager Instance; // Singleton simples

    private InputSystem_Actions inputActions;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;

    private CinemachineOrbitalFollow cinemachineOrbitalFollow;

    public Vector2 Move { get; private set; }
    public bool Jump { get; private set; }
    public bool Sprint { get; private set; }
    public bool Interact { get; private set; }
    public bool Pause { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        inputActions = new InputSystem_Actions();
        playerActions = inputActions.Player;
        moveAction = playerActions.Move;
        jumpAction = playerActions.Jump;
        sprintAction = playerActions.Sprint;
        interactAction = playerActions.Interact;

        cinemachineOrbitalFollow = FindAnyObjectByType<CinemachineOrbitalFollow>();
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
        ClearPlayerInputs();
    }

    private void ClearPlayerInputs()
    {
        Move = Vector2.zero;
        Jump = false;
        Sprint = false;
        Interact = false;
    }

    public void SwitchToPlayer()
    {
        if (inputActions == null) return;

        inputActions.UI.Disable();
        playerActions.Enable();
        Cursor.lockState = CursorLockMode.Locked;

        if (cinemachineOrbitalFollow != null)
        {
            cinemachineOrbitalFollow.enabled = true;
        }
    }

    public void SwitchToUI()
    {
        if (inputActions == null) return;

        playerActions.Disable();
        inputActions.UI.Enable();
        ClearPlayerInputs();
        Cursor.lockState = CursorLockMode.None;

        if (cinemachineOrbitalFollow != null)
        {
            cinemachineOrbitalFollow.enabled = false;
        }
    }

    private void Update()
    {
        if (inputActions == null || !playerActions.enabled)
        {
            return;
        }

        Move = moveAction.ReadValue<Vector2>();
        Jump = jumpAction.WasPressedThisFrame();
        Sprint = sprintAction.IsPressed();
        Interact = interactAction.WasPressedThisFrame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (inputActions != null)
        {
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }
}