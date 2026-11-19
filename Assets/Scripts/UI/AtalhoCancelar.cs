using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// B no controle (e Esc, se ligado) aciona o botão de voltar do painel que está por cima.
public class AtalhoCancelar : MonoBehaviour
{
    public Button botao;
    [Tooltip("Desligado nos painéis do pause, onde o Esc já é do PauseManager.")]
    public bool usarEsc = true;

    FocoInicial foco;
    static int ultimoFrame = -1; // um aperto aciona só um painel

    void Awake() { foco = GetComponent<FocoInicial>(); }

    void Update()
    {
        if (botao == null || !botao.IsInteractable() || !botao.gameObject.activeInHierarchy) return;
        if (foco != null && FocoInicial.Topo != foco) return;
        if (Time.frameCount == ultimoFrame || !Apertou()) return;

        // com uma lista de dropdown aberta, o B só fecha a lista
        foreach (var d in GetComponentsInChildren<TMP_Dropdown>())
            if (d.IsExpanded) return;

        ultimoFrame = Time.frameCount;
        botao.onClick.Invoke();
    }

    bool Apertou()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (var g in Gamepad.all)
            if (g.buttonEast.wasPressedThisFrame) return true;
        return usarEsc && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.JoystickButton1) || (usarEsc && Input.GetKeyDown(KeyCode.Escape));
#endif
    }
}
