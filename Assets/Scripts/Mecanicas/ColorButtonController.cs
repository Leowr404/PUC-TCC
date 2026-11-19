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

    void Start()
    {
        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();

        if (buttonRenderer != null)
            buttonMaterial = buttonRenderer.material;
    }

    // Altera a cor visual do botão com base no Enum
    public void SetColor(ColorPuzzleManager.ColorOption colorOption)
    {
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