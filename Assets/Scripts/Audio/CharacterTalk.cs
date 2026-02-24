using UnityEngine;
using Yarn.Unity;
using System.Collections;
using System.Collections.Generic;

public class CharacterTalk : MonoBehaviour
{
    private static readonly Dictionary<string, CharacterTalk> registry = new Dictionary<string, CharacterTalk>();

    [Header("Identifier")]
    [Tooltip("Unique ID used in Yarn commands, e.g. <<playSFX Plant>>")]
    [SerializeField] private string characterId;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip soundEffect;

    [Header("Pitch Variation")]
    [Tooltip("Enable random pitch variation to reduce repetitiveness")]
    [SerializeField] private bool randomizePitch = false;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(characterId))
        {
            registry[characterId] = this;
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(characterId) && registry.TryGetValue(characterId, out var registered) && registered == this)
        {
            registry.Remove(characterId);
        }
    }

    private static CharacterTalk Find(string id)
    {
        if (registry.TryGetValue(id, out var instance))
        {
            return instance;
        }
        Debug.LogError($"CharacterTalk: No character registered with ID \"{id}\"");
        return null;
    }

    private void ApplyPitch()
    {
        audioSource.pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;
    }

    [YarnCommand("playSFX")]
    public static void PlaySFX(string id)
    {
        Find(id)?.PlaySFXInternal();
    }

    [YarnCommand("stopSFX")]
    public static void StopSFX(string id)
    {
        Find(id)?.StopSFXInternal();
    }

    [YarnCommand("playSFXAndWait")]
    public static Coroutine PlaySFXAndWait(string id)
    {
        var instance = Find(id);
        if (instance == null) return null;
        return instance.StartCoroutine(instance.PlayAndWait());
    }

    private void PlaySFXInternal()
    {
        ApplyPitch();
        audioSource.PlayOneShot(soundEffect);
    }

    private void StopSFXInternal()
    {
        audioSource.Stop();
    }

    private IEnumerator PlayAndWait()
    {
        ApplyPitch();
        audioSource.PlayOneShot(soundEffect);
        yield return new WaitForSeconds(soundEffect.length);
    }
}
