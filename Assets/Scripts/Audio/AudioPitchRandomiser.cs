using UnityEngine;

public class AudioPitchRandomiser : MonoBehaviour
{
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
    }
}
