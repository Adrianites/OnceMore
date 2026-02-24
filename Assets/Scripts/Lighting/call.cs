using UnityEngine;

public class call : MonoBehaviour
{
    public LightingManager lightingManager;

    private void OnTriggerEnter(Collider other)
    {
        if (lightingManager != null)
        {
            lightingManager.TriggerNightTransition();
        }
    }
}
