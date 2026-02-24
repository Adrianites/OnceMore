using UnityEngine;
using UnityEngine.Rendering;

public class LightingManager : MonoBehaviour
{
    private enum LightingState { Day, TransitionToNight, Night, TransitionToDay }

    [SerializeField] private Light directionalLight;
    [SerializeField] private LightingPreset preset;
    [SerializeField, Range(0f, 24f)] private float timeOfDay;

    [Header("Night Transition")]
    [SerializeField] private float nightTimeTarget = 21f;
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private Material starrySkybox;
    [SerializeField] private Light[] nightLights;
    [SerializeField] private float nightLightIntensity = 1f;

    [Header("Day Transition")]
    [SerializeField] private float dayTransitionDuration = 3f;

    [Header("Player Hand Light")]
    [SerializeField] private GameObject handLightPrefab;
    [SerializeField] private Transform lightInHandTransform;
    [SerializeField] private float handLightRaiseHeight = 0.3f;

    [Header("Ambience Audio")]
    [SerializeField] private AudioSource dayAmbienceSource;
    [SerializeField] private AudioSource nightAmbienceSource;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.5f;

    [Header("Candle Flicker")]
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.15f;

    private const float SkyboxFadeStart = 0.35f;
    private const float SkyboxSwapPoint = 0.50f;
    private const float SkyboxFadeEnd   = 0.65f;
    private const float InvFadeOutRange = 1f / (SkyboxSwapPoint - SkyboxFadeStart);
    private const float InvFadeInRange  = 1f / (SkyboxFadeEnd - SkyboxSwapPoint);

    public bool IsNight => state == LightingState.Night;
    public bool IsTransitioning => state == LightingState.TransitionToNight || state == LightingState.TransitionToDay;

    private LightingState state = LightingState.Day;
    private float transitionTimer;
    private bool skyboxSwapped;

    private Material originalSkyboxInstance;
    private Material starrySkyboxInstance;

    private float startTimeOfDay;
    private float targetTimeOfDay;
    private Color startAmbient;
    private Color targetAmbient;
    private Color startFog;
    private Color targetFog;
    private Color startDirectionalColor;
    private Color targetDirectionalColor;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float currentDuration;

    private float originalExposure;
    private float starryExposure;

    private Material fadeOutSkybox;
    private Material fadeInSkybox;
    private float fadeOutExposure;
    private float fadeInExposure;

    private float flickerBaseline;

    private GameObject handLightInstance;
    private Light handLight;
    private Vector3 handLightStartLocalPos;
    private Vector3 handLightTargetLocalPos;

    private struct RenderSnapshot
    {
        public Material skybox;
        public AmbientMode ambientMode;
        public Color ambientLight;
        public Color fogColor;
        public float timeOfDay;
        public Color directionalColor;
        public Quaternion directionalRotation;
    }
    private RenderSnapshot savedDay;
    private bool hasSavedDaySettings;

    private static readonly int ExposureID = Shader.PropertyToID("_Exposure");
    private static readonly int TintID = Shader.PropertyToID("_Tint");

    private void Start()
    {
        flickerBaseline = 1f - flickerAmount;
        SetNightLightsIntensity(0f);
        InitAmbience();
    }

    private void Update()
    {
        if (preset == null)
            return;

        if (!Application.isPlaying)
        {
            UpdateLighting(timeOfDay / 24f);
            return;
        }

        switch (state)
        {
            case LightingState.TransitionToNight:
            case LightingState.TransitionToDay:
                UpdateTransition();
                break;
            case LightingState.Night:
                FlickerNightLights();
                break;
        }
    }

    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / currentDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t);

        timeOfDay = Mathf.Lerp(startTimeOfDay, targetTimeOfDay, eased);
        RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, eased);
        RenderSettings.fogColor = Color.Lerp(startFog, targetFog, eased);

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(startDirectionalColor, targetDirectionalColor, eased);
            directionalLight.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
        }

        UpdateSkyboxCrossFade(t);
        CrossfadeAmbience(eased);

        bool toNight = state == LightingState.TransitionToNight;
        float lightT = toNight ? t : 1f - t;
        FadeNightLights(lightT);

        if (lightInHandTransform != null && handLightInstance != null)
        {
            Vector3 from = toNight ? handLightStartLocalPos : handLightTargetLocalPos;
            Vector3 to   = toNight ? handLightTargetLocalPos : handLightStartLocalPos;
            lightInHandTransform.localPosition = Vector3.Lerp(from, to, eased);
        }

        if (t >= 1f)
            FinaliseTransition();
    }

    private void UpdateSkyboxCrossFade(float t)
    {
        if (fadeOutSkybox == null)
            return;

        if (!skyboxSwapped)
        {
            if (t >= SkyboxFadeStart && t < SkyboxSwapPoint)
            {
                float fade = Mathf.Clamp01((t - SkyboxFadeStart) * InvFadeOutRange);
                SetExposure(fadeOutSkybox, fadeOutExposure * (1f - fade));
            }
            else if (t >= SkyboxSwapPoint)
            {
                if (fadeInSkybox == null && state == LightingState.TransitionToDay && savedDay.skybox != null)
                {
                    originalSkyboxInstance = new Material(savedDay.skybox);
                    fadeInSkybox = originalSkyboxInstance;
                }

                if (fadeInSkybox != null)
                {
                    SetExposure(fadeInSkybox, 0f);
                    RenderSettings.skybox = fadeInSkybox;
                    skyboxSwapped = true;
                    DynamicGI.UpdateEnvironment();
                }
            }
        }
        else if (fadeInSkybox != null && t < SkyboxFadeEnd)
        {
            float fade = Mathf.Clamp01((t - SkyboxSwapPoint) * InvFadeInRange);
            SetExposure(fadeInSkybox, fadeInExposure * fade);
        }
    }

    private void FinaliseTransition()
    {
        if (state == LightingState.TransitionToNight)
        {
            state = LightingState.Night;

            if (starrySkyboxInstance != null)
            {
                SetExposure(starrySkyboxInstance, starryExposure);
                RenderSettings.skybox = starrySkyboxInstance;
                DynamicGI.UpdateEnvironment();
            }

            FadeNightLights(1f);
            DestroyMaterial(ref originalSkyboxInstance);
        }
        else
        {
            state = LightingState.Day;
            RestoreDaySnapshot();

            DestroyMaterial(ref originalSkyboxInstance);
            DestroyMaterial(ref starrySkyboxInstance);

            if (handLightInstance != null)
            {
                if (lightInHandTransform != null)
                    lightInHandTransform.localPosition = handLightStartLocalPos;

                Destroy(handLightInstance);
                handLightInstance = null;
                handLight = null;
            }

            SetNightLightsIntensity(0f);
            hasSavedDaySettings = false;
        }

        fadeOutSkybox = null;
        fadeInSkybox = null;
    }

    public void TriggerNightTransition()
    {
        if (state == LightingState.Night || state == LightingState.TransitionToNight)
            return;

        if (state == LightingState.TransitionToDay)
            CleanupTransitionMaterials();

        if (!hasSavedDaySettings)
        {
            SaveDaySnapshot();
            hasSavedDaySettings = true;
        }

        float targetPercent = nightTimeTarget / 24f;
        targetAmbient = preset.ambientColour.Evaluate(targetPercent);
        targetFog = preset.fogColour.Evaluate(targetPercent);
        targetDirectionalColor = preset.directionalColour.Evaluate(targetPercent);
        targetRotation = Quaternion.Euler((targetPercent * 360f) - 90f, 170f, 0f);
        targetTimeOfDay = nightTimeTarget;

        CaptureCurrentAsStart();
        currentDuration = transitionDuration;

        RenderSettings.ambientMode = AmbientMode.Flat;

        originalSkyboxInstance = RenderSettings.skybox != null ? new Material(RenderSettings.skybox) : null;
        starrySkyboxInstance = starrySkybox != null ? new Material(starrySkybox) : null;

        if (starrySkyboxInstance != null && starrySkyboxInstance.HasProperty(TintID))
            starrySkyboxInstance.SetColor(TintID, Color.white);

        if (originalSkyboxInstance != null)
            RenderSettings.skybox = originalSkyboxInstance;

        originalExposure = GetExposure(originalSkyboxInstance);
        starryExposure = GetExposure(starrySkybox);

        fadeOutSkybox = originalSkyboxInstance;
        fadeInSkybox = starrySkyboxInstance;
        fadeOutExposure = originalExposure;
        fadeInExposure = starryExposure;

        skyboxSwapped = false;
        transitionTimer = 0f;

        SpawnHandLight();

        state = LightingState.TransitionToNight;
    }

    public void TriggerDayTransition()
    {
        if (state == LightingState.Day || state == LightingState.TransitionToDay)
            return;

        if (state == LightingState.TransitionToNight)
            CleanupTransitionMaterials();

        CaptureCurrentAsStart();
        targetTimeOfDay = savedDay.timeOfDay;
        targetAmbient = savedDay.ambientLight;
        targetFog = savedDay.fogColor;
        targetDirectionalColor = savedDay.directionalColor;
        targetRotation = savedDay.directionalRotation;
        currentDuration = dayTransitionDuration;

        RenderSettings.ambientMode = savedDay.ambientMode;

        if (starrySkyboxInstance == null && RenderSettings.skybox != null)
            starrySkyboxInstance = new Material(RenderSettings.skybox);

        starryExposure = GetExposure(starrySkyboxInstance);
        originalExposure = GetExposure(savedDay.skybox);

        fadeOutSkybox = starrySkyboxInstance;
        fadeInSkybox = null;
        fadeOutExposure = starryExposure;
        fadeInExposure = originalExposure;

        skyboxSwapped = false;
        transitionTimer = 0f;

        state = LightingState.TransitionToDay;
    }

    private void CaptureCurrentAsStart()
    {
        startTimeOfDay = timeOfDay;
        startAmbient = RenderSettings.ambientLight;
        startFog = RenderSettings.fogColor;

        if (directionalLight != null)
        {
            startDirectionalColor = directionalLight.color;
            startRotation = directionalLight.transform.localRotation;
        }
        else
        {
            startDirectionalColor = Color.white;
            startRotation = Quaternion.identity;
        }
    }

    private void CleanupTransitionMaterials()
    {
        DestroyMaterial(ref originalSkyboxInstance);
        DestroyMaterial(ref starrySkyboxInstance);
        fadeOutSkybox = null;
        fadeInSkybox = null;
    }

    private void SaveDaySnapshot()
    {
        savedDay.skybox = RenderSettings.skybox;
        savedDay.ambientMode = RenderSettings.ambientMode;
        savedDay.ambientLight = RenderSettings.ambientLight;
        savedDay.fogColor = RenderSettings.fogColor;
        savedDay.timeOfDay = timeOfDay;
        savedDay.directionalColor = directionalLight != null ? directionalLight.color : Color.white;
        savedDay.directionalRotation = directionalLight != null ? directionalLight.transform.localRotation : Quaternion.identity;
    }

    private void RestoreDaySnapshot()
    {
        RenderSettings.skybox = savedDay.skybox;
        RenderSettings.ambientMode = savedDay.ambientMode;
        RenderSettings.ambientLight = savedDay.ambientLight;
        RenderSettings.fogColor = savedDay.fogColor;

        if (directionalLight != null)
        {
            directionalLight.color = savedDay.directionalColor;
            directionalLight.transform.localRotation = savedDay.directionalRotation;
        }

        timeOfDay = savedDay.timeOfDay;
        DynamicGI.UpdateEnvironment();
    }

    private void UpdateLighting(float timePercent)
    {
        RenderSettings.ambientLight = preset.ambientColour.Evaluate(timePercent);
        RenderSettings.fogColor = preset.fogColour.Evaluate(timePercent);

        if (directionalLight != null)
        {
            directionalLight.color = preset.directionalColour.Evaluate(timePercent);
            directionalLight.transform.localRotation = Quaternion.Euler((timePercent * 360f) - 90f, 170f, 0f);
        }
    }

    private void SpawnHandLight()
    {
        if (handLightPrefab == null || lightInHandTransform == null || handLightInstance != null)
            return;

        handLightInstance = Instantiate(handLightPrefab, lightInHandTransform);
        handLightInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        handLight = handLightInstance.GetComponentInChildren<Light>();
        if (handLight != null)
            handLight.intensity = 0f;

        handLightStartLocalPos = lightInHandTransform.localPosition;
        handLightTargetLocalPos = handLightStartLocalPos + Vector3.up * handLightRaiseHeight;
    }

    private void SetNightLightsIntensity(float intensity)
    {
        if (nightLights == null) return;
        for (int i = 0; i < nightLights.Length; i++)
        {
            if (nightLights[i] != null)
                nightLights[i].intensity = intensity;
        }
    }

    private void FadeNightLights(float t)
    {
        if (nightLights == null) return;
        float intensity = nightLightIntensity * t;
        for (int i = 0; i < nightLights.Length; i++)
        {
            if (nightLights[i] != null)
                nightLights[i].intensity = intensity;
        }

        if (handLight != null)
            handLight.intensity = intensity;
    }

    private void FlickerNightLights()
    {
        if (nightLights == null) return;
        float time = Time.time * flickerSpeed;

        for (int i = 0; i < nightLights.Length; i++)
        {
            if (nightLights[i] == null) continue;
            float noise = Mathf.PerlinNoise(time + i * 137.5f, i * 27.3f);
            nightLights[i].intensity = nightLightIntensity * (flickerBaseline + noise * flickerAmount);
        }

        if (handLight != null)
        {
            float noise = Mathf.PerlinNoise(time + 999.7f, 53.1f);
            handLight.intensity = nightLightIntensity * (flickerBaseline + noise * flickerAmount);
        }
    }

    private float GetExposure(Material mat)
    {
        if (mat != null && mat.HasProperty(ExposureID))
            return mat.GetFloat(ExposureID);
        return 1f;
    }

    private void SetExposure(Material mat, float value)
    {
        if (mat != null && mat.HasProperty(ExposureID))
            mat.SetFloat(ExposureID, value);
    }

    private void DestroyMaterial(ref Material mat)
    {
        if (mat != null)
        {
            Destroy(mat);
            mat = null;
        }
    }

    private void InitAmbience()
    {
        if (dayAmbienceSource != null)
        {
            dayAmbienceSource.volume = ambienceVolume;
            dayAmbienceSource.loop = true;
            if (!dayAmbienceSource.isPlaying)
                dayAmbienceSource.Play();
        }

        if (nightAmbienceSource != null)
        {
            nightAmbienceSource.volume = 0f;
            nightAmbienceSource.loop = true;
            if (!nightAmbienceSource.isPlaying)
                nightAmbienceSource.Play();
        }
    }

    private void CrossfadeAmbience(float t)
    {
        bool toNight = state == LightingState.TransitionToNight;
        float nightT = toNight ? t : 1f - t;

        if (dayAmbienceSource != null)
            dayAmbienceSource.volume = ambienceVolume * (1f - nightT);

        if (nightAmbienceSource != null)
            nightAmbienceSource.volume = ambienceVolume * nightT;
    }

    private void OnValidate()
    {
        if (directionalLight == null)
        {
            if (RenderSettings.sun != null)
            {
                directionalLight = RenderSettings.sun;
            }
            else
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }
        }

        SetNightLightsIntensity(0f);
        flickerBaseline = 1f - flickerAmount;
    }

    private void OnDestroy()
    {
        DestroyMaterial(ref originalSkyboxInstance);
        if (starrySkyboxInstance != null && RenderSettings.skybox != starrySkyboxInstance)
            DestroyMaterial(ref starrySkyboxInstance);
    }
}
