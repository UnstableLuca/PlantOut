using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController_CP : MonoBehaviour
{
    [Header("Movimiento (WASD)")]
    [SerializeField] private float velocidad = 10f;
    [SerializeField, Tooltip("Speed multiplier while holding Shift.")]
    private float multiplicadorRapido = 2.5f;
    [SerializeField, Tooltip("Move faster the higher the camera is, so it feels the same at any zoom.")]
    private bool escalarConAltura = true;

    [Header("Giro (Q / E)")]
    [SerializeField, Tooltip("Degrees per second.")] private float velocidadGiro = 90f;
    [SerializeField, Tooltip("Height of the ground the camera orbits around.")]
    private float alturaSuelo = 0f;

    [Header("Zoom (rueda)")]
    [SerializeField] private float pasoZoom = 2f;
    [SerializeField] private float suavizadoZoom = 10f;
    [SerializeField] private float alturaMin = 2f;
    [SerializeField] private float alturaMax = 30f;

    private float alturaInicial;
    private float zoomPendiente;

    private void Start() => alturaInicial = Mathf.Max(transform.position.y - alturaSuelo, 0.01f);

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        var teclado = Keyboard.current;
        var raton = Mouse.current;

        if (teclado != null)
        {
            Mover(teclado, dt);
            Girar(teclado, dt);
        }
        if (raton != null) LeerZoom(raton);
        AplicarZoom(dt);
    }

    private void Mover(Keyboard t, float dt)
    {
        Vector2 input = Vector2.zero;
        if (t.wKey.isPressed) input.y += 1f;
        if (t.sKey.isPressed) input.y -= 1f;
        if (t.dKey.isPressed) input.x += 1f;
        if (t.aKey.isPressed) input.x -= 1f;
        if (input == Vector2.zero) return;

        // The camera's forward and right flattened onto the ground, so W doesn't dive into the floor.
        Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (adelante.sqrMagnitude < 0.001f) adelante = Vector3.ProjectOnPlane(transform.up, Vector3.up);
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);

        float v = velocidad * (t.leftShiftKey.isPressed ? multiplicadorRapido : 1f);
        if (escalarConAltura)
            v *= Mathf.Max(0.2f, (transform.position.y - alturaSuelo) / alturaInicial);

        transform.position += (adelante * input.y + derecha * input.x).normalized * v * dt;
    }

    private void Girar(Keyboard t, float dt)
    {
        float giro = 0f;
        if (t.qKey.isPressed) giro -= 1f;
        if (t.eKey.isPressed) giro += 1f;
        if (giro == 0f) return;

        transform.RotateAround(PuntoPivote(), Vector3.up, giro * velocidadGiro * dt);
    }

    // The point on the ground the camera is looking at, so turning orbits around it.
    private Vector3 PuntoPivote()
    {
        var suelo = new Plane(Vector3.up, new Vector3(0f, alturaSuelo, 0f));
        var rayo = new Ray(transform.position, transform.forward);
        return suelo.Raycast(rayo, out float d) ? rayo.GetPoint(d) : transform.position;
    }

    private void LeerZoom(Mouse m)
    {
        // Uses only the scroll direction, so it behaves the same whatever size the wheel reports.
        float scroll = m.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f) zoomPendiente += Mathf.Sign(scroll) * pasoZoom;
    }

    private void AplicarZoom(float dt)
    {
        if (Mathf.Abs(zoomPendiente) < 0.001f) { zoomPendiente = 0f; return; }

        float paso = zoomPendiente * (1f - Mathf.Exp(-suavizadoZoom * dt));
        zoomPendiente -= paso;

        Vector3 destino = transform.position + transform.forward * paso;
        float altura = destino.y - alturaSuelo;

        if (altura < alturaMin || altura > alturaMax)
        {
            // Stop exactly at the limit instead of overshooting it.
            float yObjetivo = alturaSuelo + Mathf.Clamp(altura, alturaMin, alturaMax);
            if (Mathf.Abs(transform.forward.y) > 0.001f)
                destino = transform.position + transform.forward * ((yObjetivo - transform.position.y) / transform.forward.y);
            zoomPendiente = 0f;
        }

        transform.position = destino;
    }
}

