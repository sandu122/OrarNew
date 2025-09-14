using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;


namespace OrarUniver;

[ApiController]
[Route("")]
public class MainController(IDb db) : ControllerBase
{   
    private readonly IDb _db = db;
       
    [HttpGet("Entity")]
    public ActionResult GetEntity() => Ok(_db.GetEntity());

    [HttpGet("StdPlanM")]
    public ActionResult GetStdPlanM() => Ok("StdPlanM");

    [HttpGet("StdPlanD")]
    public ActionResult GetStdPlanD() => Ok("StdPlanD");

    [HttpGet("Start")]
    public ActionResult Start() => Ok(_db.Start());
}

public interface IDb 
{
    public IEnumerable<Entity> GetEntity();
    public IEnumerable<StdPlanM> GetStdPlanM();
    public IEnumerable<VStdPlanD> GetStdPlanD();
    public IEnumerable<SlotOrar> Start();
}

public class Db(NpgsqlConnection dbConnect) : IDb
{
    private readonly NpgsqlConnection _dbConnect = dbConnect;
    ///private NpgsqlTransaction? _tr;
    public IEnumerable<Entity> GetEntity() 
    { 
        string strQ = "SELECT * FROM entity";

        var res = _dbConnect.Query<Entity>(strQ);
        return res;
    }

    public IEnumerable<StdPlanM> GetStdPlanM()
    {
        string strQ = "SELECT * FROM std_plan_m";
        var res = _dbConnect.Query<StdPlanM>(strQ);
        return res;
    }

    public IEnumerable<VStdPlanD> GetStdPlanD()
    {
        string strQ = "SELECT * FROM v_std_plan_d";
        var res = _dbConnect.Query<VStdPlanD>(strQ);
        return res;
    }

    public IEnumerable<SlotOrar> Start()
    {
        var planDCall = GetStdPlanD();

        //Disciplina disciplina = new();
        List<Disciplina> disciplinaInfromatica = new();

        foreach (var line in planDCall.Where(m => m.IdPlan == 1))
        {
            var disciplina = new Disciplina()
            {
                Denumire = line.Name,
                OreCurs = line.CursCant,
                OreSeminar = line.SeminarCant,
                OreLaborator = line.LaboratorCant
            };
            disciplinaInfromatica.Add(disciplina);
        }

        List<Disciplina> disciplinaInfromaticaAplicata = new();

        foreach (var line in planDCall.Where(m => m.IdPlan == 2))
        {
            var disciplina = new Disciplina()
            {
                Denumire = line.Name,
                OreCurs = line.CursCant,
                OreSeminar = line.SeminarCant,
                OreLaborator = line.LaboratorCant
            };
            disciplinaInfromaticaAplicata.Add(disciplina);
        }

        var orarInfo = new OrarGrupa("I2301(ro)", disciplinaInfromatica, "disciplinaInfromatica");
        var orarInfoA = new OrarGrupa("IA2301(ro)", disciplinaInfromaticaAplicata, "disciplinaInfromaticaAplicata");

        var toateGrupele = new List<OrarGrupa> { orarInfo, orarInfoA };
        var listDiscipline = new List<SlotOrar> ();
        listDiscipline.AddRange(orarInfo.Sloturi);
        listDiscipline.AddRange(orarInfoA.Sloturi);

        orarInfo.GenereazaOrar(disciplinaInfromatica, 15, toateGrupele);
        orarInfoA.GenereazaOrar(disciplinaInfromaticaAplicata, 15, toateGrupele);

        return listDiscipline;
    }


}


