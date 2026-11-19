using TMPro;
using UnityEngine;

public class ColorButtonController : MonoBehaviour
{
    [Header("Identificacao")]
    public int buttonIndex = 0; // Índice de 0 a 2 para cada botão da parede
    public ColorPuzzleManager puzzleManager;

    [Header("Configuracao Visual")]
    public Renderer buttonRenderer;
    private Material buttonMaterial;

    [Header("Configuracao de Cooldown")]
    [Tooltip("Tempo em segundos que o botão espera antes de aceitar outra mudança de cor pelo laser")]
    public float colorChangeCooldown = 0.5f;
    private float lastColorChangeTime = -999f;

    [Header("Modo Daltonico")]
    [Tooltip("Opcional: se ficar vazio, um texto com o simbolo e criado sozinho na frente do botao")]
    public TextMeshPro simboloTexto;
    public Color corSimbolo = Color.black;
    private ColorPuzzleManager.ColorOption corAtual = ColorPuzzleManager.ColorOption.White;

    void Start()
    {
        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();

        if (buttonRenderer != null)
            buttonMaterial = buttonRenderer.material;

        if (simboloTexto == null) CriarSimbolo();
        Configuracoes.Mudou += AtualizarSimbolo;
        AtualizarSimbolo();
    }

    void OnDestroy() { Configuracoes.Mudou -= AtualizarSimbolo; }

    // Cria um texto 3D na frente do botao (lado da camera) do tamanho do botao
    void CriarSimbolo()
    {
        if (buttonRenderer == null) return;
        Bounds b = buttonRenderer.bounds;

        var go = new GameObject("SimboloDaltonico");
        go.transform.SetParent(transform, true);
        go.transform.position = b.center + new Vector3(0f, 0f, -(b.extents.z + 0.02f));
        go.transform.rotation = Quaternion.identity;

        simboloTexto = go.AddComponent<TextMeshPro>();
        simboloTexto.alignment = TextAlignmentOptions.Center;
        simboloTexto.rectTransform.sizeDelta = new Vector2(b.size.x, b.size.y) * 0.8f;
        simboloTexto.enableAutoSizing = true;
        simboloTexto.fontSizeMin = 0.1f;
        simboloTexto.fontSizeMax = 100f;
        simboloTexto.color = corSimbolo;
        simboloTexto.text = "";
    }

    // Um simbolo por cor; branco nao tem simbolo
    static string Simbolo(ColorPuzzleManager.ColorOption cor)
    {
        switch (cor)
        {
            case ColorPuzzleManager.ColorOption.Blue: return "\u25CF";   // circulo
            case ColorPuzzleManager.ColorOption.Red: return "\u25B2";    // triangulo
            case ColorPuzzleManager.ColorOption.Yellow: return "\u25A0"; // quadrado
            case ColorPuzzleManager.ColorOption.Green: return "\u2666";  // losango
            default: return "";
        }
    }

    void AtualizarSimbolo()
    {
        if (simboloTexto == null) return;
        simboloTexto.text = Simbolo(corAtual);
        simboloTexto.gameObject.SetActive(Configuracoes.Daltonico);
    }

    // Altera a cor visual do botão com base no Enum
    public void SetColor(ColorPuzzleManager.ColorOption colorOption)
    {
        corAtual = colorOption;
        AtualizarSimbolo();

        if (buttonMaterial == null) return;

        switch (colorOption)
        {
            case ColorPuzzleManager.ColorOption.White:
                buttonMaterial.color = Color.white;
                break;
            case ColorPuzzleManager.ColorOption.Blue:
                buttonMaterial.color = Color.blue;
                break;
            case ColorPuzzleManager.ColorOption.Red:
                buttonMaterial.color = Color.red;
                break;
            case ColorPuzzleManager.ColorOption.Yellow:
                buttonMaterial.color = Color.yellow;
                break;
            case ColorPuzzleManager.ColorOption.Green:
                buttonMaterial.color = Color.green;
                break;
        }
    }

    // Método chamado pelo seu LaserPointer enquanto o raio estiver colidindo
    public void ActivateByLaser()
    {
        // Verifica se já passou o tempo necessário desde a última troca de cor
        if (Time.time - lastColorChangeTime >= colorChangeCooldown)
        {
            lastColorChangeTime = Time.time;

            if (puzzleManager != null)
            {
                puzzleManager.CycleButtonColor(buttonIndex);
            }
        }
    }
}