using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 强制 AR 平面无视 tracking state 始终可见 + 手动同步 Camera pose。
/// </summary>
public class ForcePlaneVisible : MonoBehaviour
{
    public ARPlaneManager planeManager;
    private int _planeCount = 0;
    private int _frameCounter = 0;
    private System.Type _inputSystemHeadType;
    private PropertyInfo _headPositionProp;
    private PropertyInfo _headRotationProp;
    private bool _inputSystemFound;

    void Awake()
    {
        // enable Legacy Input sensors (activeInputHandler=0)
        UnityEngine.Input.gyro.enabled = true;
        UnityEngine.Input.compass.enabled = true;

        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
        if (planeManager != null)
            planeManager.planesChanged += OnPlanesChanged;
        
        // 反射查找 Input System XRDevice.head
        _inputSystemHeadType = System.Type.GetType("UnityEngine.InputSystem.XR.XRDevice, Unity.InputSystem");
        if (_inputSystemHeadType != null)
        {
            var headProp = _inputSystemHeadType.GetProperty("head", BindingFlags.Static | BindingFlags.Public);
            if (headProp != null)
            {
                var headPoseInstance = headProp.GetValue(null);
                if (headPoseInstance != null)
                {
                    _headPositionProp = headPoseInstance.GetType().GetProperty("position");
                    _headRotationProp = headPoseInstance.GetType().GetProperty("rotation");
                    _inputSystemFound = _headPositionProp != null && _headRotationProp != null;
                }
            }
        }

        // 诊断：列所有 Input System 设备
        try
        {
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystemType != null)
            {
                var devicesProp = inputSystemType.GetProperty("devices", BindingFlags.Static | BindingFlags.Public);
                var devices = devicesProp?.GetValue(null) as System.Collections.IEnumerable;
                var sb = new System.Text.StringBuilder();
                int count = 0;
                if (devices != null)
                {
                    foreach (var d in devices)
                    {
                        sb.AppendLine($"  Device: {d.GetType().Name} - {d}");
                        count++;
                    }
                }
                Debug.Log($"[ForcePlaneVisible] InputSystem devices ({count}):\n{sb}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ForcePlaneVisible] InputSystem diag failed: {e.Message}");
        }

        Debug.Log($"[ForcePlaneVisible] Awake. InputSystem head pose found: {_inputSystemFound}");
    }

    void OnDestroy()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var p in args.added)
        {
            p.destroyOnRemoval = false;
            p.gameObject.SetActive(true);
            var vis = p.GetComponent<ARPlaneMeshVisualizer>();
            if (vis != null) vis.trackingStateVisibilityThreshold = TrackingState.None;
            var mr = p.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
            _planeCount++;

            // 详细诊断
            var mf = p.GetComponent<MeshFilter>();
            var mat = mr != null ? mr.sharedMaterial : null;
            Debug.Log($"[ForcePlaneVisible] ADDED #{_planeCount} id={p.trackableId} ts={p.trackingState} " +
                      $"goActive={p.gameObject.activeInHierarchy} mrEnabled={(mr != null && mr.enabled)} " +
                      $"mesh={(mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount + "verts" : "NULL")} " +
                      $"mat={(mat != null ? mat.name + "/" + mat.shader.name : "NULL")} " +
                      $"color={(mat != null ? mat.color.ToString() : "")}");
        }
        foreach (var p in args.updated)
        {
            p.destroyOnRemoval = false;
            p.gameObject.SetActive(true);
            var vis = p.GetComponent<ARPlaneMeshVisualizer>();
            if (vis != null) vis.trackingStateVisibilityThreshold = TrackingState.None;
            var mr = p.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
        }
        foreach (var p in args.removed)
        {
            Debug.Log($"[ForcePlaneVisible] REMOVED id={p.trackableId} (kept alive)");
        }
    }

    void Update()
    {
        // === 手动同步 Camera pose —— 多路 fallback ===
        var cam = Camera.main;
        if (cam != null)
        {
            bool poseApplied = false;

            // 方法 0: InputTracking.GetNodeStates() —— Unity 内置 Legacy XR Node API
            // SpatialTracking.TPD 也用这个。优先查询 CenterEye → Head → ColorCamera
            try
            {
                var nodeStates = new List<UnityEngine.XR.XRNodeState>();
                UnityEngine.XR.InputTracking.GetNodeStates(nodeStates);
                if (_frameCounter % 120 == 0)
                    UnityEngine.Debug.Log($"[POSE] InputTracking nodes: {nodeStates.Count}");

                // 优先 CenterEye (ARCore 用这个)，然后 Head，然后其他
                UnityEngine.XR.XRNodeState? bestNode = null;
                UnityEngine.XR.XRNode[] priority = {
                    UnityEngine.XR.XRNode.CenterEye,
                    UnityEngine.XR.XRNode.Head,
                    UnityEngine.XR.XRNode.LeftEye,
                    UnityEngine.XR.XRNode.RightEye,
                };

                for (int i = 0; i < priority.Length && !bestNode.HasValue; i++)
                {
                    foreach (var ns in nodeStates)
                    {
                        if (ns.nodeType == priority[i] && ns.tracked)
                        {
                            bestNode = ns;
                            break;
                        }
                    }
                }

                if (bestNode.HasValue)
                {
                    var ns = bestNode.Value;
                    bool gotPos = ns.TryGetPosition(out Vector3 pos);
                    bool gotRot = ns.TryGetRotation(out Quaternion rot);
                    if (_frameCounter % 120 == 0)
                        UnityEngine.Debug.Log($"[POSE] InputTracking node={ns.nodeType} tracked={ns.tracked} pos={gotPos} rot={gotRot}");
                    if (gotPos) cam.transform.localPosition = pos;
                    if (gotRot) cam.transform.localRotation = rot;
                    poseApplied = gotPos || gotRot;
                }
            }
            catch (System.Exception e)
            {
                if (_frameCounter % 120 == 0)
                    UnityEngine.Debug.LogWarning($"[POSE] InputTracking failed: {e.Message}");
            }

            // 方法 1: XRPM InputDevices（XR Plugin Management 注册的设备）
            if (!poseApplied)
            {
                try
                {
                    var devices = new List<UnityEngine.XR.InputDevice>();
                    UnityEngine.XR.InputDevices.GetDevices(devices);
                    if (_frameCounter % 120 == 0)
                        UnityEngine.Debug.Log($"[POSE] XRPM InputDevices: {devices.Count}");

                    foreach (var device in devices)
                    {
                        if (!device.characteristics.HasFlag(UnityEngine.XR.InputDeviceCharacteristics.TrackedDevice)) continue;

                        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out Vector3 pos) ||
                            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.colorCameraPosition, out pos))
                        {
                            cam.transform.localPosition = pos;
                            poseApplied = true;
                        }
                        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out Quaternion rot) ||
                            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.colorCameraRotation, out rot))
                        {
                            cam.transform.localRotation = rot;
                            poseApplied = true;
                        }
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    if (_frameCounter % 120 == 0)
                        UnityEngine.Debug.LogWarning($"[POSE] InputDevices failed: {e.Message}");
                }
            }

            // 方法 2: Input System XRDevice.head（反射）
            if (!poseApplied && _inputSystemFound)
            {
                try
                {
                    var headProp = _inputSystemHeadType.GetProperty("head", BindingFlags.Static | BindingFlags.Public);
                    var headPose = headProp?.GetValue(null);
                    if (headPose != null)
                    {
                        var pos = (Vector3)_headPositionProp.GetValue(headPose);
                        var rot = (Quaternion)_headRotationProp.GetValue(headPose);
                        if (pos.sqrMagnitude > 0.0001f || rot != Quaternion.identity)
                        {
                            cam.transform.localPosition = pos;
                            cam.transform.localRotation = rot;
                            poseApplied = true;
                        }
                    }
                }
                catch { }
            }
        }

        if (planeManager == null) return;
        foreach (var p in planeManager.trackables)
        {
            p.destroyOnRemoval = false;
            p.gameObject.SetActive(true);
            var vis = p.GetComponent<ARPlaneMeshVisualizer>();
            if (vis != null) vis.trackingStateVisibilityThreshold = TrackingState.None;
            var mr = p.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
        }

        // 每秒打一次详细诊断（世界坐标 + 相机）
        if (_frameCounter++ % 60 == 0 && planeManager.trackables.count > 0)
        {
            var camPos = cam != null ? cam.transform.position : Vector3.zero;
            Debug.Log($"[FORCE-DIAG] CAMERA pos={camPos:F2} rot={cam?.transform.rotation.eulerAngles:F2}");
            foreach (var p in planeManager.trackables)
            {
                var mr = p.GetComponent<MeshRenderer>();
                var mf = p.GetComponent<MeshFilter>();
                var dist = Vector3.Distance(camPos, p.transform.position);
                Debug.Log($"[FORCE-DIAG] PLANE id={p.trackableId} ts={p.trackingState} " +
                          $"pos={p.transform.position:F2} rotEuler={p.transform.rotation.eulerAngles:F1} " +
                          $"dist={dist:F2}m " +
                          $"active={p.gameObject.activeInHierarchy} " +
                          $"mr.on={(mr != null && mr.enabled)} " +
                          $"mesh={(mf?.sharedMesh != null ? mf.sharedMesh.vertexCount + "v" : "NULL")} " +
                          $"subsumedBy={(p.subsumedBy != null ? p.subsumedBy.trackableId.ToString() : "none")}");
            }
        }
    }
}
