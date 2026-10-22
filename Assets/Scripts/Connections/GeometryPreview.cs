using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Uptime.Connections
{
    // Scene fixture. Draws disposable copies of the production path graphic;
    // it does not create connections or touch the simulation host.
    [ExecuteAlways]
    public sealed class GeometryPreview : MonoBehaviour
    {
        [SerializeField, Tooltip("Presentation preview only; the authored simulation remains unchanged.")]
        private bool previewEnabled;
        [SerializeField, Range(1, 16)] private int connectionCount = 1;
        [SerializeField] private PhysicalLinkView physicalLink;
        [SerializeField] private LogicalPathPresenter presenter;
        [SerializeField] private IdentityPresenter identityPresenter;

        private readonly List<PathGraphic> graphics = new List<PathGraphic>();
        private bool warned;
        private bool presentedIdentity;
#if UNITY_EDITOR
        private double nextEditorRefresh;
#endif

#if UNITY_EDITOR
        private void OnEnable() => EditorApplication.update += RequestPreviewUpdate;

        private void RequestPreviewUpdate()
        {
            if (Application.isPlaying || !previewEnabled ||
                EditorApplication.timeSinceStartup < nextEditorRefresh)
                return;
            nextEditorRefresh = EditorApplication.timeSinceStartup + 0.08;
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif

        private void Update()
        {
            if (!previewEnabled || physicalLink == null || presenter == null ||
                presenter.NetworkArea == null || presenter.PathLayer == null || presenter.PathGraphicPrefab == null)
            {
                if (Application.isPlaying && presenter != null)
                    presenter.SetPreviewMode(false);
                Clear();
                ClearIdentities();
                return;
            }
            if (Application.isPlaying)
                presenter.SetPreviewMode(true);

            int count = Mathf.Clamp(connectionCount, 1, 16);
            if (graphics.Count != count || (graphics.Count > 0 &&
                graphics[0].transform.parent != presenter.PathLayer))
            {
                Clear();
                for (int i = 0; i < count; i++)
                {
                    PathGraphic graphic = Instantiate(
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
            float budget = physicalLink.BundleWidth(area);
            IdentityPacking.Layout layout = identityPresenter != null ?
                identityPresenter.ResolveLayout(physicalLink, count, presenter.MaxLineWidth,
                    presenter.MinLineWidth, presenter.MinLineGap) : null;
            float gap = layout != null ? layout.LineGap : presenter.MinLineGap;
            float width = layout != null ? layout.LineWidth :
                EdgeGeometry.LineWidth(budget, count, gap, presenter.MaxLineWidth);
            bool belowMinimum = width < presenter.MinLineWidth;
            if (belowMinimum && !warned)
                Debug.LogWarning($"Geometry preview {physicalLink.LinkId}: {count} lines need " +
                    $"{width:0.##} px each, below the {presenter.MinLineWidth:0.##} px minimum.", this);
            warned = belowMinimum;

            var identities = new List<IdentityPresenter.LiveLine>();
            var ids = new List<string>();
            for (int i = 0; i < count; i++)
                ids.Add("c" + (i + 1));
            ids.Sort(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                float offset = EdgeGeometry.Offset(i, count, width, gap);
                Vector2 start = EdgeGeometry.CardUnderlap(physicalLink.NodeA,
                    area, direction, offset, width);
                Vector2 end = EdgeGeometry.CardUnderlap(physicalLink.NodeB,
                    area, -direction, -offset, width);
                graphics[i].SetPath(new[] { start }, new[] { end }, new[] { width }, 1f);
                identities.Add(new IdentityPresenter.LiveLine(ids[i],
                    start, end, start, end, graphics[i].color, 1f, width));
            }
            if (identityPresenter != null)
            {
                identityPresenter.BeginFrame();
                identityPresenter.PresentEdge(physicalLink, identities);
                identityPresenter.EndFrame();
                if (!Application.isPlaying)
                    identityPresenter.Tick();
                presentedIdentity = true;
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= RequestPreviewUpdate;
#endif
            if (Application.isPlaying && presenter != null)
                presenter.SetPreviewMode(false);
            Clear();
            ClearIdentities();
        }

        private void ClearIdentities()
        {
            if (!presentedIdentity || identityPresenter == null)
                return;
            identityPresenter.BeginFrame();
            identityPresenter.EndFrame();
            presentedIdentity = false;
        }

        private void Clear()
        {
            foreach (PathGraphic graphic in graphics)
            {
                if (graphic == null)
                    continue;
                graphic.gameObject.SetActive(false);
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
