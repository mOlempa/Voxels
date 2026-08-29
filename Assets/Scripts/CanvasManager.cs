using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI dateTimeText;

    private void Start()
    {
        dateTimeText.text = DateTime.Now.ToString("ddMMyy-HHmmss");
    }
}
