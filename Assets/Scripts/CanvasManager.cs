using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * Klasa odpowiedzialna za dodatkowe elementy interfejsu u¿ytkownika widzianego z kamery.
 */
public class CanvasManager : MonoBehaviour
{
    /**
     * Referencja do obiektu tekstu.
     */
    [SerializeField]
    TextMeshProUGUI dateTimeText;

    /**
     * Metoda automatycznie wywo³ywana po uruchomieniu aplikacji, ustawiaj¹ca datê jako tekst widziany z kamery. 
     */
    private void Start()
    {
        dateTimeText.text = DateTime.Now.ToString("ddMMyy-HHmmss");
    }
}
