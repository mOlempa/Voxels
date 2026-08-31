using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Klasa definiuj¹ca atrybut pozwalaj¹cy na podœwietlanie zmiennych w inspektorze w wybranych kolorach.
 */
public class HighlightAttribute : PropertyAttribute
{
    /**
     * Zmienna typu Color okreœlaj¹ca kolor podœwietlenia.
     */
    public Color col;

    /**
     * Atrybut, który mo¿na dodaæ do zmiennych a celu ich podœwietlenia w inspektorze.
     */
    public HighlightAttribute(float r = 1, float g = 0, float b = 0)
    {
        this.col = new Color(r, g, b, 1);
    }
}

/**
 * Klasa pozwalaj¹ca na podœwietlanie zmiennych w inspektorze w wybranych kolorach.
 */
[CustomPropertyDrawer(typeof(HighlightAttribute))]
public class HighlightPropertyDrawer : PropertyDrawer
{
    /**
     * Metoda nadpisuj¹ca metodê OnGUI w celu dodania mo¿liwoœci podœwietlenia zmiennych w inspektorze.
     */
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var col = (attribute as HighlightAttribute).col;
        Color prev = GUI.color;
        GUI.color = col;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.color = prev;

    }
}