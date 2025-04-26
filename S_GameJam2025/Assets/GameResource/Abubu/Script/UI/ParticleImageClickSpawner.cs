using UnityEngine;
using UnityEngine.EventSystems;
using AssetKits.ParticleImage;
using uPools;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using PIPlayMode = AssetKits.ParticleImage.Enumerations.PlayMode;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[AddComponentMenu("UI/Particle Image/Click Spawner (uPools + Odin)")]
public sealed class ParticleImageClickSpawner : MonoBehaviour, IPointerDownHandler
{
    [Title("Particle Prefab")]
    [Required, InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
    [SerializeField] private ParticleImage particlePrefab;

    [FoldoutGroup("Pooling"), MinValue(0)]
    public int prewarmCount = 10;

    [FoldoutGroup("Pooling")]
    public bool asyncPrewarm = true;

    private Canvas _canvas;
    private Camera _uiCamera;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (!_canvas)
        {
            enabled = false;
            return;
        }
        _uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;

        if (particlePrefab && prewarmCount > 0)
        {
            if (asyncPrewarm) _ = PrewarmAsync();
            else SharedGameObjectPool.Prewarm(particlePrefab.gameObject, prewarmCount);
        }
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            TrySpawn(Pointer.current.position.ReadValue());
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
                if (touch.press.wasPressedThisFrame)
                    TrySpawn(touch.position.ReadValue());
        }
    }
#else
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySpawn(Input.mousePosition);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TrySpawn(Input.GetTouch(0).position);
    }
#endif

    public void OnPointerDown(PointerEventData eventData)
    {
        TrySpawn(eventData.position);
    }

    async UniTaskVoid PrewarmAsync()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            SharedGameObjectPool.Prewarm(particlePrefab.gameObject, 1);
            await UniTask.Yield(PlayerLoopTiming.Initialization);
        }
    }

    void TrySpawn(Vector2 screenPos)
    {
        if (!particlePrefab) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPos, _uiCamera, out Vector2 localPos)) return;
        Spawn(localPos);
    }

    void Spawn(Vector2 localPosition)
    {
        var go = SharedGameObjectPool.Rent(particlePrefab.gameObject);
        go.transform.SetParent(_canvas.transform, false);

        var instance = go.GetComponent<ParticleImage>();
        var rt = (RectTransform)instance.transform;
        rt.anchoredPosition = localPosition;
        rt.localScale = Vector3.one;

        if (!instance.isPlaying && instance.PlayMode == PIPlayMode.None) instance.Play();

        instance.onLastParticleFinished.RemoveAllListeners();
        instance.onLastParticleFinished.AddListener(() =>
        {
            instance.Stop(true);
            SharedGameObjectPool.Return(go);
        });
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (prewarmCount < 0) prewarmCount = 0;
    }
#endif
}
