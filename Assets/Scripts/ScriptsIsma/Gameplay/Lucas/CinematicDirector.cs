using UnityEngine;

public class CinematicDirector : MonoBehaviour
{
    public static CinematicDirector Instance { get; private set; }

    [Header("Escenario")]
    [Tooltip("Donde se coloca la planta que esta a la izquierda durante la cinematica")]
    [SerializeField] Transform leftStage;
    [Tooltip("Donde se coloca la planta que esta a la derecha")]
    [SerializeField] Transform rightStage;
    [Tooltip("Segundos que tardan en llegar y volver")]
    [SerializeField, Min(0.05f)] float moveTime = 0.4f;

    [Header("Camara")]
    [Tooltip("Si esta vacio camera.main")]
    [SerializeField] Camera cam;
    [Tooltip("Donde mira la camara, vacio = el punto entre los dos 'escenarios'")]
    [SerializeField] Transform cameraFocus;
    [Tooltip("Zoom: 0.6 = la camara ve el 60% mas cerca. 1 = sin zoom")]
    [SerializeField, Range(0.2f, 1f)] float zoomFactor = 0.65f;
    [SerializeField, Min(0.05f)] float cameraTime = 0.4f;

    [Header("Extras")]
    [Tooltip("Cosas para activar durante la cinematica")]
    [SerializeField] GameObject[] showDuring;
    [Tooltip("Cosas que se ocultan durante la cinematica")]
    [SerializeField] GameObject[] hideDuring;
    [Tooltip("BugSpawner para pausarlo durante")]
    [SerializeField] BugSpawner spawner;

    [Header("Tiempos")]
    [Tooltip("Segundos que se mantiene la escena tras terminar la ulti (impact frames?)")]
    [SerializeField, Min(0f)] float holdAfter = 0.6f;
    [Tooltip("Termina despues de N segundos por si acaso bugs")]
    [SerializeField, Min(1f)] float maxDuration = 10f;

    Fighter caster;
    bool active;
    bool ending;
    float endAt;
    float failsafeAt;
    bool spawnerWasRunning;

    Vector3 camHomePos;
    float camHomeSize;
    float camHomeFov;
    Vector3 camVelocity;
    float sizeVelocity;
    float fovVelocity;

    public bool IsPlaying => active;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            camHomePos = cam.transform.position;
            camHomeSize = cam.orthographicSize;
            camHomeFov = cam.fieldOfView;
        }

        SetExtras(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Begin(Fighter caster)
    {
        if (Instance != null) Instance.BeginInternal(caster);
    }

    void BeginInternal(Fighter c)
    {
        if (active || c == null) return;

        caster = c;
        active = true;
        ending = false;
        failsafeAt = Time.time + maxDuration;

        Fighter rival = c.Rival;
        bool casterIsLeft = rival == null  || c.transform.position.x <= rival.transform.position.x;

        MoveToStage(c, casterIsLeft ? leftStage : rightStage);
        MoveToStage(rival, casterIsLeft ? rightStage : leftStage);

        if(spawner != null)
        {
            spawnerWasRunning = spawner.Running;
            spawner.Running = false;
        }

        SetExtras(true);
    }

    void EndInternal(Fighter c)
    {
        if (!active || ending || c != caster) return;
        
        ending = true;
        endAt = Time.time + holdAfter;
    }

    void Finish()
    {
        active = false;
        ending = false;

        ReturnHome(caster);
        if (caster != null) ReturnHome(caster.Rival);
        caster = null;

        if (spawner != null) spawner.Running = spawnerWasRunning;
        SetExtras(false);
    }

    void MoveToStage(Fighter f, Transform stage)
    {
        if (f == null || stage == null) return;

        var height = f.GetComponent<PlantHeight>();
        if (height != null) height.GoToStage(stage.position, moveTime);
    }

    void ReturnHome(Fighter f)
    {
        if (f == null) return;

        var height = f.GetComponent<PlantHeight>();
        if (height != null) height.LeaveStage(moveTime);
    }

    void Update()
    {
        if (!active) return;

        if ((ending && Time.time >= endAt) || Time.time >= failsafeAt) Finish();
    }

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 targetPos = camHomePos;
        float targetZoom = 1f;

        if (active)
        {
            Vector3 focus = FocusPoint();
            targetPos = new Vector3(focus.x, focus.y, camHomePos.z);
            targetZoom = zoomFactor;
        }

        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, targetPos, ref camVelocity, cameraTime);

        if (cam.orthographic)
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, camHomeSize * targetZoom, ref sizeVelocity, cameraTime);
        else
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, camHomeFov * targetZoom, ref fovVelocity, cameraTime);
    }

    Vector3 FocusPoint()
    {
        if (cameraFocus != null) return cameraFocus.position;
        if (leftStage != null && rightStage != null) return (leftStage.position + rightStage.position) * 0.5f;
        return camHomePos;
    }

    void SetExtras(bool playing)
    {
        if (showDuring != null)
            foreach (var go in showDuring) if (go != null) go.SetActive(playing);
        if (hideDuring != null)
            foreach (var go in hideDuring) if (go != null) go.SetActive(!playing);
    }
}
