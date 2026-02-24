using UnityEngine;

public class PickupFlower : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.CollectFlower();
            Destroy(gameObject);
        }
    }
}
