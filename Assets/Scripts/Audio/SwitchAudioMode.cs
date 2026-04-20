using UnityEngine;
using System.Collections;

public class SwitchAudioMode : MonoBehaviour
{
    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private float lerpDuration = 1.0f;
    public void SwitchAudioTo3DMode()
    {
        foreach (var audioSource in audioSources)
        {
            StartCoroutine(LerpAudioSpatialBlend(audioSource, 0.0f, 1.0f));
        }
    }

    public void SwitchAudioTo2DMode()
    {
        foreach (var audioSource in audioSources)
        {
            StartCoroutine(LerpAudioSpatialBlend(audioSource, 1.0f, 0.0f));
        }
    }

    private IEnumerator LerpAudioSpatialBlend(AudioSource audioSource, float currentValue, float targetValue)
    {
        float elapsedTime = 0f;
        while (elapsedTime < lerpDuration)
        {
            audioSource.spatialBlend = Mathf.Lerp(currentValue, targetValue, elapsedTime / lerpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.spatialBlend = targetValue;
    }
}
