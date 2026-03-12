using UnityEngine;

public class DestroyThis : MonoBehaviour
{
    public float destroyDelay = 1f;

    private void Start()
    {
        Destroy(gameObject, destroyDelay);
    }
}
