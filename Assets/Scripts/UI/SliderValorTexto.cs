using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mostra o valor do slider no texto ao lado ("80", "5,0").
[RequireComponent(typeof(Slider))]
public class SliderValorTexto : MonoBehaviour
{
    public TMP_Text texto;
    public float escala = 100f;
    public string formato = "0";

    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(Atualizar);
    }

    void OnEnable()
    {
        if (slider == null) slider = GetComponent<Slider>();
        Atualizar(slider.value);
    }

    void Atualizar(float valor)
    {
        if (texto == null) return;
        texto.text = (valor * escala).ToString(formato, CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
