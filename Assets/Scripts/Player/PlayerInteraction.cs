using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuração de Interação")]
    [SerializeField] float interactRadius = 2f;
    [SerializeField] KeyCode interactKey = KeyCode.LeftControl;

    [Header("Referências de UI")]
    [SerializeField] GameObject promptUI;           // painel que contém o texto
    [SerializeField] TextMeshProUGUI promptText;    
    [SerializeField] Button closeConsoleButton;     

    [Header("Modo Blocos")]
    [SerializeField] private QueryBuilderUI queryBuilderUI;  


    private FurnitureInteractable nearestFI;
    private bool consoleOpen = false;                

    void Start()
    {
        if (closeConsoleButton != null)
            closeConsoleButton.onClick.AddListener(CloseConsole);
    }

    void Update()
    {
        
        var all = FindObjectsOfType<FurnitureInteractable>();

       
        var inRange = all
            .Select(fi => new {
                fi,
                dist = Vector3.Distance(transform.position, fi.transform.position)
            })
            .Where(x => x.dist <= interactRadius)
            .OrderBy(x => x.dist)
            .FirstOrDefault();

        if (inRange != null)
        {
            nearestFI = inRange.fi;

            
            if (!consoleOpen)
            {
                promptUI.SetActive(true);
                promptText.text = $"Pressione CTRL para acessar {nearestFI.movableName}";
            }


            if (Input.GetKeyDown(interactKey))
            {
                
                promptUI.SetActive(false);
                consoleOpen = true;
                Debug.Log(nearestFI.textoEnunciado);

                

                
               
                queryBuilderUI.availableTokens = nearestFI.tokens;
                

               
                queryBuilderUI.Open(nearestFI.allowedTables, nearestFI.validator);
                EnunciadoUIManager.I.Show(nearestFI.textoEnunciado);
            }
        }
        else
        {
            nearestFI = null;
            promptUI.SetActive(false);
        }

        
        if (Input.GetKeyDown(KeyCode.Escape) && consoleOpen)
            CloseConsole();
    }

    private void CloseConsole()
    {
        queryBuilderUI.OnClose();
        consoleOpen = false;

        if (nearestFI != null &&
            Vector3.Distance(transform.position, nearestFI.transform.position) <= interactRadius)
        {
            promptUI.SetActive(true);
            promptText.text = $"Pressione CTRL para acessar {nearestFI.movableName}";
        }
        else
        {
            promptUI.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
