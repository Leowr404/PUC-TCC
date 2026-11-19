using DG.Tweening;
using UnityEngine;

// Animação de entrada dos painéis: fade + deslize (ou "pulo" de escala no popup e no pause).
// Funciona com o jogo pausado (tempo não escalado).
public class TransicaoPainel : MonoBehaviour
{
    public RectTransform alvo;
    public bool pulo;
    public float duracao = 0.25f;

    CanvasGroup grupo;
    Vector2 posOriginal;
    bool guardado;

    void OnEnable()
    {
        if (grupo == null)
        {
            grupo = GetComponent<CanvasGroup>();
            if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        }
        grupo.DOKill();
        grupo.alpha = 0f;
        grupo.DOFade(1f, duracao).SetUpdate(true).SetLink(gameObject);

        if (alvo == null) return;
        if (!guardado) { posOriginal = alvo.anchoredPosition; guardado = true; }
        alvo.DOKill();
        if (pulo)
        {
            alvo.localScale = Vector3.one * 0.9f;
            alvo.DOScale(1f, duracao).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }
        else
        {
            alvo.anchoredPosition = posOriginal + new Vector2(0f, -24f);
            alvo.DOAnchorPos(posOriginal, duracao).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
    }

    void OnDisable()
    {
        if (grupo != null) { grupo.DOKill(); grupo.alpha = 1f; }
        if (alvo != null)
        {
            alvo.DOKill();
            if (guardado) alvo.anchoredPosition = posOriginal;
            alvo.localScale = Vector3.one;
        }
    }
}
