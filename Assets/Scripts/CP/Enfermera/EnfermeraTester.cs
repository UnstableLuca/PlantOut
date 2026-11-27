using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class EnfermeraTester : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Pharmacy pharmacy;
    [SerializeField] private MalaHierbaEnfermera enfermera;
    [SerializeField, Tooltip("A Plant in the scene (or a prefab) that gets cloned as each patient.")]
    private Plant plantillaPaciente;
    [SerializeField] private bool colorAleatorio = true;
    [SerializeField, Tooltip("Saturation range (min, max).")] private Vector2 saturacion = new Vector2(0.6f, 1f);
    [SerializeField, Tooltip("Brightness range (min, max).")] private Vector2 brillo = new Vector2(0.7f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Cola")]
    [SerializeField, Tooltip("Where the queue starts. Empty = the template's position.")]
    private Transform puntoCola;
    [SerializeField] private Vector3 separacionCola = new Vector3(-1.2f, 0f, 0f);

    [Header("Pacientes")]
    [SerializeField] private Key teclaSpawn = Key.P;
    [SerializeField] private Vector2 saludInicial = new Vector2(10f, 60f);

    [Header("Enfermeras")]
    [SerializeField, Min(1), Tooltip("Total de enfermeras, contando la original.")]
    private int maxEnfermeras = 2;
    [SerializeField] private Key teclaEnfermera = Key.N;
    [SerializeField, Tooltip("Recepción de cada enfermera extra (por orden). Vacío = la misma que la original.")]
    private Transform[] puntosRecepcionExtra;
    private readonly List<MalaHierbaEnfermera> enfermeras = new List<MalaHierbaEnfermera>();

    [Header("Debug")]
    [SerializeField] private bool mostrarPanel = true;



    private readonly List<Plant> spawneados = new List<Plant>();
    private Vector3 origenCola;
    private int totalCamillas;
    private int contador;
    private void Awake()
    {
        if (pharmacy == null) pharmacy = FindAnyObjectByType<Pharmacy>();
        if (enfermera == null) enfermera = FindAnyObjectByType<MalaHierbaEnfermera>();

        enfermeras.Add(enfermera);
        origenCola = puntoCola != null ? puntoCola.position : plantillaPaciente.transform.position;

        if (plantillaPaciente.gameObject.scene.IsValid())
            plantillaPaciente.gameObject.SetActive(false);
    }

    private void Start() => totalCamillas = pharmacy.emptyBeds.Count;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[teclaSpawn].wasPressedThisFrame)
            SpawnPaciente();

        if (Keyboard.current != null && Keyboard.current[teclaEnfermera].wasPressedThisFrame)
            SpawnEnfermera();

        ColocarCola();
    }

    public void SpawnPaciente()
    {
        if (!Application.isPlaying) return;

        Plant p = Instantiate(plantillaPaciente);
        p.name = $"Paciente {++contador}";
        p.salud = UnityEngine.Random.Range(saludInicial.x, saludInicial.y);
        p.gameObject.SetActive(true);
        if (colorAleatorio) PintarAleatorio(p);

        spawneados.Add(p);
        pharmacy.Encolar(p);
        ColocarCola();
    }

    public void SpawnEnfermera()
    {
        if (!Application.isPlaying) return;

        if (enfermeras.Count >= maxEnfermeras)
        {
            Debug.Log($"Ya hay {enfermeras.Count}/{maxEnfermeras} enfermeras.", this);
            return;
        }

        int extra = enfermeras.Count - 1; // 0 = primera enfermera extra
        Transform punto = puntosRecepcionExtra != null && puntosRecepcionExtra.Length > 0
            ? puntosRecepcionExtra[extra % puntosRecepcionExtra.Length]
            : null;

        Vector3 pos = punto != null ? punto.position : enfermera.transform.position;
        MalaHierbaEnfermera nueva = Instantiate(enfermera, pos, enfermera.transform.rotation);
        nueva.name = $"Enfermera {enfermeras.Count + 1}";
        nueva.AsignarRecepcion(punto);

        enfermeras.Add(nueva);
    }
    private void PintarAleatorio(Plant p)
    {
        Color c = UnityEngine.Random.ColorHSV(0f, 1f, saturacion.x, saturacion.y, brillo.x, brillo.y);
        var bloque = new MaterialPropertyBlock();

        foreach (var r in p.GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(bloque);
            bloque.SetColor(BaseColorId, c);
            bloque.SetColor(ColorId, c);
            r.SetPropertyBlock(bloque);
        }
    }

    public void SpawnCinco()
    {
        for (int i = 0; i < 5; i++) SpawnPaciente();
    }

    // Keeps the waiting patients in a line, so the queue moves up when the nurse takes one.
    private void ColocarCola()
    {
        int i = 0;
        foreach (var p in pharmacy.clients)
            if (p != null) p.transform.position = origenCola + separacionCola * i++;
    }

    private void OnGUI()
    {
        if (!mostrarPanel || pharmacy == null) return;

        int atendidos = 0;
        foreach (var s in spawneados) if (s == null) atendidos++;

        // Área invisible y alta; la caja de dentro se ajusta sola al contenido.
        GUILayout.BeginArea(new Rect(10, 10, 320, Screen.height - 20));
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("MALA HIERBA ENFERMERA");

        foreach (var e in enfermeras)
        {
            if (e == null) continue;
            Plant actual = e.PacienteActual;
            GUILayout.Label($"{e.name}: {e.CurrentStateName}");
            GUILayout.Label(actual != null
                ? $"    {actual.name}   {actual.salud:0}/{actual.saludMax:0}"
                : "    Paciente: -");
        }

        GUILayout.Label($"Cola: {pharmacy.clients.Count}    Camillas libres: {pharmacy.emptyBeds.Count}/{totalCamillas}");
        GUILayout.Label($"Atendidos: {atendidos}/{spawneados.Count}");

        if (GUILayout.Button($"Nuevo paciente ({teclaSpawn})")) SpawnPaciente();
        if (GUILayout.Button("5 pacientes")) SpawnCinco();

        bool puedeAñadir = enfermeras.Count < maxEnfermeras;
        GUI.enabled = puedeAñadir;
        string textoEnfermera = puedeAñadir
            ? $"Nueva enfermera ({teclaEnfermera})  {enfermeras.Count}/{maxEnfermeras}"
            : $"Enfermeras al máximo  {enfermeras.Count}/{maxEnfermeras}";
        if (GUILayout.Button(textoEnfermera)) SpawnEnfermera();
        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
