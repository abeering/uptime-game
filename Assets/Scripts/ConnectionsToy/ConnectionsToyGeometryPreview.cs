using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // Edit-mode scene fixture. Draws disposable copies of the production path graphic;
    // it does not create connections or touch the simulation host.
    [ExecuteAlways]
    public sealed class ConnectionsToyGeometryPreview : MonoBehaviour
    {
        [SerializeField, Tooltip("Edit mode only. Clear this to return to the authored scene.")]
        private bool previewEnabled;
        [SerializeField, Range(1, 16)] private int connectionCount = 1;
        [SerializeField] private ConnectionsToyPhysicalLinkView physicalLink;
        [SerializeField] private ConnectionsToyLogicalPathPresenter presenter;

        private readonly List<ConnectionsToyPathGraphic> graphics = new List<ConnectionsToyPathGraphic>();
        private bool warned;

        private void Update()
        {
            if (Application.isPlaying || !previewEnabled || physicalLink == null || presenter == null ||
                presenter.NetworkArea == null || presenter.PathLayer == null || presenter.PathGraphicPrefab == null)
            {
                Clear();
                return;
            }

            int count = Mathf.Clamp(connectionCount, 1, 16);
            if (graphics.Count != count || (graphics.Count > 0 &&
                graphics[0].transform.parent != presenter.PathLayer))
            {
                Clear();
                for (int i = 0; i < count; i++)
                {
                    ConnectionsToyPathGraphic graphic = Instantiate(
                        presenter.PathGraphicPrefab, presenter.PathLayer, false);
                    graphic.name = "Geometry preview " + (i + 1);
                    graphic.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                    foreach (TMP_Text tag in graphic.GetComponentsInChildren<TMP_Text>(true))
                        tag.transform.parent.gameObject.SetActive(false);
                    graphic.raycastTarget = false;
                    graphics.Add(graphic);
                }
            }

            RectTransform area = presenter.NetworkArea;
            Vector2 a = area.InverseTransformPoint(physicalLink.NodeA.transform.position);
            Vector2 b = area.InverseTransformPoint(physicalLink.NodeB.transform.position);
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float budget = physicalLink.BundleWidth(area);
            float gap = presenter.MinLineGap;
            float width = ConnectionsToyEdgeGeometry.LineWidth(budget, count, gap, presenter.MaxLineWidth);
            bool belowMinimum = ConnectionsToyEdgeGeometry.BelowMinimum(
                budget, count, gap, presenter.MaxLineWidth, presenter.MinLineWidth);
            if (belowMinimum && !warned)
                Debug.LogWarning($"Geometry preview {physicalLink.LinkId}: {count} lines need " +
                    $"{width:0.##} px each, below the {presenter.MinLineWidth:0.##} px minimum.", this);
            warned = belowMinimum;

            for (int i = 0; i < count; i++)
            {
                float offset = ConnectionsToyEdgeGeometry.Offset(i, count, width, gap);
                Vector2 start = ConnectionsToyEdgeGeometry.CardEdge(physicalLink.NodeA, area, direction) + normal * offset;
                Vector2 end = ConnectionsToyEdgeGeometry.CardEdge(physicalLink.NodeB, area, -direction) + normal * offset;
                graphics[i].SetPath(new[] { start }, new[] { end }, new[] { width }, 1f);
            }
        }

        private void OnDisable() => Clear();

        private void Clear()
        {
            foreach (ConnectionsToyPathGraphic graphic in graphics)
            {
                if (graphic == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(graphic.gameObject);
                else
                    DestroyImmediate(graphic.gameObject);
            }
            graphics.Clear();
            warned = false;
        }
    }
}
