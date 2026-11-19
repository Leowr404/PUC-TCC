using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Reproduz os estados do design (normal, selecionado, pressionado, desativado) com DOTween.
// Move e gira só o filho "Visual", então não briga com o Vertical Layout Group.
// Passar o mouse por cima seleciona o botão, igual a navegar com o controle.
[DisallowMultipleComponent]
public class FocoVisual : MonoBehaviour, ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Alvo")]
    public Selectable alvo;
    public RectTransform visual;
    public Graphic fundo;

    [Header("Cores do fundo")]
    public Color corNormal = Color.white;
    public Color corSelecionado = Color.white;
    public Color corPressionado = Color.white;
    public Color corDesativado = Color.gray;
    public bool aplicarDesativado = true;

    [Header("Textos (cor quando desativado)")]
    public Graphic[] textos;
    public Color textoDesativado = Color.gray;

    [Header("Movimento")]
    public Vector2 deslocSelecionado;
    public Vector2 deslocPressionado;
    public float rotSelecionado;
    public float duracao = 0.12f;

    [Header("Extras")]
    public Shadow sombra;
    public bool sombraSoSelecionado;
    public GameObject[] mostrarQuandoSelecionado;
    public GameObject[] mostrarQuandoDesativado;

    bool selecionado, pressionado, ultimoAtivo = true, iniciado;
    Vector2 posOriginal;
    Color[] coresTexto;

    void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;
        if (alvo == null) alvo = GetComponent<Selectable>();
        if (visual != null) posOriginal = visual.anchoredPosition;
        if (textos != null)
        {
            coresTexto = new Color[textos.Length];
            for (int i = 0; i < textos.Length; i++)
                coresTexto[i] = textos[i] != null ? textos[i].color : Color.white;
        }
    }

    void OnEnable()
    {
        Iniciar();
        pressionado = false;
        selecionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Aplicar(false); // ao abrir o painel, já aparece no estado certo
    }

    void OnDisable()
    {
        if (visual != null) visual.DOKill();
        if (fundo != null) fundo.DOKill();
    }

    void Update()
    {
        // pega mudanças de interactable feitas por script (ex.: fases liberadas)
        bool ativo = alvo == null || alvo.IsInteractable();
        if (ativo != ultimoAtivo) Aplicar(true);
    }

    public void OnSelect(BaseEventData e) { selecionado = true; Aplicar(true); }
    public void OnDeselect(BaseEventData e) { selecionado = false; pressionado = false; Aplicar(true); }

    public void OnPointerEnter(PointerEventData e)
    {
        if (alvo != null && alvo.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerDown(PointerEventData e) { pressionado = true; Aplicar(true); }
    public void OnPointerUp(PointerEventData e) { pressionado = false; Aplicar(true); }

    public void Aplicar(bool animar)
    {
        Iniciar();
        bool ativo = alvo == null || alvo.IsInteractable();
        ultimoAtivo = ativo;
        bool sel = ativo && selecionado;
        bool press = sel && pressionado;

        if (fundo != null && (ativo || aplicarDesativado))
        {
            Color cor = !ativo ? corDesativado : press ? corPressionado : sel ? corSelecionado : corNormal;
            fundo.DOKill();
            if (animar) fundo.DOColor(cor, duracao).SetUpdate(true).SetLink(gameObject);
            else fundo.color = cor;
        }

        if (aplicarDesativado && textos != null && coresTexto != null)
            for (int i = 0; i < textos.Length; i++)
                if (textos[i] != null) textos[i].color = ativo ? coresTexto[i] : textoDesativado;

        if (visual != null)
        {
            Vector2 pos = posOriginal + (press ? deslocPressionado : sel ? deslocSelecionado : Vector2.zero);
            Vector3 rot = new Vector3(0f, 0f, sel ? rotSelecionado : 0f);
            visual.DOKill();
            if (animar)
            {
                visual.DOAnchorPos(pos, duracao).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
                visual.DOLocalRotate(rot, duracao).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
            else
            {
                visual.anchoredPosition = pos;
                visual.localEulerAngles = rot;
            }
        }

        if (sombra != null) sombra.enabled = ativo && !press && (!sombraSoSelecionado || sel);

        Mostrar(mostrarQuandoSelecionado, sel, animar);
        Mostrar(mostrarQuandoDesativado, !ativo, false);
    }

    static void Mostrar(GameObject[] lista, bool valor, bool animar)
    {
        if (lista == null) return;
        foreach (var g in lista)
        {
            if (g == null || g.activeSelf == valor) continue;
            g.SetActive(valor);
            var t = g.transform;
            t.DOKill();
            if (valor && animar)
            {
                t.localScale = Vector3.zero;
                t.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(g);
            }
            else t.localScale = Vector3.one;
        }
    }
}
