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
    private NodeInfectionInstance activeInfection;
    public bool IsInfected => activeInfection != null;

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
        if (activeInfection != null)
            return;

        var instance = InfectionFactory.Create(type);
        if (instance == null)
            return;

        instance.Initialize(this);
        activeInfection = instance;

        activeInfection.OnApplied();
        RefreshVisuals();
    }

    public void ClearInfection()
    {
        if (activeInfection == null)
            return;

        activeInfection.OnRemoved();
        activeInfection = null;

        RefreshVisuals();
    }

    public bool BlocksTraffic()
    {
        return activeInfection != null && activeInfection.BlocksTraffic();
    }

    private void RefreshVisuals()
    {
        var infectionType = activeInfection?.Type ?? InfectionType.None;

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