using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance; // Singleton simples
    private InputSystem_Actions inputActions;

    // Apenas os inputs utilizados no projeto
    public Vector2 Move { get; private set; }
    public bool Jump { get; private set; }
    public bool Sprint { get; private set; }  // Utilizado para o agachar/crouch
    public bool Interact { get; private set; } // Utilizado para acao/ferramentas/caixas
    public bool Pause { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (inputActions != null) inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    private void Update()
    {
        if (inputActions == null) return;

        // Le apenas as acoes existentes no projeto
        Move = inputActions.Player.Move.ReadValue<Vector2>();
        Jump = inputActions.Player.Jump.WasPressedThisFrame();
        Sprint = inputActions.Player.Sprint.IsPressed();
        Interact = inputActions.Player.Interact.WasPressedThisFrame();
    }
}