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

    private GameObject handLightInstance;
    private Light handLight;
    private Vector3 handLightStartLocalPos;
    private Vector3 handLightTargetLocalPos;

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

                float targetPercent = nightTimeTarget / 24f;
                Color targetAmbient = preset.ambientColour.Evaluate(targetPercent);
                Color targetFog = preset.fogColour.Evaluate(targetPercent);
                Color targetDirColor = preset.directionalColour.Evaluate(targetPercent);
                Quaternion targetRotation = Quaternion.Euler(new Vector3((targetPercent * 360f) - 90f, 170f, 0));

                RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, t);
                RenderSettings.fogColor = Color.Lerp(startFog, targetFog, t);

                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(startDirectionalColor, targetDirColor, t);
                    directionalLight.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                }

                if (starrySkyboxInstance != null && originalSkyboxInstance != null)
                {
                    const float fadeStart = 0.35f;
                    const float swapPoint = 0.50f;
                    const float fadeEnd   = 0.65f;

                    if (!skyboxSwapped)
                    {
                        if (t < fadeStart)
                        {
                            SetExposure(originalSkyboxInstance, originalExposure);
                        }
                        else if (t < swapPoint)
                        {
                            float fade = Mathf.Clamp01((t - fadeStart) / (swapPoint - fadeStart));
                            SetExposure(originalSkyboxInstance, Mathf.Lerp(originalExposure, 0f, fade));
                        }
                        else
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
                    else
                    {
                        SetExposure(starrySkyboxInstance, starryExposure);
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
            else if (isNight)
            {
                UpdateLighting(nightTimeTarget / 24f);
                FlickerNightLights();
            }
        }
        else
        {
            OnValidate();
            UpdateLighting(timeOfDay / 24f);
        }
    }

    public void TriggerNightTransition()
    {
        if (isNight || isTransitioning)
            return;

        startTimeOfDay = timeOfDay;
        transitionTimer = 0f;
        isTransitioning = true;

        startAmbient = RenderSettings.ambientLight;
        startFog = RenderSettings.fogColor;
        startDirectionalColor = directionalLight != null ? directionalLight.color : Color.white;
        startRotation = directionalLight != null ? directionalLight.transform.localRotation : Quaternion.identity;

        originalAmbientMode = RenderSettings.ambientMode;
        RenderSettings.ambientMode = AmbientMode.Flat;

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
