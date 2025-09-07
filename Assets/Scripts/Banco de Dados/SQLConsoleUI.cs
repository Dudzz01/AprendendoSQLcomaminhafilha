using UnityEngine;
using TMPro;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Models;
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class SQLConsoleUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;        
    [SerializeField] private TMP_InputField inputField; 
    [SerializeField] private TMP_Text feedbackText;

    [Header("Result Grid")]
    [SerializeField] private ScrollRect scrollRect;     
    [SerializeField] private RectTransform content;     
    [SerializeField] private GameObject rowPrefab;      


    private DatabaseManager db;
    private string[] allowedTables;
    private Delegate validator;
    private GameObject playerObj;

    private List<string> selectedColumns = new List<string>();

    private GameObject inputArea;

    void Start()
    {
        db = FindObjectOfType<DatabaseManager>();
        panel.SetActive(false);
    }

   
    public void Open(string[] tables, Delegate validator)
    {
        this.allowedTables = tables;
        this.validator = validator;
        inputField.text = "";
        feedbackText.text = "";
        panel.SetActive(true);
        inputField.ActivateInputField();
        Transform t = panel.transform.Find("InputArea");

        if (t != null)
        {
            inputArea = t.gameObject;

        }
        inputArea.SetActive(true);
        panel.GetComponent<Image>().enabled = true;
        playerObj = GameObject.FindGameObjectWithTag("Player");
        playerObj.GetComponent<Player>().GetComponent<Player>().enabled = false;
    }

   
    public void Close()
    {

        Transform t = panel.transform.Find("InputArea");
        
        if (t != null)
        {
            inputArea = t.gameObject;

        }
        else
        {
            
        }


        if (inputArea != null)

        {
            inputArea.SetActive(false);
            panel.GetComponent<Image>().enabled = false;
        }
        playerObj.GetComponent<Player>().GetComponent<Player>().enabled = true;
       
    }

    private IEnumerator CloseAfterDelay(float secs)
    {
        yield return new WaitForSeconds(secs);
        Close();
    }



    // DTO para resultados de INNER JOIN Itens+Moveis+Comodos
    public class ItemMovelComodoDTO
    {
        public string NomeItem { get; set; }
        public string Dica { get; set; }
        public string NomeMovel { get; set; }
        public string NomeComodo { get; set; }
    }

    public void OnExecute()
    {
        string sql = inputField.text.Trim();

        
        string norm = Regex.Replace(sql, @"\s+", " ").Trim();
        norm = Regex.Replace(norm, @"\s*\.\s*", ".");

        
        selectedColumns = ExtractSelectedColumns(norm);
        if (selectedColumns.Count == 0)
        {
            feedbackText.text = "Você deve selecionar colunas no SELECT.";
            return;
        }
        if (!ValidateTables(norm))
        {
            feedbackText.text = "Consulta contém tabela não permitida.";
            return;
        }
        var tables = ExtractTables(norm);
        if (tables.Count == 0)
        {
            feedbackText.text = "Nenhuma tabela em FROM/JOINS detectada.";
            return;
        }

        
        var bareCols = selectedColumns
    .Select(c => {
        
        var m = Regex.Match(c, @"\bAS\s+(\w+)$", RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value;

        
        if (c.Contains("."))
            return c.Split('.').Last();

       
        return c;
    })
    .ToList();

        bool isThreeWayJoin =
            bareCols.Contains("NomeItem") &&
            bareCols.Contains("Dica") &&
            bareCols.Contains("NomeMovel") &&
            bareCols.Contains("NomeComodo");

        bool isCountChaves = bareCols.Contains("IdMovel")
                           && bareCols.Contains("QtdItens")
                           && bareCols.Contains("QtdChaves");

        
        Type resultType;
        if (isThreeWayJoin)
        {
            resultType = typeof(ItemMovelComodoDTO);
        }
        else if (isCountChaves)
        {
            resultType = typeof(ItensCountChavesResult);
        }
        else
        {
            resultType = MapTableNameToType(ExtractTables(norm)[0]);
        }

        
        var mFrom = Regex.Match(norm, @"\bFROM\b", RegexOptions.IgnoreCase);
        string selectPart = norm.Substring(0, mFrom.Index);
        string fromPart = norm.Substring(mFrom.Index);

     
        var cols = selectPart
            .Substring(6)  
            .Split(',')
            .Select(c => c.Trim())
            .Select(c => {
                if (!c.Contains('.') || Regex.IsMatch(c, @"\bAS\b", RegexOptions.IgnoreCase))
                    return c;
                var parts = c.Split('.');
                return $"{c} AS {parts[1]}";
            })
            .ToArray();

        
        string dbSql = "SELECT " + string.Join(", ", cols) + " " + fromPart;
        Debug.Log($"[SQL → DB] {dbSql}");

        try
        {
            
            var exec = typeof(DatabaseManager)
                .GetMethod("ExecuteQuery", BindingFlags.Public | BindingFlags.Instance);
            var gen = exec.MakeGenericMethod(resultType);
            var itemsObj = gen.Invoke(db, new object[] { dbSql });

           
            bool valid;
            int pCount = validator.Method.GetParameters().Length;
            valid = pCount == 2
                ? (bool)validator.DynamicInvoke(norm, itemsObj)
                : (bool)validator.DynamicInvoke(itemsObj);

          
            var en = (IEnumerable)itemsObj;
            feedbackText.text = valid ? "✔ Consulta correta!" : "✖ Consulta incorreta!";
            if (valid)
            {
                if (selectedColumns.Count == 1 && selectedColumns[0] == "*")
                    RenderGridWithAllProperties(en);
                else
                    RenderGrid(en);
                Close();
            }
        }
        catch (TargetInvocationException tie)
        {
            feedbackText.text = "Erro ao executar a consulta: " + tie.InnerException.Message;
            Debug.LogError(tie.InnerException);
        }
        catch (Exception ex)
        {
            feedbackText.text = "Erro na query: " + ex.Message;
            Debug.LogError(ex);
        }
    }



    public void ExecuteRaw(string sql)
    {
        
        inputField.text = sql;
        
        OnExecute();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            panel.SetActive(false);
            panel.GetComponent<Image>().enabled = true;
            playerObj.GetComponent<Player>().GetComponent<Player>().enabled = true;
             content.DetachChildren();                
            scrollRect.gameObject.SetActive(false);  
        }

    }

    


    private List<string> ExtractTables(string sql)
    {
        var tables = new List<string>();
        var regex = new Regex(@"\bFROM\s+([^\s,]+)|\bJOIN\s+([^\s,]+)",
                               RegexOptions.IgnoreCase);
        foreach (Match m in regex.Matches(sql))
        {
            string tbl = !string.IsNullOrEmpty(m.Groups[1].Value)
                       ? m.Groups[1].Value
                       : m.Groups[2].Value;
            tbl = tbl.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (!tables.Contains(tbl, StringComparer.OrdinalIgnoreCase))
                tables.Add(tbl);
        }
        return tables;
    }

    private List<string> ExtractSelectedColumns(string sql)
    {
        var match = Regex.Match(sql, @"^\s*SELECT\s+(?<cols>.+?)\s+FROM", RegexOptions.IgnoreCase);
        if (!match.Success)
            return new List<string>();

        return match.Groups["cols"].Value
            .Split(',')
            .Select(c => c.Trim())
            .Where(c => c.Length > 0)
            .ToList();
    }



    private bool ValidateTables(string sql)
    {
        var regex = new Regex(@"\b(FROM|JOIN)\s+([A-Za-z0-9_]+)",
                              RegexOptions.IgnoreCase);
        foreach (Match m in regex.Matches(sql))
        {
            string tbl = m.Groups[2].Value;
            if (!allowedTables.Any(a =>
                a.Equals(tbl, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }

    
    private Type MapTableNameToType(string table)
    {
        Debug.Log("Selecionando tipo raiz.");
        switch (table.ToLower())
        {
            case "itens": return typeof(Item);
            case "moveis": return typeof(Movel);
            case "casas": return typeof(Casa);
            case "comodos": return typeof(Comodo);
            default:
                throw new ArgumentException($"Nenhum tipo mapeado para '{table}'");
        }
    }

   
   
   
    private void EnsureCellCount(Transform rowTf, int needed)
    {
        int diff = needed - rowTf.childCount;
        if (diff <= 0) return;                 

       
        Transform template = rowTf.GetChild(0);
        for (int i = 0; i < diff; i++)
        {
            Transform clone = Instantiate(template, rowTf);
            clone.name = $"Col{rowTf.childCount - 1}";
        }
    }

    private void RenderGrid(IEnumerable list)
    {
        
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var enumer = list.GetEnumerator();
        if (!enumer.MoveNext())
        {
            
            GameObject placeholder = Instantiate(rowPrefab, content);
            EnsureCellCount(placeholder.transform, selectedColumns.Count);

            
            var cellText = placeholder.transform.GetChild(0).GetComponent<TMP_Text>();
            cellText.text = "Nenhum registro encontrado.";
            cellText.alignment = TextAlignmentOptions.Center;

            
            for (int i = 1; i < selectedColumns.Count; i++)
                placeholder.transform.GetChild(i).gameObject.SetActive(false);

            
            scrollRect.gameObject.SetActive(true);
            return;
        }

        
        GameObject head = Instantiate(rowPrefab, content);
        EnsureCellCount(head.transform, selectedColumns.Count);
        for (int i = 0; i < selectedColumns.Count; i++)
        {
           
            var raw = selectedColumns[i].Trim().TrimEnd(';');

            string name;
            
            var m = Regex.Match(raw, @"\bAS\s+(\w+)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                name = m.Groups[1].Value;
            }
            else if (raw.Contains("."))
            {
                
                name = raw.Split('.').Last();
            }
            else
            {
                
                name = raw;
            }

            head.transform.GetChild(i)
                .GetComponent<TMP_Text>()
                .text = $"<b>{name}</b>";
        }

        // primeira linha de dados
        CreateDynamicRowProp(enumer.Current);

        // demais linhas
        while (enumer.MoveNext())
            CreateDynamicRowProp(enumer.Current);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1;
        scrollRect.gameObject.SetActive(true);
    }


    /*
    private void RenderGrid(IEnumerable list)
    {
        // limpa linhas antigas
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var enumer = list.GetEnumerator();
        if (!enumer.MoveNext())
        {
            feedbackText.text += "\nNenhum registro encontrado.";
            return;
        }

        // cabeçalho só com os nomes "puros"
        GameObject head = Instantiate(rowPrefab, content);
        EnsureCellCount(head.transform, selectedColumns.Count);
        for (int i = 0; i < selectedColumns.Count; i++)
        {
            // extrai depois do ponto, se houver
            var raw = selectedColumns[i];
            var name = raw.Contains(".")
                ? raw.Split('.').Last()
                : raw;
            head.transform.GetChild(i).GetComponent<TMP_Text>().text =
                $"<b>{name}</b>";
        }

        // primeira linha de dados
        CreateDynamicRowProp(enumer.Current);

        // demais linhas
        while (enumer.MoveNext())
            CreateDynamicRowProp(enumer.Current);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1;
        scrollRect.gameObject.SetActive(true);
    }
    */

    private void RenderGridWithAllProperties(IEnumerable list)
    {

        foreach (Transform child in content)
            Destroy(child.gameObject);


        var enumer = list.GetEnumerator();
        if (!enumer.MoveNext())
        {
            feedbackText.text += "\nNenhum registro encontrado.";
            return;
        }


        object first = enumer.Current;
        var props = first.GetType()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance);


        GameObject head = Instantiate(rowPrefab, content);
        EnsureCellCount(head.transform, props.Length);
        for (int i = 0; i < props.Length; i++)
            head.transform.GetChild(i).GetComponent<TMP_Text>().text =
                $"<b>{props[i].Name}</b>";


        CreateDynamicRow(first, props);


        while (enumer.MoveNext())
            CreateDynamicRow(enumer.Current, props);


        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1;
        scrollRect.gameObject.SetActive(true);
    }


    private void CreateDynamicRow(object item, PropertyInfo[] props)
    {
        GameObject row = Instantiate(rowPrefab, content);
        EnsureCellCount(row.transform, props.Length);

        for (int i = 0; i < props.Length; i++)
        {
            object v = props[i].GetValue(item, null);
            row.transform.GetChild(i).GetComponent<TMP_Text>().text =
                v != null ? v.ToString() : "null";
        }
    }
    /*
    private void CreateDynamicRowProp(object item)
    {
        GameObject row = Instantiate(rowPrefab, content);
        EnsureCellCount(row.transform, selectedColumns.Count);

        for (int i = 0; i < selectedColumns.Count; i++)
        {
            // extrai a parte depois do ponto para encontrar a propriedade
            var raw = selectedColumns[i];
            var propName = raw.Contains(".")
                ? raw.Split('.').Last()
                : raw;

            var prop = item.GetType().GetProperty(propName);
            object v = prop != null ? prop.GetValue(item, null) : null;
            row.transform.GetChild(i).GetComponent<TMP_Text>().text =
                v != null ? v.ToString() : string.Empty;
        }
    }*/
    private void CreateDynamicRowProp(object item)
    {
       
        GameObject row = Instantiate(rowPrefab, content);
        EnsureCellCount(row.transform, selectedColumns.Count);

        for (int i = 0; i < selectedColumns.Count; i++)
        {
            
            var raw = selectedColumns[i].Trim().TrimEnd(';');

           
            var m = Regex.Match(raw, @"\bAS\s+(\w+)$", RegexOptions.IgnoreCase);
            string propName;
            if (m.Success)
            {
                propName = m.Groups[1].Value;           
            }
            else if (raw.Contains("."))
            {
                propName = raw.Split('.').Last();        
            }
            else
            {
                propName = raw;                          
            }

            
            var prop = item.GetType().GetProperty(propName);
            var v = prop != null ? prop.GetValue(item, null) : null;
            row.transform.GetChild(i)
               .GetComponent<TMP_Text>()
               .text = v != null ? v.ToString() : string.Empty;
        }
    }





}
