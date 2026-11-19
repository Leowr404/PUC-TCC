using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Toggle estilo "chave": move a bolinha e troca as cores conforme ligado/desligado.
[RequireComponent(typeof(Toggle))]
public class ToggleSwitch : MonoBehaviour
{
    public Image fundo;
    public RectTransform alca;
    public Image alcaImagem;
    public TMP_Text rotulo;

    public Color fundoLigado = new Color32(0xFF, 0xD8, 0x4D, 0xFF);
    public Color fundoDesligado = new Color32(0x2F, 0x39, 0x48, 0xFF);
    public Color alcaLigado = new Color32(0x1A, 0x1F, 0x29, 0xFF);
    public Color alcaDesligado = new Color32(0x7A, 0x82, 0x91, 0xFF);
    public float xLigado = 54f;
    public float xDesligado = 6f;

    Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(_ => Atualizar());
    }

    void OnEnable() { Atualizar(); }

    public void Atualizar()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        bool ligado = toggle.isOn;
        if (fundo != null) fundo.color = ligado ? fundoLigado : fundoDesligado;
        if (alcaImagem != null) alcaImagem.color = ligado ? alcaLigado : alcaDesligado;
        if (alca != null)
        {
            var p = alca.anchoredPosition;
            p.x = ligado ? xLigado : xDesligado;
            alca.anchoredPosition = p;
        }
        if (rotulo != null) rotulo.text = ligado ? "LIGADO" : "DESLIGADO";
    }
}
