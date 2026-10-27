using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 点击检测到的水平平面，在命中位置放置 AR 标记。
/// 挂在 XR Origin (AR Rig) 物体上，需要同物体上有 ARRaycastManager 组件。
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class PlaceMarkerOnPlane : MonoBehaviour
{
    [Tooltip("标记 Prefab，一个简单的立方体/圆柱体即可")]
    public GameObject markerPrefab;

    [Tooltip("表单控制器引用，放置标记后通知它记录位置")]
    public IssueFormUI issueForm;

    private ARRaycastManager raycastManager;
    private static readonly List<ARRaycastHit> hits = new();

    /// <summary>当前放置的标记（只保留最后一个）</summary>
    private GameObject currentMarker;

    /// <summary>当前标记的世界坐标，供上报时使用</summary>
    public Vector3? CurrentMarkerPosition => currentMarker == null ? null : currentMarker.transform.position;

    /// <summary>当前标记的 GameObject，供外部（表单）修改样式</summary>
    public GameObject CurrentMarker => currentMarker;

    /// <summary>根据优先级设置标记的颜色和大小（供 IssueFormUI 调用）</summary>
    public void ApplyMarkerStyle(int priorityIndex)
    {
        if (currentMarker == null) return;
        // 0=low 绿, 1=medium 黄, 2=high 红
        Color[] colors = {
            new Color(0.15f, 0.85f, 0.35f, 1f), // low 亮绿
            new Color(0.95f, 0.75f, 0.10f, 1f), // medium 亮黄
            new Color(0.95f, 0.20f, 0.20f, 1f), // high 亮红
        };
        float[] sizes = { 0.12f, 0.16f, 0.22f }; // low 小, medium 中, high 大
        int idx = Mathf.Clamp(priorityIndex, 0, 2);

        // 改所有 MeshRenderer 的材质颜色
        var renderers = currentMarker.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                mat.color = colors[idx];
            }
        }
        // 改缩放：让 high 的标记明显更大
        var targetScale = sizes[idx];
        currentMarker.transform.localScale = Vector3.one * targetScale;
    }

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    private void Update()
    {
        // 表单打开时不响应平面点击，避免误放标记
        if (issueForm != null && issueForm.IsFormOpen) return;

        // 只响应"刚开始"的单点触摸（编辑器里支持鼠标点击方便调试）
        if (!TryGetTapPosition(out Vector2 tapPosition)) return;

        // 从触摸点向检测到的平面发射射线
        if (!raycastManager.Raycast(tapPosition, hits, TrackableType.PlaneWithinPolygon)) return;

        Pose hitPose = hits[0].pose;
        PlaceMarker(hitPose.position);
    }

    private void PlaceMarker(Vector3 position)
    {
        // 只保留最后一个标记，先销毁旧的
        if (currentMarker != null) Destroy(currentMarker);

        currentMarker = Instantiate(markerPrefab, position, Quaternion.identity);

        // 放好后先给个默认灰白色，大小中等
        currentMarker.transform.localScale = Vector3.one * 0.16f;
        var renderers = currentMarker.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                mat.color = new Color(0.65f, 0.65f, 0.65f, 1f); // 中性灰
            }
        }

        // 通知表单：已有新标记，可以填写问题信息了
        issueForm?.OnMarkerPlaced();
    }

    private static bool TryGetTapPosition(out Vector2 position)
    {
        // 真机触摸
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }
        // 编辑器鼠标调试
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
        position = default;
        return false;
    }
}
