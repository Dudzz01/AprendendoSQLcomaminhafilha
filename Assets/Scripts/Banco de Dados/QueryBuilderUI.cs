using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class QueryBuilderUI : MonoBehaviour
{
    [Header("Tokens do desafio")]
    [SerializeField] public string[] availableTokens;

    [Header("Refer�ncias")]
    [SerializeField] private GameObject tokenPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private Transform assemblyContainer;
    [SerializeField] private TextMeshProUGUI assembledText;

    [Header("Controles")]
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button closeButton;

    [Header("Execu��o")]
    [SerializeField] private SQLConsoleUI sqlConsoleUI;

    private readonly List<string> _tokens = new List<string>();
    private RectTransform _assembledTextRT;

    // ========================
    // Ciclo de vida
    // ========================
    private void Awake()
    {
        gameObject.SetActive(false);

        if (assembledText != null)
        {
            _assembledTextRT = assembledText.rectTransform;
            assembledText.margin = new Vector4(0, 12, 0, 30);
        }

        if (backspaceButton) backspaceButton.onClick.AddListener(RemoveLast);
        if (clearButton) clearButton.onClick.AddListener(ClearAll);
        if (executeButton) executeButton.onClick.AddListener(OnExecute);
        if (closeButton) closeButton.onClick.AddListener(OnClose);
    }

    // ========================
    // API nova (UI pura)
    // ========================
    /// <summary>Mostra o builder. Opcionalmente substitui os tokens dispon�veis desta fase.</summary>
    public void Show(string[] tokensForPhase = null)
    {
        if (tokensForPhase != null) availableTokens = tokensForPhase;

        RefreshPool();
        _tokens.Clear();
        UpdateAssembly();
        gameObject.SetActive(true);
    }

    /// <summary>Esconde apenas o builder (n�o fecha o console).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Atualiza a lista de tokens dispon�veis (sem abrir a UI).</summary>
    public void SetAvailableTokens(string[] tokensForPhase)
    {
        availableTokens = tokensForPhase ?? Array.Empty<string>();
        if (gameObject.activeSelf) RefreshPool();
    }

    // ========================
    // Compat (legado)
    // ========================
    /// <summary>
    /// Compat: assinatura antiga usada pelo PlayerInteraction legado.
    /// Agora s� mostra a UI do builder. N�O abre o console.
    /// </summary>
    public void Open(string[] _ignoredAllowedTables, Delegate _ignoredValidator)
    {
        Show(); // apenas UI
    }

    // ========================
    // Montagem
    // ========================
    private void RefreshPool()
    {
        if (!poolContainer || !tokenPrefab) return;

        foreach (Transform c in poolContainer) Destroy(c.gameObject);

        var list = availableTokens ?? Array.Empty<string>();
        foreach (var t in list)
        {
            var go = Instantiate(tokenPrefab, poolContainer);
            var btn = go.GetComponent<TokenButton>();
            if (btn != null) btn.Init(t, AddToken);
        }
    }

    private void UpdateAssembly()
    {
        string rawSql = string.Join(" ", _tokens);

        // Quebra visual por palavras-chave (apenas est�tica)
        string displaySql = Regex.Replace(
            rawSql,
            @"\b(SELECT|FROM|WHERE|INNER|LEFT|RIGHT|JOIN|ON|GROUP BY|HAVING|ORDER BY|LIMIT|ALTER|ADD|DROP|COLUMN|,|ID|NOME|CATEGORIA|COR|MATERIAL|IDADE_RECOMENDADA|DATA_CADASTRO|ATIVO)\b",
            "\n$1",
            RegexOptions.IgnoreCase
        ).TrimStart('\n');

        if (assembledText)
        {
            assembledText.text = displaySql;
            assembledText.margin = new Vector4(0, 12, 0, 30);

            if (_assembledTextRT)
            {
                Vector2 pref = assembledText.GetPreferredValues(displaySql);
                _assembledTextRT.sizeDelta = new Vector2(pref.x, pref.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_assembledTextRT);
            }
        }

        if (!assemblyContainer || !tokenPrefab) return;

        foreach (Transform c in assemblyContainer) Destroy(c.gameObject);
        foreach (var t in _tokens)
        {
            var go = Instantiate(tokenPrefab, assemblyContainer);
            var btn = go.GetComponent<TokenButton>();
            if (btn != null) btn.Init(t, _ => { /* r�plica visual, sem a��o */ });
        }
    }

    // ========================
    // Eventos UI
    // ========================
    private void AddToken(string t)
    {
        _tokens.Add(t);
        UpdateAssembly();
    }

    private void RemoveLast()
    {
        if (_tokens.Count > 0) _tokens.RemoveAt(_tokens.Count - 1);
        UpdateAssembly();
    }

    private void ClearAll()
    {
        _tokens.Clear();
        UpdateAssembly();
    }

    public void OnExecute()
    {
        if (!sqlConsoleUI)
        {
            Debug.LogError("[QueryBuilderUI] SQLConsoleUI n�o atribu�do.");
            return;
        }

        string rawSql = string.Join(" ", _tokens).Trim();
        sqlConsoleUI.ExecuteRaw(rawSql);
    }

    /// <summary>
    /// Compat: por padr�o fecha o console e esconde o builder,
    /// pois seu PlayerInteraction atual chama s� isso.
    /// </summary>
    public void OnClose()
    {
        if (sqlConsoleUI) sqlConsoleUI.Close(); // mant�m compat
        Hide();
    }
}
