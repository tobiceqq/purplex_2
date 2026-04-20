using UnityEngine;
using TMPro; 

public class PlayerStats : MonoBehaviour
{
    public int energy;
    public bool hasHyperRoll = false;

    [Header("UI")]
    public TextMeshProUGUI energyText;

    private void Start()
    {
        UpdateUI(); 
    }

    public void AddEnergy(int amount)
    {
        energy += amount;
        UpdateUI();
    }
    public void RemoveEnergy(int amount)
    {
        energy -= amount;
        UpdateUI(); 
    }
    public void ActivateHyperRoll()
    {
        hasHyperRoll = true;
    }

    public void UpdateUI()
    {
        if (energyText != null)
        {
            energyText.text = "Energie: " + energy;
            Debug.Log("UI se aktualizuje! Nová hodnota: " + energy);
        }
        else
        {
            Debug.LogWarning("POZOR: EnergyText není v Inspectoru pøiøazen!");
        }
    }
}