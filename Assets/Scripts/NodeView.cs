using UnityEngine;

public class NodeView : MonoBehaviour
{
    public string nodeId;

    [Header("Tags")]
    public bool isEntryPoint;
    public bool isExternal;
    public bool isCritical;
 
    void Start()
    {
        FindObjectOfType<NetworkRuntime>().RegisterNode(this);
    }

}