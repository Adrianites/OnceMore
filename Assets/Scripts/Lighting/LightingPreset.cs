using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "LightingPreset", menuName = "ScriptableObjects/LightingPreset", order = 1)]
public class LightingPreset : ScriptableObject
{
    public Gradient ambientColour;
    public Gradient directionalColour;
    public Gradient fogColour;
}
