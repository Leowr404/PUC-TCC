#if UNITY_EDITOR
#pragma warning disable 0618
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// COLOQUE ESTE ARQUIVO NUMA PASTA CHAMADA "Editor" (ex.: Assets/Editor/).
// Abra pelo menu do Unity: Tools > Gerar Menu do JSON
public class GeradorMenuJSON : EditorWindow
{
    const string PASTA = "Assets/UI/Gerado";
    const string PASTA_SPR = "Assets/UI/Gerado/Sprites";

    TextAsset json;
    TMP_FontAsset bri500, bri600, bri700, bri800, mono400, mono700;

    Dictionary<string, object> estilos;
    string telaAtual;
    readonly List<Button> cartas = new List<Button>();
    readonly Dictionary<string, Sprite> cacheSprites = new Dictionary<string, Sprite>();

    static readonly string[] Resolucoes = { "1920 × 1080", "2560 × 1440", "1600 × 900", "1280 × 720" };

    [MenuItem("Tools/Gerar Menu do JSON")]
    static void Abrir() { GetWindow<GeradorMenuJSON>("Gerar Menu"); }

    void OnEnable() { ProcurarFontes(); }

    // ------------------------------------------------------------------ janela

    void OnGUI()
    {
        EditorGUILayout.LabelField("Ficha do Claude Design", EditorStyles.boldLabel);
        json = (TextAsset)EditorGUILayout.ObjectField("Ficha Unity.json", json, typeof(TextAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fontes TextMeshPro", EditorStyles.boldLabel);
        bri500 = CampoFonte("Bricolage 500 (Medium)", bri500);
        bri600 = CampoFonte("Bricolage 600 (SemiBold)", bri600);
        bri700 = CampoFonte("Bricolage 700 (Bold)", bri700);
        bri800 = CampoFonte("Bricolage 800 (ExtraBold)", bri800);
        mono400 = CampoFonte("JetBrains Mono 400 (Regular)", mono400);
        mono700 = CampoFonte("JetBrains Mono 700 (Bold)", mono700);
        if (GUILayout.Button("Procurar fontes no projeto")) ProcurarFontes();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(json == null))
        {
            if (GUILayout.Button("Gerar menu na cena", GUILayout.Height(36))) Gerar();
        }

        EditorGUILayout.HelpBox(
            "Gera na cena aberta: MenuCanvas (menu principal, levels, settings, créditos e popup) " +
            "já ligado ao MainMenuManager.\n" +
            "Gera também o prefab PauseCanvas em " + PASTA + " para colocar nas fases.\n" +
            "Fonte faltando = usa a fonte padrão do TMP.", MessageType.Info);
    }

    static TMP_FontAsset CampoFonte(string rotulo, TMP_FontAsset f)
    {
        return (TMP_FontAsset)EditorGUILayout.ObjectField(rotulo, f, typeof(TMP_FontAsset), false);
    }

    void ProcurarFontes()
    {
        var todas = AssetDatabase.FindAssets("t:TMP_FontAsset")
            .Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(f => f != null).ToList();

        Func<string, Func<string, bool>, TMP_FontAsset> achar = (familia, peso) =>
            todas.FirstOrDefault(f => { string n = Norm(f.name); return n.Contains(familia) && peso(n); });

        Func<string, bool> soBold = n => n.Contains("bold") && !n.Contains("extrabold") && !n.Contains("semibold");

        if (bri800 == null) bri800 = achar("bricolage", n => n.Contains("extrabold"));
        if (bri600 == null) bri600 = achar("bricolage", n => n.Contains("semibold"));
        if (bri500 == null) bri500 = achar("bricolage", n => n.Contains("medium"));
        if (bri700 == null) bri700 = achar("bricolage", soBold);
        if (mono700 == null) mono700 = achar("jetbrains", soBold);
        if (mono400 == null) mono400 = achar("jetbrains", n => n.Contains("regular"));
    }

    static string Norm(string s)
    {
        return s.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    }

    TMP_FontAsset Fonte(string familia, int peso, out bool substituta)
    {
        bool mono = (familia ?? "").ToLowerInvariant().Contains("mono");
        TMP_FontAsset f = mono
            ? (peso >= 600 ? mono700 : mono400)
            : (peso >= 800 ? bri800 : peso >= 700 ? bri700 : peso >= 600 ? bri600 : bri500);
        substituta = false;
        if (f == null) f = mono ? Primeira(mono700, mono400) : Primeira(bri700, bri800, bri600, bri500);
        if (f == null) { f = TMP_Settings.defaultFontAsset; substituta = true; }
        return f;
    }

    static TMP_FontAsset Primeira(params TMP_FontAsset[] lista)
    {
        foreach (var f in lista) if (f != null) return f;
        return null;
    }

    // ------------------------------------------------------------------ geração

    void Gerar()
    {
        var raiz = MiniJson.Parse(json.text.TrimStart('\uFEFF')) as Dictionary<string, object>;
        if (raiz == null) { EditorUtility.DisplayDialog("Erro", "Não consegui ler o JSON.", "OK"); return; }

        estilos = D(raiz, "estadosBotoes");
        var telas = new Dictionary<string, Dictionary<string, object>>();
        var lista = L(raiz, "telas");
        if (lista != null)
            foreach (var t in lista)
            {
                var td = t as Dictionary<string, object>;
                if (td != null && S(td, "id") != null) telas[S(td, "id")] = D(td, "raiz");
            }

        foreach (var id in new[] { "1a", "1c", "1d", "1e", "1f", "1g" })
            if (!telas.ContainsKey(id) || telas[id] == null)
            {
                EditorUtility.DisplayDialog("Erro", "A tela " + id + " não está no JSON.", "OK");
                return;
            }

        var antigo = UnityEngine.Object.FindObjectOfType<MainMenuManager>();
        if (antigo != null && EditorUtility.DisplayDialog("Menu antigo",
                "Já existe um MainMenuManager em \"" + antigo.gameObject.name + "\". Desativar o antigo?",
                "Desativar", "Manter"))
        {
            Undo.RecordObject(antigo.gameObject, "Desativar menu antigo");
            antigo.gameObject.SetActive(false);
        }

        try
        {
            Directory.CreateDirectory(PASTA_SPR);
            AssetDatabase.Refresh();
            cacheSprites.Clear();
            cartas.Clear();

            GarantirEventSystem();

            // -------- menu principal e telas
            var canvas = CriarCanvas("MenuCanvas", 0);
            telaAtual = "1a"; var principal = ConstruirTela(telas["1a"], canvas.transform, "PainelPrincipal");
            telaAtual = "1d"; var levels = ConstruirTela(telas["1d"], canvas.transform, "LevelsPainel");
            telaAtual = "1e"; var settings = ConstruirTela(telas["1e"], canvas.transform, "SettingsPainel");
            telaAtual = "1f"; var creditos = ConstruirTela(telas["1f"], canvas.transform, "CreditsPainel");
            telaAtual = "1c"; var popup = ConstruirTela(telas["1c"], canvas.transform, "PopupNovoJogo");

            var mm = canvas.AddComponent<MainMenuManager>();
            var so = new SerializedObject(mm);
            Ref(so, "painelPrincipal", principal);
            Ref(so, "painelLevels", levels);
            Ref(so, "painelSettings", settings);
            Ref(so, "painelCreditos", creditos);
            Ref(so, "popupNovoJogo", popup);
            var btContinuar = Achar(principal.transform, "Btn Continuar");
            Ref(so, "botaoContinuar", btContinuar != null ? btContinuar.gameObject : null);

            var pBotoes = so.FindProperty("botoesLevels");
            if (pBotoes != null)
            {
                pBotoes.arraySize = cartas.Count;
                for (int i = 0; i < cartas.Count; i++) pBotoes.GetArrayElementAtIndex(i).objectReferenceValue = cartas[i];
            }
            var pCenas = so.FindProperty("cenasLevels");
            if (pCenas != null)
            {
                string primeira = pCenas.arraySize > 0 ? pCenas.GetArrayElementAtIndex(0).stringValue : "Level1";
                pCenas.arraySize = cartas.Count;
                for (int i = 0; i < cartas.Count; i++)
                    pCenas.GetArrayElementAtIndex(i).stringValue = i == 0 ? primeira : "Level" + (i + 1);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Ligar(Botao(principal, "Btn Jogar"), mm.Jogar, "Btn Jogar");
            Ligar(Botao(principal, "Btn Continuar"), mm.Continuar, "Btn Continuar");
            Ligar(Botao(principal, "Btn Levels"), mm.AbrirLevels, "Btn Levels");
            Ligar(Botao(principal, "Btn Settings"), mm.AbrirSettings, "Btn Settings");
            Ligar(Botao(principal, "Btn Créditos"), mm.AbrirCreditos, "Btn Créditos");
            Ligar(Botao(principal, "Btn Sair"), mm.Sair, "Btn Sair");
            Ligar(Botao(levels, "Btn Voltar"), mm.Voltar, "Voltar (Levels)");
            Ligar(Botao(settings, "Btn Voltar"), mm.Voltar, "Voltar (Settings)");
            Ligar(Botao(creditos, "Btn Voltar"), mm.Voltar, "Voltar (Créditos)");
            Ligar(Botao(popup, "Btn Sim"), mm.ConfirmarNovoJogo, "Btn Sim");
            Ligar(Botao(popup, "Btn Não"), mm.CancelarNovoJogo, "Btn Não");
            for (int i = 0; i < cartas.Count; i++)
                UnityEventTools.AddIntPersistentListener(cartas[i].onClick, mm.CarregarLevel, i + 1);

            // foco inicial (controle)
            Foco(principal, Achar(principal.transform, "Btn Jogar"));
            Foco(levels, cartas.Count > 0 ? cartas[0].transform : Achar(levels.transform, "Btn Voltar"));
            var primeiroSlider = settings.GetComponentInChildren<Slider>(true);
            Foco(settings, primeiroSlider != null ? primeiroSlider.transform : Achar(settings.transform, "Btn Voltar"));
            Foco(creditos, Achar(creditos.transform, "Btn Voltar"));
            Foco(popup, Achar(popup.transform, "Btn Não"));

            // Sim <-> Não só entre si
            var sim = Botao(popup, "Btn Sim");
            var nao = Botao(popup, "Btn Não");
            if (sim != null && nao != null)
            {
                sim.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = nao };
                nao.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = sim };
            }

            ConfigurarSettings(settings);
            Extras(principal, "Conteudo", false, null, true);
            Extras(levels, "Conteudo", false, Botao(levels, "Btn Voltar"), true);
            Extras(settings, "Conteudo", false, Botao(settings, "Btn Voltar"), true);
            Extras(creditos, "Conteudo", false, Botao(creditos, "Btn Voltar"), true);
            Extras(popup, "Memorando", true, nao, true);

            levels.SetActive(false);
            settings.SetActive(false);
            creditos.SetActive(false);
            popup.SetActive(false);
            Undo.RegisterCreatedObjectUndo(canvas, "Gerar menu");

            // -------- pause (prefab), com a mesma tela de Settings
            var pauseCanvas = CriarCanvas("PauseCanvas", 10);
            telaAtual = "1g";
            var pausePainel = ConstruirTela(telas["1g"], pauseCanvas.transform, "PausePainel");
            telaAtual = "1e";
            var pauseSettings = ConstruirTela(telas["1e"], pauseCanvas.transform, "SettingsPainel");
            ConfigurarSettings(pauseSettings);

            var pm = pauseCanvas.AddComponent<PauseManager>();
            var sp = new SerializedObject(pm);
            Ref(sp, "painelPause", pausePainel);
            Ref(sp, "painelSettings", pauseSettings);
            sp.ApplyModifiedPropertiesWithoutUndo();
            Ligar(Botao(pausePainel, "Btn Continuar"), pm.Despausar, "Continuar (Pause)");
            Ligar(Botao(pausePainel, "Btn Settings"), pm.AbrirSettings, "Settings (Pause)");
            Ligar(Botao(pausePainel, "Btn Voltar ao Menu"), pm.VoltarAoMenu, "Voltar ao Menu (Pause)");
            Ligar(Botao(pauseSettings, "Btn Voltar"), pm.FecharSettings, "Voltar (Settings do pause)");

            Foco(pausePainel, Achar(pausePainel.transform, "Btn Continuar"));
            var sliderPause = pauseSettings.GetComponentInChildren<Slider>(true);
            Foco(pauseSettings, sliderPause != null ? sliderPause.transform : Achar(pauseSettings.transform, "Btn Voltar"));
            Extras(pausePainel, "Painel pause", true, Botao(pausePainel, "Btn Continuar"), false);
            Extras(pauseSettings, "Conteudo", false, Botao(pauseSettings, "Btn Voltar"), false);

            pausePainel.SetActive(false);
            pauseSettings.SetActive(false);
            string caminhoPause = PASTA + "/PauseCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(pauseCanvas, caminhoPause);
            DestroyImmediate(pauseCanvas);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeGameObject = canvas;
            EditorUtility.DisplayDialog("Pronto",
                "MenuCanvas criado na cena e PauseCanvas salvo em " + caminhoPause + ".\n\n" +
                "Confira os nomes das cenas em MainMenuManager > Cenas Levels.", "OK");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Erro ao gerar", e.Message, "OK");
        }
    }

    static void Ref(SerializedObject so, string campo, UnityEngine.Object valor)
    {
        var p = so.FindProperty(campo);
        if (p != null) p.objectReferenceValue = valor;
        else Debug.LogWarning("Campo não encontrado no script: " + campo);
    }

    static Button Botao(GameObject painel, string nome)
    {
        var t = Achar(painel.transform, nome);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static void Ligar(Button b, UnityAction acao, string nome)
    {
        if (b == null) { Debug.LogWarning("Botão não encontrado: " + nome); return; }
        UnityEventTools.AddPersistentListener(b.onClick, acao);
    }

    static void Foco(GameObject painel, Transform alvo)
    {
        if (alvo == null) return;
        painel.AddComponent<FocoInicial>().alvo = alvo.gameObject;
    }

    static Transform Achar(Transform t, string nome)
    {
        if (t.name == nome) return t;
        foreach (Transform c in t)
        {
            var r = Achar(c, nome);
            if (r != null) return r;
        }
        return null;
    }

    static void GarantirEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Undo.RegisterCreatedObjectUndo(go, "EventSystem");
    }

    static GameObject CriarCanvas(string nome, int ordem)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5;
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = ordem;
        var cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        return go;
    }

    // ------------------------------------------------------------------ telas e nós

    GameObject ConstruirTela(Dictionary<string, object> raizNo, Transform pai, string nomePainel)
    {
        var painel = NovoUI(nomePainel, pai);
        var prt = Esticar(painel);

        if (Obj(raizNo, "corFundo") != null)
        {
            var img = painel.AddComponent<Image>();
            img.color = Cor(Obj(raizNo, "corFundo"), Color.black);
        }

        var ifd = D(raizNo, "imagemFundo");
        var coresFundo = L(ifd, "cores");
        if ((S(ifd, "tipo") ?? "").StartsWith("grade") && coresFundo != null && coresFundo.Count > 0)
        {
            var g = NovoUI("Grade", painel.transform);
            Esticar(g);
            var gi = g.AddComponent<Image>();
            gi.sprite = SpriteGrade();
            gi.type = Image.Type.Tiled;
            gi.color = Cor(coresFundo[0], Color.clear);
            gi.raycastTarget = false;
        }

        RectTransform conteudo = null;
        var filhos = L(raizNo, "filhos");
        if (filhos != null)
            foreach (var f in filhos)
            {
                var fd = f as Dictionary<string, object>;
                if (fd == null) continue;
                var tam = D(fd, "tamanho");
                bool telaCheia = F(tam, "largura") >= 1920f && F(tam, "altura") >= 1080f;
                if (telaCheia)
                {
                    var g = Construir(fd, prt, 1920f, raizNo);
                    if (g != null)
                    {
                        Esticar(g);
                        var gi = g.GetComponent<Image>();
                        if (gi != null) gi.raycastTarget = true; // escurecimento bloqueia cliques
                    }
                    continue;
                }
                if (conteudo == null)
                {
                    conteudo = (RectTransform)NovoUI("Conteudo", painel.transform).transform;
                    conteudo.anchorMin = conteudo.anchorMax = conteudo.pivot = new Vector2(0.5f, 0.5f);
                    conteudo.sizeDelta = new Vector2(1920, 1080);
                    conteudo.anchoredPosition = Vector2.zero;
                }
                Construir(fd, conteudo, 1920f, raizNo);
            }
        return painel;
    }

    GameObject Construir(Dictionary<string, object> no, RectTransform pai, float larguraPai, Dictionary<string, object> paiNo)
    {
        if (no == null) return null;
        string tipo = S(no, "tipo") ?? "Grupo";
        string nome = S(no, "nome") ?? tipo;

        if (tipo == "Prefab" || nome.StartsWith("Gameplay") || nome == "Ponto laser") return null;
        if (tipo == "Button" && S(no, "estilo") != null) return ConstruirBotao(no, pai, larguraPai);
        if (nome == "Grade de fases") return ConstruirGradeFases(no, pai);
        if (tipo == "Text") return ConstruirTexto(no, pai, paiNo);
        if (tipo == "Slider") return ConstruirSlider(no, pai);
        if (tipo == "Toggle") return ConstruirToggle(no, pai);
        if (tipo == "Dropdown") return ConstruirDropdown(no, pai);
        if (telaAtual == "1e" && nome == "Opções") return ConstruirOpcoes(no, pai);

        var t = D(no, "tamanho");
        float w = F(t, "largura"), h = F(t, "altura");
        var go = NovoUI(nome, pai);
        var rt = (RectTransform)go.transform;
        Posicionar(rt, no, w, h, F(no, "rotacaoZ"), false);

        bool linhaOpcao = nome == "Linha de opção";
        object raio = Obj(no, "raio");

        Color? cor = null;
        if (Obj(no, "corFundo") != null) cor = Cor(Obj(no, "corFundo"), Color.clear);
        else
        {
            var ifd = D(no, "imagemFundo");
            var cs = L(ifd, "cores");
            if (cs != null && cs.Count > 0 && !(S(ifd, "tipo") ?? "").StartsWith("grade")) cor = Cor(cs[0], Color.clear);
        }
        if (linhaOpcao) cor = Color.clear; // o destaque vem do FocoVisual

        Image img = null;
        if (cor.HasValue)
        {
            img = go.AddComponent<Image>();
            img.color = cor.Value;
            img.raycastTarget = false;
            AplicarFormato(img, raio, w, h, 0, false);
            AplicarSombra(img, L(no, "sombra"));
        }

        var borda = D(no, "borda");
        if (borda != null) CriarBorda(go.transform, raio, w, h, borda);
        var bordas = D(no, "bordas");
        if (bordas != null) CriarBordas(go.transform, raio, w, h, bordas);

        var filhos = L(no, "filhos");
        if (filhos != null)
            foreach (var f in filhos) Construir(f as Dictionary<string, object>, rt, w, no);

        if (telaAtual == "1a" && nome == "Coluna de botões")
        {
            // fecha o espaço quando o Continuar some
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = F(D(no, "layout"), "espaco", 14f);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = false;
            v.childControlHeight = false;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
        }

        if (linhaOpcao) FinalizarLinha(go, img, w, h);
        return go;
    }

    GameObject ConstruirTexto(Dictionary<string, object> no, RectTransform pai, Dictionary<string, object> paiNo)
    {
        var t = D(no, "tamanho");
        var f = D(no, "fonte");
        var pl = D(no, "posicaoLocal");
        string nome = S(no, "nome") ?? "Txt";
        string texto = S(no, "texto") ?? "";
        if (nome == "Txt Fase 03") { nome = "Txt FaseSalva"; texto = ""; }

        var tmp = Texto(nome, pai, F(pl, "x"), F(pl, "y"), F(t, "largura"), F(t, "altura"), texto,
            S(f, "familia"), (int)F(f, "peso", 400f), F(f, "tamanho", 16f), Cor(Obj(f, "cor"), Color.white),
            F(D(f, "espacamentoLetras"), "tmp"), S(f, "alinhamento"));

        // letra dentro de círculo/pílula (A, B, Start, teclas): centraliza no pai
        if (paiNo != null && Obj(paiNo, "raio") != null && texto.Length <= 5)
        {
            var tp = D(paiNo, "tamanho");
            if (F(tp, "largura") <= 72f && F(tp, "altura") <= 40f)
            {
                Esticar(tmp.gameObject);
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }
        return tmp.gameObject;
    }

    TextMeshProUGUI Texto(string nome, Transform pai, float x, float y, float w, float h, string texto,
        string familia, int peso, float tamanho, Color cor, float espacamento, string alinhamento)
    {
        var go = Retangulo(nome, pai, x, y, w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        bool substituta;
        tmp.font = Fonte(familia, peso, out substituta);
        if (substituta && peso >= 700) tmp.fontStyle = FontStyles.Bold;
        tmp.fontSize = tamanho;
        tmp.color = cor;
        tmp.characterSpacing = espacamento;
        tmp.text = texto;
        bool multilinha = h > tamanho * 1.8f;
        tmp.enableWordWrapping = multilinha;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.alignment = Alinhamento(alinhamento, multilinha);
        if (multilinha) ((RectTransform)go.transform).sizeDelta = new Vector2(w * 1.1f, h);
        return tmp;
    }

    static TextAlignmentOptions Alinhamento(string a, bool topo)
    {
        if (a == "centro") return topo ? TextAlignmentOptions.Top : TextAlignmentOptions.Center;
        if (a == "direita") return topo ? TextAlignmentOptions.TopRight : TextAlignmentOptions.Right;
        return topo ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left;
    }

    // ------------------------------------------------------------------ botões

    GameObject ConstruirBotao(Dictionary<string, object> no, RectTransform pai, float larguraPai)
    {
        string estilo = S(no, "estilo");
        var est = D(estilos, estilo);
        var normal = D(est, "normal");
        var sel = D(est, "selecionado");
        var press = D(est, "pressionado");
        var des = D(est, "desativado");

        var t = D(no, "tamanho");
        float w = F(t, "largura"), h = F(t, "altura");
        if (larguraPai > 0f && w > larguraPai) w = larguraPai; // pause: 560 numa coluna de 556

        var go = NovoUI(S(no, "nome") ?? "Botao", pai);
        var rt = (RectTransform)go.transform;
        Posicionar(rt, no, w, h, 0f, true); // sempre nasce no estado normal

        var alvo = go.AddComponent<Image>();
        alvo.color = Color.clear; // área de clique
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = alvo;
        btn.interactable = B(no, "interactable", true);

        var vis = NovoUI("Visual", go.transform);
        var vrt = Esticar(vis);
        object raio = Obj(est, "raio") ?? Obj(no, "raio");

        var fundo = vis.AddComponent<Image>();
        fundo.raycastTarget = false;
        fundo.color = Cor(Obj(normal, "corFundo"), Color.white);
        AplicarFormato(fundo, raio, w, h, 0, false);

        var dadosSombra = D(normal, "sombra") ?? D(sel, "sombra");
        Shadow sombra = dadosSombra != null ? SombraDe(fundo, dadosSombra) : null;

        var borda = D(normal, "borda");
        if (borda != null) CriarBorda(vis.transform, raio, w, h, borda);

        var filhos = L(no, "filhos");
        if (filhos != null)
            foreach (var f in filhos) Construir(f as Dictionary<string, object>, vrt, w, no);

        var textos = vis.GetComponentsInChildren<TMP_Text>(true).Cast<Graphic>().ToArray();

        GameObject laser = null;
        if (estilo == "botaoPrincipal") laser = CriarLaser(vis.transform, -46f, (h - 18f) / 2f);
        else if (estilo == "botaoPopup") laser = CriarLaser(vis.transform, w + 20f, (h - 18f) / 2f);

        var foco = go.AddComponent<FocoVisual>();
        foco.alvo = btn;
        foco.visual = vrt;
        foco.fundo = fundo;
        foco.corNormal = fundo.color;
        foco.corSelecionado = Cor(Obj(sel, "corFundo"), foco.corNormal);
        foco.corPressionado = Cor(Obj(press, "corFundo"), foco.corSelecionado);
        foco.corDesativado = Cor(Obj(des, "corFundo"), foco.corNormal);
        foco.aplicarDesativado = true;
        foco.textos = textos;
        foco.textoDesativado = Cor(Obj(des, "corTexto"), Color.gray);

        var desl = D(sel, "deslocamento");
        bool deslNo = S(no, "estado") == "selecionado" && D(no, "deslocamento") != null;
        if (deslNo) desl = D(no, "deslocamento"); // pause usa 12 px em vez de 24
        foco.deslocSelecionado = Desloc(desl);
        foco.deslocPressionado = Desloc(D(press, "deslocamento"));
        if (deslNo && D(press, "deslocamento") != null)
            foco.deslocPressionado = new Vector2(foco.deslocSelecionado.x, foco.deslocPressionado.y);
        foco.rotSelecionado = F(sel, "rotacaoZ");

        foco.sombra = sombra;
        foco.sombraSoSelecionado = D(normal, "sombra") == null;
        if (sombra != null) sombra.enabled = !foco.sombraSoSelecionado;
        foco.mostrarQuandoSelecionado = laser != null ? new[] { laser } : new GameObject[0];
        return go;
    }

    // ------------------------------------------------------------------ levels

    GameObject ConstruirGradeFases(Dictionary<string, object> no, RectTransform pai)
    {
        var t = D(no, "tamanho");
        var go = NovoUI("Grade de fases", pai);
        Posicionar((RectTransform)go.transform, no, F(t, "largura"), F(t, "altura"), 0f, false);
        var filhos = L(no, "filhos");
        if (filhos != null)
        {
            int i = 0;
            foreach (var f in filhos)
            {
                var fd = f as Dictionary<string, object>;
                if (fd == null) continue;
                var carta = ConstruirCarta(fd, (RectTransform)go.transform, i);
                cartas.Add(carta.GetComponent<Button>());
                i++;
            }
        }
        return go;
    }

    // Cada card tem a versão liberada e a bloqueada; o FocoVisual troca conforme interactable,
    // então o SaveSystem pode liberar fases sem gerar o menu de novo.
    GameObject ConstruirCarta(Dictionary<string, object> no, RectTransform pai, int indice)
    {
        var est = D(estilos, "cardFase");
        var normal = D(est, "normal");
        var sel = D(est, "selecionado");
        var press = D(est, "pressionado");
        var des = D(est, "desativado");
        var chefeD = D(est, "varianteChefe");
        bool chefe = S(no, "variante") == "chefe";
        bool livre = B(no, "interactable", true);

        string nomeFase = AcharTexto(no, n => F(D(n, "fonte"), "tamanho") >= 26f) ?? ("Fase " + (indice + 1).ToString("00"));
        string textoAba = AcharTexto(no, n =>
        {
            string s = S(n, "texto") ?? "";
            return s.StartsWith("FASE") || s.StartsWith("CHEFE");
        }) ?? ("FASE " + (indice + 1).ToString("00"));

        var t = D(no, "tamanho");
        float w = F(t, "largura", 380f), h = F(t, "altura", 300f);
        var go = NovoUI("Card Fase " + (indice + 1).ToString("00"), pai);
        Posicionar((RectTransform)go.transform, no, w, h, 0f, true);

        var alvo = go.AddComponent<Image>();
        alvo.color = Color.clear;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = alvo;
        btn.interactable = livre;

        var vis = NovoUI("Visual", go.transform);
        var vrt = Esticar(vis);
        var laser = CriarLaser(vis.transform, -34f, 180f);

        Color abaNormal = chefe ? Cor(Obj(chefeD, "corAba"), Color.red) : Cor(Obj(normal, "corAba"), Color.white);
        Color abaTextoNormal = chefe ? Cor(Obj(chefeD, "corTextoAba"), Color.white) : Cor(Obj(normal, "corTextoAba"), Color.black);

        // aba
        var aba = Retangulo("Aba", vis.transform, 0, 0, 170, 36);
        var abaImg = aba.AddComponent<Image>();
        abaImg.raycastTarget = false;
        AplicarFormato(abaImg, Obj(D(est, "aba"), "raio"), 170, 36, 0, false);
        abaImg.color = livre || chefe ? abaNormal : Cor(Obj(des, "corAba"), Color.gray);
        var abaTxt = Texto("Txt Aba", aba.transform, 16, 8, 150, 20, textoAba, "JetBrains Mono", 700, 15, abaTextoNormal, 12, "esquerda");

        // pasta (liberada)
        object raioPasta = Obj(D(est, "pasta"), "raio");
        var pasta = Retangulo("Pasta", vis.transform, 0, 36, w, 264);
        var pastaImg = pasta.AddComponent<Image>();
        pastaImg.raycastTarget = false;
        pastaImg.color = Cor(Obj(normal, "corPasta"), Color.white);
        AplicarFormato(pastaImg, raioPasta, w, 264, 0, false);
        var sombra = SombraDe(pastaImg, D(normal, "sombra"));
        if (sombra != null) sombra.enabled = livre;

        var contorno = NovoUI("Contorno", pasta.transform);
        Esticar(contorno, 6f);
        var ci = contorno.AddComponent<Image>();
        ci.raycastTarget = false;
        ci.color = Cor(Obj(D(sel, "contorno"), "cor"), Color.yellow);
        AplicarFormato(ci, new[] { 6, 16, 16, 16 }, w + 12, 276, 6, false);
        contorno.SetActive(false);

        var print = Retangulo("Print", pasta.transform, 20, 20, w - 40, 140);
        var pi = print.AddComponent<Image>();
        pi.raycastTarget = false;
        pi.color = Cor("#D9B877", Color.white);
        AplicarFormato(pi, 6.0, w - 40, 140, 0, false);
        Texto("Txt Print", print.transform, 12, 113, 200, 17, "print da fase", "JetBrains Mono", 400, 13, Cor("#6B5530", Color.black), 0, "esquerda");
        Texto("Txt Nome", pasta.transform, 20, 174, w - 40, 36, nomeFase, "Bricolage Grotesque", 800, 30, Cor(Obj(normal, "corTexto"), Color.black), 0, "esquerda");
        Texto("Txt Status", pasta.transform, 20, 218, w - 40, 20, "", "JetBrains Mono", 700, 15, Cor(Obj(normal, "corStatus"), Color.black), 12, "esquerda");

        // bloqueio (aparece quando interactable = false)
        var bloq = Retangulo("Bloqueio", vis.transform, 0, 36, w, 264);
        var bi = bloq.AddComponent<Image>();
        bi.raycastTarget = false;
        bi.color = Cor(Obj(des, "corPasta"), Color.gray);
        AplicarFormato(bi, raioPasta, w, 264, 0, false);
        var bordaDes = D(des, "borda");
        if (bordaDes != null) CriarBorda(bloq.transform, raioPasta, w, 264, bordaDes);
        Color corCadeado = Cor(Obj(des, "corCadeado"), Color.gray);
        var cad = Retangulo("Cadeado", bloq.transform, (w - 56) / 2f, 62, 56, 64);
        var arco = Retangulo("Arco", cad.transform, 10, 0, 36, 34);
        var ai = arco.AddComponent<Image>();
        ai.raycastTarget = false;
        ai.color = corCadeado;
        AplicarFormato(ai, new[] { 18, 18, 0, 0 }, 36, 34, 6, false);
        var corpo = Retangulo("Corpo", cad.transform, 0, 24, 56, 40);
        var co = corpo.AddComponent<Image>();
        co.raycastTarget = false;
        co.color = corCadeado;
        AplicarFormato(co, 8.0, 56, 40, 0, false);
        Texto("Txt Nome", bloq.transform, 0, 140, w, 31, nomeFase, "Bricolage Grotesque", 700, 26, Cor(Obj(des, "corTexto"), Color.gray), 0, "centro");
        Texto("Txt Bloqueado", bloq.transform, 0, 185, w, 18, "BLOQUEADO", "JetBrains Mono", 700, 14, Cor(Obj(des, "corStatus"), Color.gray), 16, "centro");
        bloq.SetActive(!livre);

        var foco = go.AddComponent<FocoVisual>();
        foco.alvo = btn;
        foco.visual = vrt;
        foco.fundo = abaImg;
        foco.corNormal = abaNormal;
        foco.corSelecionado = Cor(Obj(sel, "corAba"), Color.yellow);
        foco.corPressionado = Cor(Obj(press, "corAba"), foco.corSelecionado);
        foco.corDesativado = chefe ? abaNormal : Cor(Obj(des, "corAba"), Color.gray);
        foco.aplicarDesativado = true;
        foco.textos = new Graphic[] { abaTxt };
        foco.textoDesativado = chefe ? abaTextoNormal : Cor(Obj(des, "corTextoAba"), Color.gray);
        foco.deslocSelecionado = Desloc(D(sel, "deslocamento"));
        foco.deslocPressionado = Desloc(D(press, "deslocamento"));
        foco.rotSelecionado = F(sel, "rotacaoZ");
        foco.sombra = sombra;
        foco.mostrarQuandoSelecionado = new[] { laser, contorno };
        foco.mostrarQuandoDesativado = new[] { bloq };
        return go;
    }

    static string AcharTexto(Dictionary<string, object> no, Func<Dictionary<string, object>, bool> filtro)
    {
        if (no == null) return null;
        if (S(no, "tipo") == "Text" && filtro(no)) return S(no, "texto");
        var filhos = L(no, "filhos");
        if (filhos == null) return null;
        foreach (var f in filhos)
        {
            var r = AcharTexto(f as Dictionary<string, object>, filtro);
            if (r != null) return r;
        }
        return null;
    }

    // ------------------------------------------------------------------ settings

    void FinalizarLinha(GameObject linha, Image fundo, float w, float h)
    {
        var destaque = NovoUI("Destaque", linha.transform);
        Esticar(destaque);
        var di = destaque.AddComponent<Image>();
        di.raycastTarget = false;
        di.color = Cor("#FFD84D", Color.yellow);
        AplicarFormato(di, 8.0, w, h, 2, false);
        destaque.transform.SetSiblingIndex(0);
        destaque.SetActive(false);

        var laser = CriarLaser(linha.transform, -36f, (h - 18f) / 2f);

        var alvo = linha.GetComponentInChildren<Selectable>(true);
        if (alvo != null && fundo != null)
        {
            var f = alvo.gameObject.AddComponent<FocoVisual>();
            f.alvo = alvo;
            f.fundo = fundo;
            f.corNormal = Color.clear;
            f.corSelecionado = Cor("#222C3A", Color.gray);
            f.corPressionado = f.corSelecionado;
            f.aplicarDesativado = false;
            f.mostrarQuandoSelecionado = new[] { destaque, laser };
        }

        foreach (var s in linha.GetComponentsInChildren<Slider>(true))
        {
            var valor = TextoIrmao(s.transform);
            if (valor == null) continue;
            var sv = s.gameObject.AddComponent<SliderValorTexto>();
            sv.texto = valor;
            bool decimais = valor.text.Contains(",");
            sv.escala = decimais ? 10f : 100f;
            sv.formato = decimais ? "0.0" : "0";
        }

        foreach (var sw in linha.GetComponentsInChildren<ToggleSwitch>(true))
            sw.rotulo = TextoIrmao(sw.transform);
    }

    // Lista de opções do Settings com rolagem (as opções não cabem mais numa tela só).
    GameObject ConstruirOpcoes(Dictionary<string, object> no, RectTransform pai)
    {
        var pl = D(no, "posicaoLocal");
        float x = F(pl, "x", 140f), y = F(pl, "y", 240f);
        float w = F(D(no, "tamanho"), "largura", 980f);
        const float margem = 60f;          // espaço à esquerda para o ponto laser não ser cortado
        float alturaVisivel = 930f - y;    // até a barra inferior

        var go = Retangulo("Opções", pai, x - margem, y, w + margem, alturaVisivel);
        var area = go.AddComponent<Image>();
        area.color = Color.clear;          // recebe a rodinha do mouse
        var sr = go.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;

        var vp = NovoUI("Viewport", go.transform);
        var vprt = Esticar(vp);
        vp.AddComponent<RectMask2D>();
        var content = NovoUI("Content", vp.transform);
        var crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        sr.viewport = vprt;
        sr.content = crt;

        float cy = 0f;
        Color corTexto = Cor("#F3EFE6", Color.white);

        void Cabecalho(string texto)
        {
            Texto("Txt " + texto, content.transform, margem + 24f, cy + 15f, 400f, 21f, texto,
                "JetBrains Mono", 700, 16f, Cor("#7A8291", Color.gray), 20f, "esquerda");
            cy += 50f;
        }

        GameObject NovaLinha(string rotulo)
        {
            var linha = Retangulo("Linha de opção", content.transform, margem, cy, w, 76f);
            var img = linha.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            AplicarFormato(img, 8.0, w, 76f, 0, false);
            Texto("Txt " + rotulo, linha.transform, 24f, 20f, 440f, 36f, rotulo,
                "Bricolage Grotesque", 600, 30f, corTexto, 0f, "esquerda");
            cy += 82f;
            return linha;
        }

        void LinhaSlider(string rotulo, string nome, float valor)
        {
            var linha = NovaLinha(rotulo);
            var grupo = Retangulo("Valor", linha.transform, 476f, 18f, 480f, 40f);
            SliderEm(grupo.transform, nome, 0f, 0f, 400f, 40f, valor);
            Texto("Txt Valor", grupo.transform, 424f, 5f, 56f, 31f, Mathf.RoundToInt(valor * 100f).ToString(),
                "JetBrains Mono", 700, 24f, corTexto, 0f, "direita");
            FinalizarLinha(linha, linha.GetComponent<Image>(), w, 76f);
        }

        void LinhaToggle(string rotulo, string nome, bool ligado)
        {
            var linha = NovaLinha(rotulo);
            var grupo = Retangulo("Chave", linha.transform, 476f, 14f, 480f, 48f);
            Texto("Txt Estado", grupo.transform, 204f, 11f, 160f, 26f, ligado ? "LIGADO" : "DESLIGADO",
                "JetBrains Mono", 700, 20f, Cor("#B8BEC8", Color.gray), 0f, "direita");
            ToggleEm(grupo.transform, nome, 384f, 0f, 96f, 48f, ligado);
            FinalizarLinha(linha, linha.GetComponent<Image>(), w, 76f);
        }

        void LinhaDropdown(string rotulo, string nome, string[] opcoes, int valor)
        {
            var linha = NovaLinha(rotulo);
            DropdownEm(linha.transform, nome, 476f, 10f, 480f, 56f, opcoes, valor);
            FinalizarLinha(linha, linha.GetComponent<Image>(), w, 76f);
        }

        Cabecalho("ÁUDIO");
        LinhaSlider("Volume geral", "Slider VolumeGeral", 0.8f);
        LinhaSlider("Música", "Slider Musica", 0.6f);
        LinhaSlider("Efeitos", "Slider Efeitos", 0.75f);
        Cabecalho("VÍDEO");
        LinhaDropdown("Resolução", "Dropdown Resolucao", Resolucoes, 0);
        LinhaToggle("Tela cheia", "Toggle TelaCheia", true);
        LinhaDropdown("Qualidade gráfica", "Dropdown Qualidade", new[] { "Baixa", "Média", "Alta" }, 2);
        LinhaDropdown("Limite de FPS", "Dropdown FPS", new[] { "60", "120", "144", "Sem limite" }, 3);
        LinhaToggle("VSync", "Toggle VSync", false);
        Cabecalho("ACESSIBILIDADE");
        LinhaToggle("Modo daltônico", "Toggle Daltonico", false);
        LinhaToggle("Tremor de tela", "Toggle Tremor", true);
        LinhaToggle("Vibração do controle", "Toggle Vibracao", true);

        crt.sizeDelta = new Vector2(0f, cy + 10f);
        go.AddComponent<RolarAteSelecionado>();
        return go;
    }

    static void ConfigurarSettings(GameObject painel)
    {
        var ui = painel.AddComponent<SettingsPainelUI>();
        ui.volumeGeral = Comp<Slider>(painel, "Slider VolumeGeral");
        ui.volumeMusica = Comp<Slider>(painel, "Slider Musica");
        ui.volumeEfeitos = Comp<Slider>(painel, "Slider Efeitos");
        ui.resolucao = Comp<TMP_Dropdown>(painel, "Dropdown Resolucao");
        ui.telaCheia = Comp<Toggle>(painel, "Toggle TelaCheia");
        ui.qualidade = Comp<TMP_Dropdown>(painel, "Dropdown Qualidade");
        ui.limiteFps = Comp<TMP_Dropdown>(painel, "Dropdown FPS");
        ui.vsync = Comp<Toggle>(painel, "Toggle VSync");
        ui.daltonico = Comp<Toggle>(painel, "Toggle Daltonico");
        ui.tremor = Comp<Toggle>(painel, "Toggle Tremor");
        ui.vibracao = Comp<Toggle>(painel, "Toggle Vibracao");
    }

    static T Comp<T>(GameObject painel, string nome) where T : Component
    {
        var t = Achar(painel.transform, nome);
        return t != null ? t.GetComponent<T>() : null;
    }

    // animação de entrada (DOTween) e botão B/Esc para voltar
    static void Extras(GameObject painel, string alvoTransicao, bool pulo, Button cancelar, bool usarEsc)
    {
        var tr = painel.AddComponent<TransicaoPainel>();
        tr.alvo = Achar(painel.transform, alvoTransicao) as RectTransform;
        tr.pulo = pulo;
        if (cancelar == null) return;
        var ac = painel.AddComponent<AtalhoCancelar>();
        ac.botao = cancelar;
        ac.usarEsc = usarEsc;
    }

    static TMP_Text TextoIrmao(Transform t)
    {
        if (t.parent == null) return null;
        foreach (Transform irmao in t.parent)
        {
            if (irmao == t) continue;
            var tx = irmao.GetComponent<TMP_Text>();
            if (tx != null) return tx;
        }
        return null;
    }

    GameObject ConstruirSlider(Dictionary<string, object> no, RectTransform pai)
    {
        var t = D(no, "tamanho");
        var pl = D(no, "posicaoLocal");
        return SliderEm(pai, S(no, "nome") ?? "Slider", F(pl, "x"), F(pl, "y"),
            F(t, "largura", 400f), F(t, "altura", 40f), F(no, "valorPercentual", 50f) / 100f);
    }

    GameObject SliderEm(Transform pai, string nome, float x, float y, float w, float h, float valor)
    {
        var est = D(estilos, "slider");
        var trilho = D(est, "trilho");
        var alcaD = D(est, "alca");
        float hTrilho = F(trilho, "altura", 10f);

        var go = Retangulo(nome, pai, x, y, w, h);

        var bg = NovoUI("Background", go.transform);
        var bgrt = (RectTransform)bg.transform;
        bgrt.anchorMin = new Vector2(0f, 0.5f);
        bgrt.anchorMax = new Vector2(1f, 0.5f);
        bgrt.sizeDelta = new Vector2(0f, hTrilho);
        bgrt.anchoredPosition = Vector2.zero;
        var bgi = bg.AddComponent<Image>();
        bgi.color = Cor(Obj(trilho, "cor"), Color.gray);
        AplicarFormato(bgi, Obj(trilho, "raio"), w, hTrilho, 0, false);

        var area = NovoUI("Fill Area", go.transform);
        var art = (RectTransform)area.transform;
        art.anchorMin = new Vector2(0f, 0.5f);
        art.anchorMax = new Vector2(1f, 0.5f);
        art.sizeDelta = new Vector2(0f, hTrilho);
        art.anchoredPosition = Vector2.zero;
        var fill = NovoUI("Fill", area.transform);
        var frt = (RectTransform)fill.transform;
        frt.sizeDelta = Vector2.zero;
        var fi = fill.AddComponent<Image>();
        fi.color = Cor(Obj(D(est, "preenchimento"), "cor"), Color.yellow);
        AplicarFormato(fi, Obj(trilho, "raio"), w, hTrilho, 0, false);

        var harea = NovoUI("Handle Slide Area", go.transform);
        Esticar(harea);
        var handle = NovoUI("Handle", harea.transform);
        var hrt = (RectTransform)handle.transform;
        hrt.sizeDelta = new Vector2(F(alcaD, "largura", 24f), 0f);
        var hi = handle.AddComponent<Image>();
        hi.color = Cor(Obj(alcaD, "normal"), Color.white);
        AplicarFormato(hi, Obj(alcaD, "raio"), F(alcaD, "largura", 24f), F(alcaD, "altura", 40f), 0, false);
        SombraDe(hi, D(alcaD, "sombra"));

        var s = go.AddComponent<Slider>();
        s.transition = Selectable.Transition.None;
        s.fillRect = frt;
        s.handleRect = hrt;
        s.targetGraphic = hi;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = valor;
        return go;
    }

    GameObject ConstruirToggle(Dictionary<string, object> no, RectTransform pai)
    {
        var t = D(no, "tamanho");
        var pl = D(no, "posicaoLocal");
        return ToggleEm(pai, S(no, "nome") ?? "Toggle", F(pl, "x"), F(pl, "y"),
            F(t, "largura", 96f), F(t, "altura", 48f), B(no, "ligado", true));
    }

    GameObject ToggleEm(Transform pai, string nome, float x, float y, float w, float h, bool ligado)
    {
        var est = D(estilos, "toggle");
        var lig = D(est, "ligado");
        var desl = D(est, "desligado");
        float alca = F(D(est, "alca"), "tamanho", 36f), margem = F(D(est, "alca"), "margem", 6f);

        var go = Retangulo(nome, pai, x, y, w, h);
        var img = go.AddComponent<Image>();
        AplicarFormato(img, Obj(est, "raio") ?? 24.0, w, h, 0, false);

        var hgo = Retangulo("Handle", go.transform, margem, margem, alca, alca);
        var hi = hgo.AddComponent<Image>();
        hi.raycastTarget = false;
        AplicarFormato(hi, "50%", alca, alca, 0, false);

        var tg = go.AddComponent<Toggle>();
        tg.transition = Selectable.Transition.None;
        tg.targetGraphic = img;
        tg.graphic = null;
        tg.isOn = ligado;

        var sw = go.AddComponent<ToggleSwitch>();
        sw.fundo = img;
        sw.alca = (RectTransform)hgo.transform;
        sw.alcaImagem = hi;
        sw.fundoLigado = Cor(Obj(lig, "corFundo"), sw.fundoLigado);
        sw.alcaLigado = Cor(Obj(lig, "corAlca"), sw.alcaLigado);
        sw.fundoDesligado = Cor(Obj(desl, "corFundo"), sw.fundoDesligado);
        sw.alcaDesligado = Cor(Obj(desl, "corAlca"), sw.alcaDesligado);
        sw.xLigado = w - alca - margem;
        sw.xDesligado = margem;
        sw.Atualizar();
        return go;
    }

    GameObject ConstruirDropdown(Dictionary<string, object> no, RectTransform pai)
    {
        var t = D(no, "tamanho");
        var pl = D(no, "posicaoLocal");
        return DropdownEm(pai, S(no, "nome") ?? "Dropdown", F(pl, "x"), F(pl, "y"),
            F(t, "largura", 480f), F(t, "altura", 56f), Resolucoes, 0);
    }

    GameObject DropdownEm(Transform pai, string nome, float x, float y, float w, float h, string[] opcoes, int valor)
    {
        var est = D(estilos, "dropdown");
        var fechado = D(est, "fechado");
        var aberto = D(est, "aberto");
        var item = D(est, "item");
        object raio = 6.0;

        var res = new TMP_DefaultControls.Resources();
        res.standard = SpriteRetangulo(new[] { 6, 6, 6, 6 }, 0, false);
        res.background = res.standard;
        res.inputField = res.standard;
        res.mask = res.standard;
        res.knob = SpriteCirculo();
        res.checkmark = SpriteCirculo();
        res.dropdown = SpriteCirculo();

        var go = TMP_DefaultControls.CreateDropdown(res);
        go.name = nome;
        go.transform.SetParent(pai, false);
        foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 5;
        PosicionarXY((RectTransform)go.transform, x, y, w, h, 0f, false);

        var dd = go.GetComponent<TMP_Dropdown>();
        dd.transition = Selectable.Transition.None;
        var img = go.GetComponent<Image>();
        img.color = Cor(Obj(fechado, "corFundo"), Color.gray);
        AplicarFormato(img, raio, w, h, 0, false);
        var bordaF = D(fechado, "borda");
        if (bordaF != null) CriarBorda(go.transform, raio, w, h, bordaF).transform.SetSiblingIndex(0);

        var fonteD = D(est, "fonte");
        bool subst;
        var fonte = Fonte(S(fonteD, "familia") ?? "JetBrains Mono", (int)F(fonteD, "peso", 700f), out subst);

        var cap = dd.captionText;
        if (cap != null)
        {
            cap.font = fonte;
            cap.fontSize = F(fonteD, "tamanho", 24f);
            cap.color = Cor(Obj(fechado, "corTexto"), Color.white);
            cap.alignment = TextAlignmentOptions.Left;
            cap.rectTransform.offsetMin = new Vector2(20f, 0f);
            cap.rectTransform.offsetMax = new Vector2(-50f, 0f);
        }

        var seta = go.transform.Find("Arrow");
        if (seta != null)
        {
            var si = seta.GetComponent<Image>();
            if (si != null) DestroyImmediate(si);
            var st = seta.gameObject.AddComponent<TextMeshProUGUI>();
            st.text = "▾";
            st.font = fonte;
            st.fontSize = 24f;
            st.color = Cor(Obj(fechado, "corSeta"), Color.yellow);
            st.alignment = TextAlignmentOptions.Center;
            st.raycastTarget = false;
            var srt = (RectTransform)seta;
            srt.sizeDelta = new Vector2(30f, 40f);
            srt.anchoredPosition = new Vector2(-30f, 0f);
        }

        float hItem = F(item, "altura", 48f);
        var tpl = dd.template;
        if (tpl != null)
        {
            // altura para 6 itens; o TMP encolhe sozinho quando há menos
            tpl.sizeDelta = new Vector2(0f, 6 * hItem + 8f);
            var ti = tpl.GetComponent<Image>();
            if (ti != null)
            {
                ti.color = Cor(Obj(fechado, "corFundo"), Color.gray);
                AplicarFormato(ti, raio, w, tpl.sizeDelta.y, 0, false);
            }
            var bordaA = D(aberto, "borda");
            if (bordaA != null) CriarBorda(tpl, raio, w, tpl.sizeDelta.y, bordaA).transform.SetSiblingIndex(0);

            var sr = tpl.GetComponent<ScrollRect>();
            var barra = tpl.Find("Scrollbar");
            if (sr != null) sr.verticalScrollbar = null;
            if (barra != null) DestroyImmediate(barra.gameObject);

            var viewport = tpl.Find("Viewport") as RectTransform;
            if (viewport != null)
            {
                viewport.offsetMin = new Vector2(2f, 2f);
                viewport.offsetMax = new Vector2(-2f, -2f);
            }

            var it = tpl.Find("Viewport/Content/Item");
            if (it != null)
            {
                ((RectTransform)it).sizeDelta = new Vector2(0f, hItem);
                var check = it.Find("Item Checkmark");
                if (check != null)
                {
                    var chi = check.GetComponent<Image>();
                    if (chi != null) chi.color = Color.clear;
                }
                var tg = it.GetComponent<Toggle>();
                if (tg != null)
                {
                    var cb = tg.colors;
                    cb.normalColor = Cor(Obj(fechado, "corFundo"), Color.gray);
                    cb.highlightedColor = Cor(Obj(D(item, "selecionado"), "corFundo"), Color.yellow);
                    cb.selectedColor = cb.highlightedColor;
                    cb.pressedColor = Cor("#E9BE14", Color.yellow);
                    tg.colors = cb;
                }
            }

            var itemTxt = dd.itemText;
            if (itemTxt != null)
            {
                itemTxt.font = fonte;
                itemTxt.fontSize = 22f;
                itemTxt.color = Cor(Obj(D(item, "normal"), "corTexto"), Color.white);
                itemTxt.alignment = TextAlignmentOptions.Left;
                itemTxt.rectTransform.offsetMin = new Vector2(18f, 0f);
                itemTxt.rectTransform.offsetMax = new Vector2(-10f, 0f);
            }
        }

        dd.options.Clear();
        foreach (var r in opcoes) dd.options.Add(new TMP_Dropdown.OptionData(r));
        dd.value = Mathf.Clamp(valor, 0, opcoes.Length - 1);
        dd.RefreshShownValue();
        return go;
    }

    // ------------------------------------------------------------------ peças visuais

    GameObject CriarLaser(Transform pai, float x, float y)
    {
        var g = Retangulo("Ponto laser", pai, x, y, 18, 18);

        var brilho = NovoUI("Brilho", g.transform);
        Esticar(brilho, 28f);
        var bi = brilho.AddComponent<Image>();
        bi.sprite = SpriteBrilho();
        bi.color = Cor("#FF4B3E8C", Color.red);
        bi.raycastTarget = false;

        var anel = NovoUI("Anel", g.transform);
        Esticar(anel, 4f);
        var ai = anel.AddComponent<Image>();
        ai.sprite = SpriteCirculo();
        ai.color = Cor("#FF4B3E40", Color.red);
        ai.raycastTarget = false;

        var nucleo = NovoUI("Nucleo", g.transform);
        Esticar(nucleo);
        var ni = nucleo.AddComponent<Image>();
        ni.sprite = SpriteCirculo();
        ni.color = Cor("#FF4B3E", Color.red);
        ni.raycastTarget = false;

        g.SetActive(false);
        return g;
    }

    GameObject CriarBorda(Transform pai, object raio, float w, float h, Dictionary<string, object> borda)
    {
        var go = NovoUI("Borda", pai);
        Esticar(go);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = Cor(Obj(borda, "cor"), Color.white);
        int largura = Mathf.Max(1, Mathf.RoundToInt(F(borda, "largura", 1f)));
        AplicarFormato(img, raio, w, h, largura, S(borda, "estilo") == "tracejada");
        return go;
    }

    void CriarBordas(Transform pai, object raio, float w, float h, Dictionary<string, object> bordas)
    {
        if (bordas.Count >= 3)
        {
            CriarBorda(pai, raio, w, h, bordas.Values.First() as Dictionary<string, object>);
            return;
        }
        foreach (var kv in bordas)
        {
            var b = kv.Value as Dictionary<string, object>;
            if (b == null) continue;
            float lw = Mathf.Max(1f, F(b, "largura", 1f));
            var go = NovoUI("Linha " + kv.Key, pai);
            var rt = (RectTransform)go.transform;
            switch (kv.Key)
            {
                case "topo":
                    rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, lw); break;
                case "base":
                    rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(0f, lw); break;
                case "esquerda":
                    rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f); rt.sizeDelta = new Vector2(lw, 0f); break;
                default:
                    rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 0.5f); rt.sizeDelta = new Vector2(lw, 0f); break;
            }
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = Cor(Obj(b, "cor"), Color.white);
            img.raycastTarget = false;
        }
    }

    static void AplicarSombra(Graphic g, List<object> sombras)
    {
        if (sombras == null) return;
        foreach (var o in sombras)
        {
            var s = o as Dictionary<string, object>;
            if (s == null || Obj(s, "expansao") != null || Obj(s, "desfoque") != null || Obj(s, "interna") != null) continue;
            SombraDe(g, s);
            return;
        }
    }

    static Shadow SombraDe(Graphic g, Dictionary<string, object> s)
    {
        if (s == null) return null;
        var sh = g.gameObject.AddComponent<Shadow>();
        sh.effectColor = Cor(Obj(s, "cor"), Color.black);
        sh.effectDistance = new Vector2(F(s, "x"), -F(s, "y"));
        sh.useGraphicAlpha = false;
        return sh;
    }

    void AplicarFormato(Image img, object raio, float w, float h, int borda, bool tracejada)
    {
        bool circulo;
        int[] r = Raios(raio, out circulo);
        if (circulo && borda == 0)
        {
            img.sprite = SpriteCirculo();
            img.type = Image.Type.Simple;
            return;
        }
        int limite = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(w, h) / 2f));
        for (int i = 0; i < 4; i++) r[i] = circulo ? limite : Mathf.Min(r[i], limite);
        if (borda == 0 && r.All(v => v == 0))
        {
            img.sprite = null;
            img.type = Image.Type.Simple;
            return;
        }
        img.sprite = SpriteRetangulo(r, borda, tracejada);
        img.type = tracejada ? Image.Type.Tiled : Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = 1f;
    }

    static int[] Raios(object raio, out bool circulo)
    {
        circulo = false;
        var s = raio as string;
        if (s != null) { circulo = s.Trim().EndsWith("%"); return new int[4]; }
        if (raio is double) { int v = Mathf.RoundToInt((float)(double)raio); return new[] { v, v, v, v }; }
        var arr = raio as int[];
        if (arr != null && arr.Length >= 4) return new[] { arr[0], arr[1], arr[2], arr[3] };
        var l = raio as List<object>;
        if (l != null && l.Count >= 4)
            return new[] { Mathf.RoundToInt(Num(l[0])), Mathf.RoundToInt(Num(l[1])), Mathf.RoundToInt(Num(l[2])), Mathf.RoundToInt(Num(l[3])) };
        return new int[4];
    }

    // ------------------------------------------------------------------ sprites gerados

    Sprite CarregarOuCriar(string nome, Vector4 borda, bool repetir, Func<Texture2D> gerar)
    {
        Sprite sp;
        if (cacheSprites.TryGetValue(nome, out sp) && sp != null) return sp;
        string caminho = PASTA_SPR + "/" + nome + ".png";
        if (!File.Exists(caminho))
        {
            var tex = gerar();
            File.WriteAllBytes(caminho, tex.EncodeToPNG());
            DestroyImmediate(tex);
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceSynchronousImport);
            var ti = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.spriteBorder = borda;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
        }
        sp = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        cacheSprites[nome] = sp;
        return sp;
    }

    static Texture2D NovaTextura(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
    }

    // retângulo arredondado (raios: sup-esq, sup-dir, inf-dir, inf-esq), cheio ou só a borda
    Sprite SpriteRetangulo(int[] r, int borda, bool tracejada)
    {
        string nome = "ret_" + r[0] + "_" + r[1] + "_" + r[2] + "_" + r[3]
                      + (borda > 0 ? "_b" + borda : "") + (tracejada ? "_tr" : "");
        int m = Mathf.Max(r.Max(), borda) + 1;
        const int traco = 6, periodo = 10;
        int meio = tracejada ? periodo : 2;
        int W = 2 * m + meio, H = 2 * m + meio;

        return CarregarOuCriar(nome, new Vector4(m, m, m, m), false, () =>
        {
            var tex = NovaTextura(W, H);
            var px = new Color32[W * H];
            float hw = W / 2f, hh = H / 2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float cx = x + 0.5f - hw, cy = y + 0.5f - hh; // y = 0 é a base da textura
                    float rr = cx >= 0f ? (cy >= 0f ? r[1] : r[2]) : (cy >= 0f ? r[0] : r[3]);
                    float qx = Mathf.Abs(cx) - hw + rr, qy = Mathf.Abs(cy) - hh + rr;
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - rr;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (borda > 0) a *= Mathf.Clamp01(0.5f + d + borda);
                    if (tracejada)
                    {
                        bool meioX = x >= m && x < m + meio, meioY = y >= m && y < m + meio;
                        if (meioX && !meioY && (x - m) >= traco) a = 0f;
                        if (meioY && !meioX && (y - m) >= traco) a = 0f;
                    }
                    px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        });
    }

    Sprite SpriteCirculo()
    {
        return CarregarOuCriar("circulo", Vector4.zero, false, () =>
        {
            const int TAM = 64;
            var tex = NovaTextura(TAM, TAM);
            var px = new Color32[TAM * TAM];
            for (int y = 0; y < TAM; y++)
                for (int x = 0; x < TAM; x++)
                {
                    float dx = x + 0.5f - TAM / 2f, dy = y + 0.5f - TAM / 2f;
                    float a = Mathf.Clamp01(TAM / 2f - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * TAM + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        });
    }

    Sprite SpriteBrilho()
    {
        return CarregarOuCriar("brilho", Vector4.zero, false, () =>
        {
            const int TAM = 128;
            var tex = NovaTextura(TAM, TAM);
            var px = new Color32[TAM * TAM];
            for (int y = 0; y < TAM; y++)
                for (int x = 0; x < TAM; x++)
                {
                    float dx = x + 0.5f - TAM / 2f, dy = y + 0.5f - TAM / 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / (TAM / 2f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                    px[y * TAM + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        });
    }

    Sprite SpriteGrade()
    {
        return CarregarOuCriar("grade_120", Vector4.zero, true, () =>
        {
            const int TAM = 120;
            var tex = NovaTextura(TAM, TAM);
            var px = new Color32[TAM * TAM];
            for (int y = 0; y < TAM; y++)
                for (int x = 0; x < TAM; x++)
                    px[y * TAM + x] = new Color32(255, 255, 255, (byte)(x == 0 || y == TAM - 1 ? 255 : 0));
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        });
    }

    // ------------------------------------------------------------------ RectTransform

    static GameObject NovoUI(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(pai, false);
        return go;
    }

    static RectTransform Esticar(GameObject go, float margem = 0f)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-margem, -margem);
        rt.offsetMax = new Vector2(margem, margem);
        rt.localEulerAngles = Vector3.zero;
        return rt;
    }

    static GameObject Retangulo(string nome, Transform pai, float x, float y, float w, float h)
    {
        var go = NovoUI(nome, pai);
        PosicionarXY((RectTransform)go.transform, x, y, w, h, 0f, false);
        return go;
    }

    static void Posicionar(RectTransform rt, Dictionary<string, object> no, float w, float h, float rot, bool pivoCentro)
    {
        var pl = D(no, "posicaoLocal");
        PosicionarXY(rt, F(pl, "x"), F(pl, "y"), w, h, rot, pivoCentro);
    }

    // x, y = canto superior esquerdo relativo ao canto superior esquerdo do pai (y para baixo)
    static void PosicionarXY(RectTransform rt, float x, float y, float w, float h, float rot, bool pivoCentro)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        if (pivoCentro || Mathf.Abs(rot) > 0.001f)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w / 2f, -(y + h / 2f));
        }
        else
        {
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }
        rt.localEulerAngles = new Vector3(0f, 0f, rot);
    }

    static Vector2 Desloc(Dictionary<string, object> d)
    {
        return d == null ? Vector2.zero : new Vector2(F(d, "x"), -F(d, "y"));
    }

    // ------------------------------------------------------------------ leitura do JSON

    static object Obj(Dictionary<string, object> d, string k)
    {
        object v;
        return d != null && d.TryGetValue(k, out v) ? v : null;
    }
    static Dictionary<string, object> D(Dictionary<string, object> d, string k) { return Obj(d, k) as Dictionary<string, object>; }
    static List<object> L(Dictionary<string, object> d, string k) { return Obj(d, k) as List<object>; }
    static string S(Dictionary<string, object> d, string k) { return Obj(d, k) as string; }
    static float F(Dictionary<string, object> d, string k, float padrao = 0f)
    {
        var v = Obj(d, k);
        return v is double ? (float)(double)v : padrao;
    }
    static bool B(Dictionary<string, object> d, string k, bool padrao)
    {
        var v = Obj(d, k);
        return v is bool ? (bool)v : padrao;
    }
    static float Num(object o) { return o is double ? (float)(double)o : 0f; }

    static Color Cor(object v, Color padrao)
    {
        var s = v as string;
        if (s == null) return padrao;
        if (s == "transparente") return Color.clear;
        Color c;
        return ColorUtility.TryParseHtmlString(s, out c) ? c : padrao;
    }
}

// Leitor de JSON simples (o JsonUtility do Unity não lê estruturas livres como esta).
static class MiniJson
{
    public static object Parse(string s)
    {
        int i = 0;
        return Valor(s, ref i);
    }

    static void Espacos(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    static object Valor(string s, ref int i)
    {
        Espacos(s, ref i);
        if (i >= s.Length) return null;
        char c = s[i];
        if (c == '{')
        {
            i++;
            var d = new Dictionary<string, object>();
            Espacos(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                Espacos(s, ref i);
                string chave = Texto(s, ref i);
                Espacos(s, ref i);
                i++; // ':'
                d[chave] = Valor(s, ref i);
                Espacos(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++; // '}'
                return d;
            }
        }
        if (c == '[')
        {
            i++;
            var l = new List<object>();
            Espacos(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Valor(s, ref i));
                Espacos(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++; // ']'
                return l;
            }
        }
        if (c == '"') return Texto(s, ref i);
        if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
        if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
        if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
        int ini = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        return double.Parse(s.Substring(ini, i - ini), CultureInfo.InvariantCulture);
    }

    static string Texto(string s, ref int i)
    {
        var sb = new StringBuilder();
        i++; // abre aspas
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') break;
            if (c != '\\') { sb.Append(c); continue; }
            char e = s[i++];
            switch (e)
            {
                case 'n': sb.Append('\n'); break;
                case 't': sb.Append('\t'); break;
                case 'r': break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                default: sb.Append(e); break;
            }
        }
        return sb.ToString();
    }
}
#endif
