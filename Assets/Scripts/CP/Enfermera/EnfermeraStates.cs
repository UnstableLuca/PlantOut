using AI.Core;

public class EsperarClientesState : State
{
    private readonly MalaHierbaEnfermera _enfermera;
    public EsperarClientesState(MalaHierbaEnfermera enfermera) => _enfermera = enfermera;

    public override string Name => "Esperando clientes";

    public override void Enter() => _enfermera.IrARecepcion();
}

public class AtenderPacienteState : State
{
    private readonly MalaHierbaEnfermera _enfermera;
    public AtenderPacienteState(MalaHierbaEnfermera enfermera) => _enfermera = enfermera;

    public override string Name => "Atendiendo paciente";
    public override void Enter() => _enfermera.AtenderPaciente();
}

public class CurarState : State
{
    private readonly MalaHierbaEnfermera _enfermera;
    public CurarState(MalaHierbaEnfermera enfermera) => _enfermera = enfermera;

    public override string Name => "Curando";

    public override void Enter() => _enfermera.StopMoving();
    public override void Update() => _enfermera.CurarPaso();
    public override void Exit() => _enfermera.FinalizarAtencion();
}
