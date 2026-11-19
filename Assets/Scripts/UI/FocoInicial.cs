using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Seleciona um botão quando o painel abre (necessário para navegar com controle).
// O painel aberto por último manda: quando o popup fecha, o foco volta pro menu.
public class FocoInicial : MonoBehaviour
{
    public GameObject alvo;

    static readonly List<FocoInicial> pilha = new List<FocoInicial>();

    // painel aberto por último
    public static FocoInicial Topo { get { return pilha.Count > 0 ? pilha[pilha.Count - 1] : null; } }

    void OnEnable()
    {
        pilha.Remove(this);
        pilha.Add(this);
        StartCoroutine(SelecionarDepois());
    }

    void OnDisable() { pilha.Remove(this); }

    IEnumerator SelecionarDepois()
    {
        yield return null; // espera os botões terminarem de ativar
        Selecionar();
    }

    public void Selecionar()
    {
        var es = EventSystem.current;
        if (es == null || alvo == null || !alvo.activeInHierarchy) return;
        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(alvo);
    }

    void Update()
    {
        if (pilha.Count == 0 || pilha[pilha.Count - 1] != this) return;
        var es = EventSystem.current;
        if (es == null) return;
        var atual = es.currentSelectedGameObject;
        if (atual == null || !atual.activeInHierarchy) Selecionar();
    }
}
