using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ConsoleGlitchOverlay : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material runtimeMaterial;

    [Header("Base Settings")]
    [Range(0f, 1f)] public float opacity = 0.16f;
    [Range(0f, 1f)] public float intensity = 0.28f;

    [Header("Reactive Spike")]
    [Range(0f, 1f)] public float spikeIntensity = 0.55f;
    [SerializeField] private float spikeDecaySpeed = 2.5f;

    [Header("Optional Pulse")]
    [SerializeField] private bool pulse = false;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmplitude = 0.05f;

    private RawImage rawImage;
    private Material instantiatedMaterial;
    private float currentSpike = 0f;

    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();

        if (runtimeMaterial != null)
        {
            instantiatedMaterial = new Material(runtimeMaterial);
            rawImage.material = instantiatedMaterial;
        }
        else if (rawImage.material != null)
        {
            instantiatedMaterial = new Material(rawImage.material);
            rawImage.material = instantiatedMaterial;
        }
        else
        {
            Debug.LogWarning($"{nameof(ConsoleGlitchOverlay)} on {name} has no material assigned.");
        }

        rawImage.raycastTarget = false;
    }

    private void Update()
    {
        if (instantiatedMaterial == null)
            return;

        currentSpike = Mathf.MoveTowards(currentSpike, 0f, spikeDecaySpeed * Time.deltaTime);

        float pulsedOpacity = opacity;
        if (pulse)
        {
            pulsedOpacity += Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        }

        float finalIntensity = Mathf.Clamp01(intensity + currentSpike);
        float finalOpacity = Mathf.Clamp01(pulsedOpacity + currentSpike * 0.08f);

        instantiatedMaterial.SetFloat(OpacityId, finalOpacity);
        instantiatedMaterial.SetFloat(IntensityId, finalIntensity);
    }

    public void TriggerGlitch()
    {
        TriggerGlitch(spikeIntensity);
    }

    public void TriggerGlitch(float amount)
    {
        currentSpike = Mathf.Max(currentSpike, Mathf.Clamp01(amount));
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }

    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
    }

    private void OnDestroy()
    {
        if (instantiatedMaterial != null)
        {
            Destroy(instantiatedMaterial);
        }
    }
}