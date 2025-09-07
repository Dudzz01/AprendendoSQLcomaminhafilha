using System;
using UnityEngine;
using Models;
using TMPro;  

public class FurnitureInteractable : MonoBehaviour
{
    [Header("Identificação")]
    public string movableName;       

    [Header("Contexto SQL")]
    public string[] allowedTables;
    public Delegate validator;
    public SQLConsoleUI sqlUI;
    [Header("Blocos SQL deste móvel")]
    public string[] tokens;

    [Header("TextosEnunciados")]
    
    public string textoEnunciado;

   

}
