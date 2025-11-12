using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Linq;
using System.Collections.Generic;

public class ControllerMotionTracker : MonoBehaviour
{
    public enum SourceMode { ManualTransform, XRI_BaseController, XRI_Interactor, AutoByHandedness }
    public enum Handedness { Left, Right }
    public enum SourceSet { Single, LeftRight, LeftRightHead }

    [Header("Resolution Scope")]
    [Tooltip("Single: 기존과 동일(한 손만). LeftRight/LeftRightHead: 양손(+머리) 동시 추적")]
    public SourceSet sourceSet = SourceSet.Single;

    [Header("Resolution Targets (Common)")]
    public XROrigin xrOrigin;

    [Header("Single Mode (하위호환)")]
    public Transform controllerOverride;      // ManualTransform
    public XRBaseController xriController;   // XRI_BaseController
    public MonoBehaviour xriInteractor;      // XRI_Interactor (XRRayInteractor 등)
    public SourceMode sourceMode = SourceMode.AutoByHandedness;
    public Handedness handedness = Handedness.Right;

    [Header("Multi Mode Manual Overrides (Optional)")]
    public Transform leftOverride;
    public Transform rightOverride;
    public Transform headOverride;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // ===== 결과 (Single) 하위호환 =====
    private Transform _target;
    public Transform CurrentTarget => _target;
    public event System.Action<Transform> OnTargetResolved;

    // ===== 결과 (Multi) =====
    private Transform _left, _right, _head;
    public Transform CurrentLeft  => _left;
    public Transform CurrentRight => _right;
    public Transform CurrentHead  => _head;
    public event System.Action<Transform, Transform, Transform> OnTargetsResolved;

    void OnEnable() => ResolveTargets(force:true);
    void Update()
    {
        // null 되면 다시 시도
        if (NeedReResolve()) ResolveTargets(force:false);
    }

    public void ResolveNow() => ResolveTargets(force:true);

    private bool NeedReResolve()
    {
        if (sourceSet == SourceSet.Single)
            return _target == null;
        else
            return (_left == null) || (_right == null) || (sourceSet == SourceSet.LeftRightHead && _head == null);
    }

    private void ResolveTargets(bool force)
    {
        if (sourceSet == SourceSet.Single)
            ResolveSingle(force);
        else
            ResolveMulti(force);
    }

    // ================== Single (기존 로직 유지) ==================
    private void ResolveSingle(bool force)
    {
        Transform resolved = null;

        switch (sourceMode)
        {
            case SourceMode.ManualTransform:
                resolved = controllerOverride;
                break;
            case SourceMode.XRI_BaseController:
                resolved = xriController ? xriController.transform : null;
                break;
            case SourceMode.XRI_Interactor:
                resolved = xriInteractor ? xriInteractor.transform : null;
                break;
            case SourceMode.AutoByHandedness:
                resolved = ResolveAutoByHandedness(handedness);
                break;
        }

        if (resolved != _target || force)
        {
            _target = resolved;
            if (showDebugLogs)
                Debug.Log($"[ControllerMotionTracker] Single Target: {(_target ? _target.name : "null")} ({sourceMode}, {handedness})");
            OnTargetResolved?.Invoke(_target);
        }
    }

    // ================== Multi (Left/Right/(Head)) ==================
    private void ResolveMulti(bool force)
    {
        // 1) 수동 Override 우선
        Transform l = leftOverride;
        Transform r = rightOverride;
        Transform h = headOverride;

        // 2) 자동 탐색 보완
        if (l == null) l = ResolveAutoByHandedness(Handedness.Left);
        if (r == null) r = ResolveAutoByHandedness(Handedness.Right);
        if (sourceSet == SourceSet.LeftRightHead && h == null) h = ResolveHead();

        bool changed =
            (l != _left) || (r != _right) || (sourceSet == SourceSet.LeftRightHead && h != _head) || force;

        if (changed)
        {
            _left  = l;
            _right = r;
            if (sourceSet == SourceSet.LeftRightHead) _head = h;

            if (showDebugLogs)
            {
                Debug.Log($"[ControllerMotionTracker] Multi Targets => " +
                          $"L:{(_left ? _left.name : "null")}  " +
                          $"R:{(_right ? _right.name : "null")}  " +
                          (sourceSet == SourceSet.LeftRightHead ? $"H:{(_head ? _head.name : "null")}" : ""));
            }

            OnTargetsResolved?.Invoke(_left, _right, _head);

            // 하위호환: Single 사용자가 CurrentTarget를 계속 쓴다면, handedness 기준으로 노출
            _target = (handedness == Handedness.Left) ? _left : _right;
            OnTargetResolved?.Invoke(_target);
        }
    }

    // ================== Auto Helpers ==================
    private Transform ResolveAutoByHandedness(Handedness hand)
    {
        if (xrOrigin)
        {
            var t = FindControllerUnder(xrOrigin.transform, hand);
            if (t) return t;
        }
        return FindControllerUnder(null, hand);
    }

    private Transform ResolveHead()
    {
        // 1) XROrigin의 Camera 우선
        if (xrOrigin && xrOrigin.Camera != null)
            return xrOrigin.Camera.transform;

        // 2) 루트가 있으면 그 아래, 없으면 전체 씬에서 탐색
        Transform root = xrOrigin ? xrOrigin.transform : null;

        // a) 이름 패턴
        string[] headTags = new[] { "Head", "Camera", "Main Camera", "XR Origin Camera", "HMD", "CenterEye" };

        var all = root
            ? root.GetComponentsInChildren<Transform>(true)
            : FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var picked = PickByNameContains(all, headTags);
        if (picked) return picked;

        // b) 메인카메라
        if (Camera.main) return Camera.main.transform;

        return null;
    }

    private Transform FindControllerUnder(Transform root, Handedness hand)
    {
        string[] handTags = hand == Handedness.Left
            ? new[] { "Left", "left", "L_", "_L", "LeftHand", "LHand" }
            : new[] { "Right", "right", "R_", "_R", "RightHand", "RHand" };

        // 1) XRBaseController 우선
        var controllers = root
            ? root.GetComponentsInChildren<XRBaseController>(true)
            : FindObjectsByType<XRBaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var picked = PickByNameContains(controllers.Select(c => c.transform), handTags);
        if (picked) return picked;

        // 2) XRBaseInteractor 등 인터랙터
        var interactorComponents = root
            ? root.GetComponentsInChildren<MonoBehaviour>(true)
            : FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var interactorTransforms = interactorComponents
            .Where(m => m is XRBaseInteractor || m.GetType().Name.Contains("Interactor"))
            .Select(m => m.transform);

        picked = PickByNameContains(interactorTransforms, handTags);
        if (picked) return picked;

        // 3) 일반 네이밍 후보
        string[] commonNames = hand == Handedness.Left
            ? new[] { "LeftHand Controller", "Left Controller", "LeftHand", "ControllerLeft", "Left XR Controller" }
            : new[] { "RightHand Controller", "Right Controller", "RightHand", "ControllerRight", "Right XR Controller" };

        var allTransforms = root
            ? root.GetComponentsInChildren<Transform>(true)
            : FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        return PickByNameContains(allTransforms, commonNames);
    }

    private Transform PickByNameContains(IEnumerable<Transform> candidates, string[] keys)
    {
        Transform best = null; int bestScore = -1;
        foreach (var t in candidates)
        {
            if (!t) continue;
            string n = t.name; int score = 0;

            // 키워드 매칭 가점
            foreach (var k in keys)
            {
                if (!string.IsNullOrEmpty(k) && n.Contains(k))
                    score += 2;
            }
            // "Controller" 라벨 가점
            if (n.Contains("Controller")) score += 1;

            if (score > bestScore) { bestScore = score; best = t; }
        }
        return bestScore > 0 ? best : null;
    }
}
