using UnityEngine;

public class ResetFlowers : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && GameManager.Instance.isNightTime)
        {
            GameManager.Instance.ResetGame();
            Debug.Log("Game reset!");
        }
    }
}
