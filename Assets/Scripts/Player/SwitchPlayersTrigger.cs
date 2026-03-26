using UnityEngine;

public class SwitchPlayersTrigger : MonoBehaviour
{
    public GameObject pressFCanvas;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.canSwitchToRoom = true;
            pressFCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.canSwitchToRoom = false;
            pressFCanvas.SetActive(false);
        }
    }
}
