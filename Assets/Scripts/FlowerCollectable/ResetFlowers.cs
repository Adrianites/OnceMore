using UnityEngine;

public class ResetFlowers : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && GameManager.Instance.isNightTime)
        {
            Debug.Log("Resetting");
            GameManager.Instance.ResetGame();
        }
    }
}
