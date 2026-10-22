using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ao navegar com controle ou teclado, rola a lista até a opção selecionada ficar visível.
[RequireComponent(typeof(ScrollRect))]
public class RolarAteSelecionado : MonoBehaviour
{
    public float margem = 24f;
    public float velocidade = 14f;

    ScrollRect scroll;
    GameObject ultimo;
    float tempo;
    readonly Vector3[] cantosItem = new Vector3[4];
    readonly Vector3[] cantosVista = new Vector3[4];

    void Awake() { scroll = GetComponent<ScrollRect>(); }

    void LateUpdate()
    {
        var es = EventSystem.current;
        if (es == null || scroll.content == null || scroll.viewport == null) return;
        var sel = es.currentSelectedGameObject;
        if (sel == null || !sel.transform.IsChildOf(scroll.content)) { ultimo = null; return; }

        // só ajusta logo depois de trocar a seleção, para não brigar com a rodinha do mouse
        if (sel != ultimo) { ultimo = sel; tempo = 0.6f; }
        if (tempo <= 0f) return;
        tempo -= Time.unscaledDeltaTime;

        // usa a linha inteira (filho direto do Content) como referência
        Transform linha = sel.transform;
        while (linha.parent != null && linha.parent != scroll.content) linha = linha.parent;

        ((RectTransform)linha).GetWorldCorners(cantosItem);
        scroll.viewport.GetWorldCorners(cantosVista);
        float escala = scroll.content.lossyScale.y;
        if (escala <= 0f) return;
        float m = margem * escala;

        float delta = 0f;
        if (cantosItem[1].y > cantosVista[1].y - m) delta = cantosItem[1].y - (cantosVista[1].y - m);
        else if (cantosItem[0].y < cantosVista[0].y + m) delta = cantosItem[0].y - (cantosVista[0].y + m);
        if (Mathf.Abs(delta) < 0.5f * escala) return;

        var p = scroll.content.anchoredPosition;
        p.y -= delta / escala * Mathf.Clamp01(Time.unscaledDeltaTime * velocidade);
        scroll.content.anchoredPosition = p;
    }
}
