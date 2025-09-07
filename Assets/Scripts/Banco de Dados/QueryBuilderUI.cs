using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class QueryBuilderUI : MonoBehaviour
{
    [Header("Tokens do desafio")]
    public string[] availableTokens;

    [Header("Referências")]
    public GameObject tokenPrefab;
    public Transform poolContainer;
    public Transform assemblyContainer;
    public TextMeshProUGUI assembledText;
    private RectTransform textRT;

    public Button backspaceButton;
    public Button clearButton;
    public Button executeButton;
    public Button closeButton;
    public SQLConsoleUI sqlConsoleUI;

    List<string> tokens = new List<string>();
    string[] allowedTables;
    Delegate validator;

    void Awake()
    {
        gameObject.SetActive(false);
        textRT = assembledText.rectTransform;
        assembledText.margin = new Vector4(0, 12, 0, 30);  
        

        backspaceButton.onClick.AddListener(RemoveLast);
        clearButton.onClick.AddListener(ClearAll);
        executeButton.onClick.AddListener(OnExecute);
        closeButton.onClick.AddListener(OnClose);
    }

    public void Open(string[] allowedTables, Delegate validator)
    {
        this.allowedTables = allowedTables;
        this.validator = validator;

        RefreshPool();
        tokens.Clear();
        UpdateAssembly();
        gameObject.SetActive(true);

        sqlConsoleUI.Open(allowedTables, validator);
    }

    void RefreshPool()
    {
        foreach (Transform c in poolContainer) Destroy(c.gameObject);
        foreach (var t in availableTokens)
        {
            var go = Instantiate(tokenPrefab, poolContainer);
            go.GetComponent<TokenButton>().Init(t, AddToken);
        }
    }

    void AddToken(string t)
    {
        tokens.Add(t);
        UpdateAssembly();
    }

    void RemoveLast()
    {
        if (tokens.Count > 0) tokens.RemoveAt(tokens.Count - 1);
        UpdateAssembly();
    }

    void ClearAll()
    {
        tokens.Clear();
        UpdateAssembly();
    }

    void UpdateAssembly()
    {

        string rawSql = string.Join(" ", tokens);

      
        string displaySql = System.Text.RegularExpressions.Regex.Replace(
            rawSql,
            @"\b(FROM|WHERE|INNER|LEFT|GROUP BY|HAVING|LIMIT)\b",
            "\n$1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        ).TrimStart('\n');

       
        assembledText.text = displaySql;
        assembledText.margin = new Vector4(0, 12, 0, 30);
     
        Vector2 pref = assembledText.GetPreferredValues(displaySql);
        textRT.sizeDelta = new Vector2(pref.x, pref.y);

        LayoutRebuilder.ForceRebuildLayoutImmediate(textRT);

      
        foreach (Transform c in assemblyContainer) Destroy(c.gameObject);
        foreach (var t in tokens)
        {
            var go = Instantiate(tokenPrefab, assemblyContainer);
            go.GetComponent<TokenButton>().Init(t, _ => { });
        }
    }

    public void OnExecute()
    {
        
        string rawSql = string.Join(" ", tokens);
        sqlConsoleUI.ExecuteRaw(rawSql);
    }

    public void OnClose()
    {
        sqlConsoleUI.Close();
        gameObject.SetActive(false);
    }
}