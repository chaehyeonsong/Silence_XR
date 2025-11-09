using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Linq;

public class ControllerMotionTracker : MonoBehaviour
{
    public enum SourceMode { ManualTransform, XRI_BaseController, XRI_Interactor, AutoByHandedness }
    public enum Handedness { Left, Right }

    [Header("Resolution Targets")]
    public XROrigin xrOrigin;
    public Transform controllerOverride;      // ManualTransform
    public XRBaseController xriController;   // XRI_BaseController
    public MonoBehaviour xriInteractor;      // XRI_Interactor (XRRayInteractor 등)
    public SourceMode sourceMode = SourceMode.AutoByHandedness;
    public Handedness handedness = Handedness.Right;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // 결과
    private Transform _target;
    public Transform CurrentTarget => _target;

    public event System.Action<Transform> OnTargetResolved;

    void OnEnable() => ResolveTarget(true);
    void Update() { if (_target == null) ResolveTarget(false); }

    public void ResolveNow() => ResolveTarget(true);

    private void ResolveTarget(bool force)
    {
        Transform resolved = null;
        switch (sourceMode)
        {
            case SourceMode.ManualTransform:   resolved = controllerOverride; break;
            case SourceMode.XRI_BaseController:resolved = xriController ? xriController.transform : null; break;
            case SourceMode.XRI_Interactor:    resolved = xriInteractor ? xriInteractor.transform : null; break;
            case SourceMode.AutoByHandedness:  resolved = ResolveAutoByHandedness(handedness); break;
        }

        if (resolved != _target || force)
        {
            _target = resolved;
            if (showDebugLogs)
                Debug.Log($"[ControllerMotionTracker] Target: {(_target ? _target.name : "null")} ({sourceMode}, {handedness})");
            OnTargetResolved?.Invoke(_target);
        }
    }

    private Transform ResolveAutoByHandedness(Handedness hand)
    {
        if (xrOrigin)
        {
            var t = FindControllerUnder(xrOrigin.transform, hand);
            if (t) return t;
        }
        return FindControllerUnder(null, hand);
    }

    private Transform FindControllerUnder(Transform root, Handedness hand)
    {
        string[] handTags = hand == Handedness.Left ? new[] { "Left", "left", "L_" }
                                                    : new[] { "Right", "right", "R_" };

        var controllers = root
            ? root.GetComponentsInChildren<XRBaseController>(true)
            : FindObjectsByType<XRBaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var picked = PickByNameContains(controllers.Select(c => c.transform), handTags);
        if (picked) return picked;

        var interactorComponents = root
            ? root.GetComponentsInChildren<MonoBehaviour>(true)
            : FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var interactorTransforms = interactorComponents
            .Where(m => m is XRBaseInteractor || m.GetType().Name.Contains("Interactor"))
            .Select(m => m.transform);
        picked = PickByNameContains(interactorTransforms, handTags);
        if (picked) return picked;

        string[] commonNames = hand == Handedness.Left
            ? new[] { "LeftHand Controller", "Left Controller", "LeftHand", "ControllerLeft" }
            : new[] { "RightHand Controller", "Right Controller", "RightHand", "ControllerRight" };

        var allTransforms = root
            ? root.GetComponentsInChildren<Transform>(true)
            : FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        return PickByNameContains(allTransforms, commonNames);
    }

    private Transform PickByNameContains(System.Collections.Generic.IEnumerable<Transform> candidates, string[] keys)
    {
        Transform best = null; int bestScore = -1;
        foreach (var t in candidates)
        {
            if (!t) continue;
            string n = t.name; int score = 0;
            foreach (var k in keys) if (n.Contains(k)) score += 2;
            if (n.Contains("Controller")) score += 1;
            if (score > bestScore) { bestScore = score; best = t; }
        }
        return bestScore > 0 ? best : null;
    }
}
