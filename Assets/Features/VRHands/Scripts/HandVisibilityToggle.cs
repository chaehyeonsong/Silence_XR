using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandVisibilityToggle : MonoBehaviour
{
    [Header("Assign the interactor(s) you use")]
    [SerializeField] private XRDirectInteractor directInteractor; // Near grab
    [SerializeField] private XRRayInteractor rayInteractor;       // Far grab (optional)
    [SerializeField] private XRPokeInteractor pokeInteractor;     // 👈 추가: Poke용

    private SkinnedMeshRenderer handModel;
    private bool isGrabbed = false;
    private bool isPoking = false;                                // 👈 추가
    private IXRSelectInteractor currentInteractor = null;

    private void Awake()
    {
        handModel = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (handModel == null)
            Debug.LogError("[HandVisibilityToggle] SkinnedMeshRenderer not found under this GameObject.");
    }

    private void OnEnable()
    {
        if (directInteractor != null)
        {
            directInteractor.selectEntered.AddListener(OnGrab);
            directInteractor.selectExited.AddListener(OnLetGo);
        }
        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.AddListener(OnGrab);
            rayInteractor.selectExited.AddListener(OnLetGo);
        }
        if (pokeInteractor != null)
        {
            pokeInteractor.selectEntered.AddListener(OnPokeBegin);   // 👈 추가
            pokeInteractor.selectExited.AddListener(OnPokeEnd);      // 👈 추가
        }
    }

    private void OnDisable()
    {
        if (directInteractor != null)
        {
            directInteractor.selectEntered.RemoveListener(OnGrab);
            directInteractor.selectExited.RemoveListener(OnLetGo);
        }
        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.RemoveListener(OnGrab);
            rayInteractor.selectExited.RemoveListener(OnLetGo);
        }
        if (pokeInteractor != null)
        {
            pokeInteractor.selectEntered.RemoveListener(OnPokeBegin); // 👈 추가
            pokeInteractor.selectExited.RemoveListener(OnPokeEnd);    // 👈 추가
        }
    }

    private void Update()
    {
        if (handModel == null) return;

        // 1) 기존 로직: "잡힌 상태 && 그 인터랙터가 직접(interactor)일 때만 감추기"
        bool doingNearGrab =
            isGrabbed &&
            currentInteractor != null &&
            directInteractor != null &&
            ReferenceEquals(currentInteractor, directInteractor);

        // 2) pokeInteractor가 뭔가를 잡고 있는 중이면 손 감추기
        bool doingPoke = isPoking;

        bool shouldHide = doingNearGrab || doingPoke;

        if (handModel.enabled == shouldHide)
            handModel.enabled = !shouldHide;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        currentInteractor = args.interactorObject;
    }

    private void OnLetGo(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentInteractor = null;
    }

    private void OnPokeBegin(SelectEnterEventArgs args)
    {
        isPoking = true;
    }

    private void OnPokeEnd(SelectExitEventArgs args)
    {
        isPoking = false;
    }
}
