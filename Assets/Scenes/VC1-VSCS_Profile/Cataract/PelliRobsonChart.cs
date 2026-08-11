using UnityEngine;
using TMPro; // Requires TextMeshPro
using System.Collections.Generic;

public class PelliRobsonChart : MonoBehaviour
{
    [Header("Chart Configuration")]
    [Tooltip("The letters to display. Pelli-Robson uses Sloan letters (C, D, H, K, N, O, R, S, V, Z).")]
    public string overrideSequence = "VRS KDR NHC SVZ DKO CNZ RHV ZSO"; // Standard start
    
    [Tooltip("Starting Log Contrast Sensitivity (Usually 0.00 for top left).")]
    public float startLogCS = 0.00f;
    
    [Tooltip("Step size per triplet (Standard is 0.15).")]
    public float logCSStep = 0.15f;

    [Header("Display Settings")]
    public TMP_Text textComponent; // The TextMeshPro component to control
    public Color backgroundColor = Color.white; // The background luminance (L_max)

    [Header("Debug")]
    public bool updateInRealtime = true;

    void Start()
    {
        GenerateChart();
    }

    void OnValidate()
    {
        if (updateInRealtime) GenerateChart();
    }

    public void GenerateChart()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null) return; // Wait for assignment
        }

        // Clean up text
        textComponent.text = "";
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.richText = true; // We use Rich Text <color> tags

        // Parse Sequence
        // Split by spaces to get triplets
        string[] triplets = overrideSequence.Split(' ');
        
        float currentLogCS = startLogCS;
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (string triplet in triplets)
        {
            // Calculate Contrast
            // Clinical Definition (Weber Contrast): C = (L_back - L_fore) / L_back
            // LogCS = log10(1/C) = -log10(C)
            // => C = 10^(-LogCS)
            
            float contrast = Mathf.Pow(10, -currentLogCS);
            
            // Limit C to 1.0 (max contrast)
            contrast = Mathf.Clamp01(contrast);

            // Calculate Foreground Luminance (L_fore)
            // L_fore = L_back * (1 - C)
            // Assuming simplified Grayscale model where Color Value = Luminance
            
            float L_back = backgroundColor.grayscale; // Usually 1.0
            float L_fore = L_back * (1.0f - contrast);
            
            // Convert to Color
            Color letterColor = new Color(L_fore, L_fore, L_fore, 1.0f);
            
            // Convert to Hex for Rich Text
            string hexColor = ColorUtility.ToHtmlStringRGB(letterColor);
            
            // Append with Color Tag
            sb.Append($"<color=#{hexColor}>{triplet}</color> ");
            
            // Advance Step
            currentLogCS += logCSStep;
            
            // Optional: New line every 2 triplets (standard Pelli-Robson layout)
            // Or let TextMeshPro handle wrapping width
        }

        textComponent.text = sb.ToString();
    }
    
    // Helper to calculate Contrast of a specific logCS
    public float GetContrast(float logCS)
    {
        return Mathf.Pow(10, -logCS);
    }
}
