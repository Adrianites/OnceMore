using UnityEngine;
using UnityEngine.Rendering;

public class LightingManager : MonoBehaviour
{
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

    [Header("Candle Flicker")]
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.15f;

    private bool isTransitioning;
    private bool isNight;
    private float transitionTimer;
    private float startTimeOfDay;
    private bool skyboxSwapped;

    private Material originalSkyboxInstance;
    private Material starrySkyboxInstance;

    private Color startAmbient;
    private Color startDirectionalColor;
    private Color startFog;
    private Quaternion startRotation;
    private float originalExposure;
    private float starryExposure;
    private AmbientMode originalAmbientMode;
    private float[] nightLightTargetIntensities;

    private Color cachedNightAmbient;
    private Color cachedNightFog;
    private Color cachedNightDirColor;
    private Quaternion cachedNightRotation;

    private GameObject handLightInstance;
    private Light handLight;
    private Vector3 handLightStartLocalPos;
    private Vector3 handLightTargetLocalPos;

    private bool isDayTransitioning;
    private float dayTransitionTimer;
    private float dayStartTimeOfDay;
    private bool daySkyboxSwapped;

    private Color dayStartAmbient;
    private Color dayStartFog;
    private Color dayStartDirectionalColor;
    private Quaternion dayStartRotation;

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
        if (nightLights != null)
        {
            foreach (Light l in nightLights)
            {
                if (l != null)
                    l.intensity = 0f;
            }
        }
    }

    private void Update()
    {
        if (preset == null)
            return;

        if (Application.isPlaying)
        {
            if (isTransitioning)
            {
                transitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(transitionTimer / transitionDuration);

                timeOfDay = Mathf.Lerp(startTimeOfDay, nightTimeTarget, t);

                RenderSettings.ambientLight = Color.Lerp(startAmbient, cachedNightAmbient, t);
                RenderSettings.fogColor = Color.Lerp(startFog, cachedNightFog, t);

                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(startDirectionalColor, cachedNightDirColor, t);
                    directionalLight.transform.localRotation = Quaternion.Slerp(startRotation, cachedNightRotation, t);
                }

                if (starrySkyboxInstance != null && originalSkyboxInstance != null)
                {
                    const float fadeStart = 0.35f;
                    const float swapPoint = 0.50f;
                    const float fadeEnd   = 0.65f;

                    if (!skyboxSwapped)
                    {
                        if (t >= fadeStart && t < swapPoint)
                        {
                            float fade = Mathf.Clamp01((t - fadeStart) / (swapPoint - fadeStart));
                            SetExposure(originalSkyboxInstance, Mathf.Lerp(originalExposure, 0f, fade));
                        }
                        else if (t >= swapPoint)
                        {
                            SetExposure(starrySkyboxInstance, 0f);
                            RenderSettings.skybox = starrySkyboxInstance;
                            skyboxSwapped = true;
                            DynamicGI.UpdateEnvironment();
                        }
                    }
                    else if (t < fadeEnd)
                    {
                        float fade = Mathf.Clamp01((t - swapPoint) / (fadeEnd - swapPoint));
                        SetExposure(starrySkyboxInstance, Mathf.Lerp(0f, starryExposure, fade));
                    }
                }

                FadeNightLights(t);

                if (lightInHandTransform != null && handLightInstance != null)
                    lightInHandTransform.localPosition = Vector3.Lerp(handLightStartLocalPos, handLightTargetLocalPos, Mathf.SmoothStep(0f, 1f, t));

                if (t >= 1f)
                {
                    isTransitioning = false;
                    isNight = true;

                    if (starrySkyboxInstance != null)
                    {
                        SetExposure(starrySkyboxInstance, starryExposure);
                        RenderSettings.skybox = starrySkyboxInstance;
                        DynamicGI.UpdateEnvironment();
                    }

                    FadeNightLights(1f);

                    if (originalSkyboxInstance != null)
                    {
                        Destroy(originalSkyboxInstance);
                        originalSkyboxInstance = null;
                    }
                }
            }
            else if (isDayTransitioning)
            {
                dayTransitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(dayTransitionTimer / dayTransitionDuration);

                timeOfDay = Mathf.Lerp(dayStartTimeOfDay, savedDay.timeOfDay, t);

                RenderSettings.ambientLight = Color.Lerp(dayStartAmbient, savedDay.ambientLight, t);
                RenderSettings.fogColor = Color.Lerp(dayStartFog, savedDay.fogColor, t);

                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(dayStartDirectionalColor, savedDay.directionalColor, t);
                    directionalLight.transform.localRotation = Quaternion.Slerp(dayStartRotation, savedDay.directionalRotation, t);
                }

                if (starrySkyboxInstance != null && savedDay.skybox != null)
                {
                    const float fadeStart = 0.35f;
                    const float swapPoint = 0.50f;
                    const float fadeEnd   = 0.65f;

                    if (!daySkyboxSwapped)
                    {
                        if (t >= fadeStart && t < swapPoint)
                        {
                            float fade = Mathf.Clamp01((t - fadeStart) / (swapPoint - fadeStart));
                            SetExposure(starrySkyboxInstance, Mathf.Lerp(starryExposure, 0f, fade));
                        }
                        else if (t >= swapPoint)
                        {
                            originalSkyboxInstance = new Material(savedDay.skybox);
                            SetExposure(originalSkyboxInstance, 0f);
                            RenderSettings.skybox = originalSkyboxInstance;
                            daySkyboxSwapped = true;
                            DynamicGI.UpdateEnvironment();
                        }
                    }
                    else if (t < fadeEnd)
                    {
                        float fade = Mathf.Clamp01((t - swapPoint) / (fadeEnd - swapPoint));
                        SetExposure(originalSkyboxInstance, Mathf.Lerp(0f, originalExposure, fade));
                    }
                }

                FadeNightLightsReverse(t);

                if (lightInHandTransform != null && handLightInstance != null)
                    lightInHandTransform.localPosition = Vector3.Lerp(handLightTargetLocalPos, handLightStartLocalPos, Mathf.SmoothStep(0f, 1f, t));

                if (t >= 1f)
                {
                    isDayTransitioning = false;
                    isNight = false;

                    RestoreDaySnapshot();

                    if (originalSkyboxInstance != null)
                    {
                        Destroy(originalSkyboxInstance);
                        originalSkyboxInstance = null;
                    }
                    if (starrySkyboxInstance != null)
                    {
                        Destroy(starrySkyboxInstance);
                        starrySkyboxInstance = null;
                    }

                    if (handLightInstance != null)
                    {
                        if (lightInHandTransform != null)
                            lightInHandTransform.localPosition = handLightStartLocalPos;

                        Destroy(handLightInstance);
                        handLightInstance = null;
                        handLight = null;
                    }

                    if (nightLights != null)
                    {
                        foreach (Light l in nightLights)
                        {
                            if (l != null) l.intensity = 0f;
                        }
                    }

                    hasSavedDaySettings = false;
                }
            }
            else if (isNight)
            {
                FlickerNightLights();
            }
        }
        else
        {
            UpdateLighting(timeOfDay / 24f);
        }
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
            directionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0));
        }
    }

    public void TriggerNightTransition()
    {
        if (isNight || isTransitioning || isDayTransitioning)
            return;

        if (!hasSavedDaySettings)
        {
            SaveDaySnapshot();
            hasSavedDaySettings = true;
        }

        startTimeOfDay = timeOfDay;
        transitionTimer = 0f;
        isTransitioning = true;

        startAmbient = RenderSettings.ambientLight;
        startFog = RenderSettings.fogColor;
        startDirectionalColor = directionalLight != null ? directionalLight.color : Color.white;
        startRotation = directionalLight != null ? directionalLight.transform.localRotation : Quaternion.identity;

        originalAmbientMode = RenderSettings.ambientMode;
        RenderSettings.ambientMode = AmbientMode.Flat;

        float targetPercent = nightTimeTarget / 24f;
        cachedNightAmbient = preset.ambientColour.Evaluate(targetPercent);
        cachedNightFog = preset.fogColour.Evaluate(targetPercent);
        cachedNightDirColor = preset.directionalColour.Evaluate(targetPercent);
        cachedNightRotation = Quaternion.Euler(new Vector3((targetPercent * 360f) - 90f, 170f, 0));

        originalSkyboxInstance = RenderSettings.skybox != null
            ? new Material(RenderSettings.skybox)
            : null;
        starrySkyboxInstance = starrySkybox != null
            ? new Material(starrySkybox)
            : null;

        if (starrySkyboxInstance != null && starrySkyboxInstance.HasProperty(TintID))
            starrySkyboxInstance.SetColor(TintID, Color.white);

        if (originalSkyboxInstance != null)
            RenderSettings.skybox = originalSkyboxInstance;

        skyboxSwapped = false;
        originalExposure = GetExposure(originalSkyboxInstance);
        starryExposure = GetExposure(starrySkybox);

        if (handLightPrefab != null && lightInHandTransform != null && handLightInstance == null)
        {
            handLightInstance = Instantiate(handLightPrefab, lightInHandTransform);
            handLightInstance.transform.localPosition = Vector3.zero;
            handLightInstance.transform.localRotation = Quaternion.identity;

            handLight = handLightInstance.GetComponentInChildren<Light>();
            if (handLight != null)
                handLight.intensity = 0f;

            handLightStartLocalPos = lightInHandTransform.localPosition;
            handLightTargetLocalPos = handLightStartLocalPos + Vector3.up * handLightRaiseHeight;
        }

        if (nightLights != null)
        {
            nightLightTargetIntensities = new float[nightLights.Length];
            for (int i = 0; i < nightLights.Length; i++)
            {
                if (nightLights[i] != null)
                {
                    nightLightTargetIntensities[i] = nightLightIntensity;
                    nightLights[i].intensity = 0f;
                }
            }
        }
    }

    public void TriggerDayTransition()
    {
        if (!isNight || isTransitioning || isDayTransitioning)
            return;

        dayStartTimeOfDay = timeOfDay;
        dayTransitionTimer = 0f;
        isDayTransitioning = true;
        daySkyboxSwapped = false;

        dayStartAmbient = RenderSettings.ambientLight;
        dayStartFog = RenderSettings.fogColor;
        dayStartDirectionalColor = directionalLight != null ? directionalLight.color : Color.white;
        dayStartRotation = directionalLight != null ? directionalLight.transform.localRotation : Quaternion.identity;

        RenderSettings.ambientMode = savedDay.ambientMode;

        if (starrySkyboxInstance == null && RenderSettings.skybox != null)
            starrySkyboxInstance = new Material(RenderSettings.skybox);

        starryExposure = GetExposure(starrySkyboxInstance);
        originalExposure = GetExposure(savedDay.skybox);
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

    private void FadeNightLights(float t)
    {
        if (nightLights == null || nightLightTargetIntensities == null) return;
        for (int i = 0; i < nightLights.Length; i++)
        {
            if (nightLights[i] != null)
                nightLights[i].intensity = Mathf.Lerp(0f, nightLightTargetIntensities[i], t);
        }

        if (handLight != null)
            handLight.intensity = Mathf.Lerp(0f, nightLightIntensity, t);
    }

    private void FadeNightLightsReverse(float t)
    {
        if (nightLights == null || nightLightTargetIntensities == null) return;
        for (int i = 0; i < nightLights.Length; i++)
        {
            if (nightLights[i] != null)
                nightLights[i].intensity = Mathf.Lerp(nightLightTargetIntensities[i], 0f, t);
        }

        if (handLight != null)
            handLight.intensity = Mathf.Lerp(nightLightIntensity, 0f, t);
    }

    private void FlickerNightLights()
    {
        if (nightLights == null) return;
        for (int i = 0; i < nightLights.Length; i++)
        {
            if (nightLights[i] == null) continue;
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + i * 137.5f, i * 27.3f);
            float flicker = 1f - flickerAmount + noise * flickerAmount;
            nightLights[i].intensity = nightLightIntensity * flicker;
        }

        if (handLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + 999.7f, 53.1f);
            float flicker = 1f - flickerAmount + noise * flickerAmount;
            handLight.intensity = nightLightIntensity * flicker;
        }
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

        if (nightLights != null)
        {
            foreach (Light l in nightLights)
            {
                if (l != null)
                    l.intensity = 0f;
            }
        }
    }

    private void OnDestroy()
    {
        if (originalSkyboxInstance != null)
            Destroy(originalSkyboxInstance);
        if (starrySkyboxInstance != null && RenderSettings.skybox != starrySkyboxInstance)
            Destroy(starrySkyboxInstance);
    }
}
