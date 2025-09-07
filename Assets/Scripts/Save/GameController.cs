using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static Save s = new Save();
    private static GameObject gmController;
    private void Awake()
    {
        Application.targetFrameRate = 60;
        s.arrayFasesDesbloqueadas[0] = true; // fase 1
        Debug.Log("Desafios Concluidos: " + s.desafiosConcluidos);
        DontDestroyOnLoad(this.transform.root.gameObject);

        if (gmController == null)
        {
            gmController = this.transform.root.gameObject;
        }
        else
        {
            Destroy(this.transform.root.gameObject);
        }
    }

    private void Update()
    {


        switch (s.desafiosConcluidos)
        {
            case 3:
                if(s.arrayFasesDesbloqueadas[1] == false)
                {
                    s.arrayFasesDesbloqueadas[1] = true; // fase 2
                    GetComponent<SaveGame>().SaveGameOfScene(s);
                }
                
                break;
            case 7:
                if(s.arrayFasesDesbloqueadas[2] == false)
                {
                    s.arrayFasesDesbloqueadas[2] = true; // fase 3
                    GetComponent<SaveGame>().SaveGameOfScene(s);
                }
                break;
            case 11:
                if (s.arrayFasesDesbloqueadas[3] == false)
                {
                    s.arrayFasesDesbloqueadas[3] = true; // fase 4
                    GetComponent<SaveGame>().SaveGameOfScene(s);
                }

                break;
            case 17:
                if (s.arrayFasesDesbloqueadas[4] == false)
                {
                    s.arrayFasesDesbloqueadas[4] = true; // fase 5
                    GetComponent<SaveGame>().SaveGameOfScene(s);
                }

                break;

            case 20:
                if (s.arrayFasesDesbloqueadas[5] == false)
                {
                    s.arrayFasesDesbloqueadas[5] = true; // fase final
                    GetComponent<SaveGame>().SaveGameOfScene(s);
                }

                break;
            case 23:
                if (s.arrayFasesDesbloqueadas[6] == false)
                {
                    s.arrayFasesDesbloqueadas[6] = true; // fase final
                    GetComponent<SaveGame>().SaveGameOfScene(s);
                }

                break;

        }
    }




}




