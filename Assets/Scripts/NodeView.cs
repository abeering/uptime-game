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
    [SerializeField] private Color blackoutColor = new Color(0.45f, 0.1f, 0.1f);
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

    public void ApplyInfection(InfectionType type)
    {
        if (type == InfectionType.None)
            return;

        // prevent duplicate types (Phase 6A rule)
        for (int i = 0; i < activeInfections.Count; i++)
        {
            if (activeInfections[i].Type == type)
                return;
        }

        var instance = InfectionFactory.Create(type);
        if (instance == null)
            return;

        instance.Initialize(this);
        activeInfections.Add(instance);

        instance.OnApplied();
        RefreshVisuals();
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
        InfectionType infectionType = InfectionType.None;

        // prioritize blackout if present
        for (int i = 0; i < activeInfections.Count; i++)
        {
            if (activeInfections[i].Type == InfectionType.Blackout)
            {
                infectionType = InfectionType.Blackout;
                break;
            }
        }

        // fallback to first infection if any
        if (infectionType == InfectionType.None && activeInfections.Count > 0)
        {
            infectionType = activeInfections[0].Type;
        }

        if (visualRenderer != null)
        {
            switch (infectionType)
            {
                case InfectionType.Blackout:
                    visualRenderer.color = blackoutColor;
                    break;

                default:
                    visualRenderer.color = normalColor;
                    break;
            }
        }

        if (labelText != null)
        {
            switch (infectionType)
            {
                case InfectionType.Blackout:
                    labelText.color = infectedLabelColor;
                    break;

                default:
                    labelText.color = normalLabelColor;
                    break;
            }
        }
    }
}