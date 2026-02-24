using UnityEngine;

public class PickupFlower : MonoBehaviour
{
    public GameObject collectSoundEffect;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.CollectFlower();
            Instantiate(collectSoundEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
