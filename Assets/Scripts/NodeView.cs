using TMPro;
using UnityEngine;

public class NodeView : MonoBehaviour
{
    public string nodeId;

    [Header("Tags")]
    public bool isEntryPoint;
    public bool isExternal;
    public bool isCritical;

    [Header("References")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private TMP_Text labelText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color infectedLabelColor = new Color(1f, 0.8f, 0.8f);
    [SerializeField] private Color normalLabelColor = Color.black;

    [Header("Infection")]
    private readonly System.Collections.Generic.List<NodeInfectionInstance> activeInfections = new();
    public bool IsInfected => activeInfections.Count > 0;
    
    private void Awake()
    {
        if (visualRenderer == null)
        {
            Transform visual = transform.Find("Visual");
            if (visual != null)
                visualRenderer = visual.GetComponent<SpriteRenderer>();
        }

        if (labelText == null)
        {
            Transform label = transform.Find("Label");
            if (label != null)
                labelText = label.GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        RefreshVisuals();
    }

    public void Initialize(NetworkRuntime runtime)
    {
        runtime.RegisterNode(this);
    }

    public void Tick(InfectionContext context)
    {
        TickInfections(context);
    }

    public void TickInfections(InfectionContext context)
    {
        for (int i = 0; i < activeInfections.Count; i++)
        {
            activeInfections[i].OnTick(context);
        }
    }

    public bool CanAcceptInfection(InfectionPayload payload)
    {
        if (payload == null || payload.type == InfectionType.None)
            return false;

        for (int i = 0; i < activeInfections.Count; i++)
        {
            if (activeInfections[i].Type == payload.type)
                return false;
        }

        return true;
    }

    public bool ApplyInfection(InfectionPayload payload)
    {
        if (!CanAcceptInfection(payload))
            return false;

        var instance = InfectionFactory.Create(payload);
        if (instance == null)
            return false;

        Debug.Log($"[Node][{nodeId}] applying infection: {payload}");

        instance.Initialize(this, payload);
        activeInfections.Add(instance);

        instance.OnApplied();
        RefreshVisuals();
        return true;
    }

    public void ApplyInfection(InfectionType type)
    {
        if (type == InfectionType.None)
            return;

        // ApplyInfection(new InfectionPayload(type));
        ApplyInfection(InfectionFactory.CreateDefaultPayload(type));
    }

    public void ClearInfection()
    {
        if (activeInfections.Count == 0)
            return;

        for (int i = 0; i < activeInfections.Count; i++)
        {
            activeInfections[i].OnRemoved();
        }

        activeInfections.Clear();
        RefreshVisuals();
    }

    public bool BlocksTraffic()
    {
        for (int i = 0; i < activeInfections.Count; i++)
        {
            if (activeInfections[i].BlocksTraffic())
                return true;
        }

        return false;
    }

    private void RefreshVisuals()
    {
        Color displayColor = normalColor;
        int tintCount = 0;

        for (int i = 0; i < activeInfections.Count; i++)
        {
            Color? tint = activeInfections[i].GetNodeTintColor();
            if (!tint.HasValue)
                continue;

            tintCount++;
            float lerpAmount = 1f / tintCount;
            displayColor = Color.Lerp(displayColor, tint.Value, lerpAmount);
        }

        if (visualRenderer != null)
        {
            visualRenderer.color = displayColor;
        }

        if (labelText != null)
        {
            labelText.color = activeInfections.Count > 0
                ? infectedLabelColor
                : normalLabelColor;
        }
    }

}