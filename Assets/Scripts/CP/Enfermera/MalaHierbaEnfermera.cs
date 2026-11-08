using AI.Core;
using UnityEngine;
using static UnityEditor.Rendering.InspectorCurveEditor;

public class MalaHierbaEnfermera : AIAgent
{
    [Header("Enfermería")]
    [SerializeField] private Pharmacy pharmacy;

    [SerializeField, Tooltip("Posición de recepción donde espera a los pacientes.")]
    private Transform puntoRecepcion;

    [SerializeField, Tooltip("Puntos de salud que recupera el paciente por segundo.")]
    private float velocidadCuracion = 10f;

    [SerializeField, Tooltip("Distancia a partir de la cual una camilla puntúa 0.")]
    private float distanciaMaxCamilla = 20f;

    public Plant PacienteActual { get; private set; }

    public Bed CamillaActual { get; private set; }

    protected override void BuildStateMachine(StateMachine fsm)
    {
        // ---------- Percepciones (Pull) — tabla de percepciones ----------
        var llegaPaciente = new Perception("LlegaPaciente", () => pharmacy.clients.Count >= 1);
        var camillaDisponible = new Perception("CamillaDisponible", () => pharmacy.emptyBeds.Count >= 1);
        var curacionCompletada = new Perception("CuracionCompletada", () => PacienteActual == null || PacienteActual.EstaCurada);

        // ---------- Percepciones internas (controlan el flujo dentro de "Atender") ----------
        var pacienteEnCamilla = new Perception("PacienteEnCamilla", () => PacienteActual != null && HasArrived);
        var atencionFallida = new Perception("AtencionFallida", () => PacienteActual == null);

        // ---------- Estados ----------
        var esperar = new EsperarClientesState(this);
        var atender = new AtenderPacienteState(this);
        var curar = new CurarState(this);

        // ---------- Transiciones (el orden es la prioridad) ----------
        fsm.AddTransition(esperar, atender, llegaPaciente.And(camillaDisponible));
        fsm.AddTransition(atender, esperar, atencionFallida);
        fsm.AddTransition(atender, curar, pacienteEnCamilla);
        fsm.AddTransition(curar, esperar, curacionCompletada);

        fsm.SetInitialState(esperar);
    }

    //ACCIONES
    public void IrARecepcion()
    {
        if (puntoRecepcion != null) MoveTo(puntoRecepcion.position);
        else StopMoving();
    }

    public bool AtenderPaciente()
    {
        if (pharmacy.clients.Count == 0 || pharmacy.emptyBeds.Count == 0) return false;

        // Utility System: elegimos la camilla con mayor puntuación
        if (!UtilitySelector.TrySelectBest(pharmacy.emptyBeds, PuntuarCamilla, out Bed camilla))
            return false;

        PacienteActual = pharmacy.clients.Dequeue();
        CamillaActual = camilla;
        pharmacy.emptyBeds.Remove(camilla);

        PacienteActual.AsignarCamilla(camilla);
        MoveTo(camilla.PuntoEnfermera);
        return true;
    }
    public void CurarPaso()
    {
        if (PacienteActual != null)
            PacienteActual.Curar(velocidadCuracion * Time.deltaTime);
    }

    public void FinalizarAtencion()
    {
        if (CamillaActual != null && !pharmacy.emptyBeds.Contains(CamillaActual))
            pharmacy.emptyBeds.Add(CamillaActual);

        if (PacienteActual != null)
            PacienteActual.DarAlta();

        PacienteActual = null;
        CamillaActual = null;
    }

    //UTIL
    private float PuntuarCamilla(Bed camilla)
    {
        float distancia = Vector3.Distance(transform.position, camilla.transform.position);
        float cercania = UtilityCurves.Inverse(distancia, 0f, distanciaMaxCamilla);
        return cercania;
    }
}
