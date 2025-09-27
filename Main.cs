using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Diagnostics.Metrics;


namespace OrarUniver;

[ApiController]
[Route("")]
public class MainController(IDb db) : ControllerBase
{   
    private readonly IDb _db = db;

    [HttpGet("Start")]
    public ActionResult Start() => Ok(_db.Start());
}

public interface IDb 
{
    public IEnumerable<Entity> GetEntity();
    public IEnumerable<StdPlanM> GetStdPlanM();
    public IEnumerable<VStdPlanClusterGroupD> GetStdPlanD();
    public IEnumerable<SlotOrar> Start();
}

public class Db(NpgsqlConnection dbConnect) : IDb
{
    private readonly NpgsqlConnection _dbConnect = dbConnect;
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

    public IEnumerable<VStdPlanClusterGroupD> GetStdPlanD()
    {
        string strQ = "SELECT * FROM v_lesson_cluster_group_plan";
        var res = _dbConnect.Query<VStdPlanClusterGroupD>(strQ);
        return res;
    }

    public IEnumerable<SlotOrar> Start()
    {
        var planDCall = GetStdPlanD().ToList();

        // Single LINQ operation instead of separate common/non-common processing
        var allDiscipline = planDCall
            .GroupBy(m => m.Comun == "comun" ?
                new { m.IdEntity, m.IdPlan, Id = m.IdEntity } :
                new { m.IdEntity, m.IdPlan, Id = m.Id })
            .Select(g => new Disciplina
            {
                Id = g.First().Id,
                IdEntity = g.Key.IdEntity,
                IdPlan = g.Key.IdPlan,
                Denumire = g.First().LessonName,
                OreCurs = g.First().CursCant,
                OreSeminar = g.First().SeminarCant,
                OreLaborator = g.First().LaboratorCant,
                EsteComuna = g.First().Comun == "comun",
                Clusters = g
                    .Where(x => !string.IsNullOrEmpty(x.ClusterName))
                    .GroupBy(x => x.ClusterName)
                    .Select(cg => new Cluster
                    {
                        Name = cg.Key,
                        Groups = cg
                            .Where(x => !string.IsNullOrEmpty(x.GroupName))
                            .Select(x => x.GroupName)
                            .Distinct()
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        var orarInfo = new OrarGrupa("I2301(ro)", "disciplinaInfromatica");
        var orarInfoA = new OrarGrupa("IA2301(ro)", "disciplinaInfromaticaAplicata");
        var orarInfo2 = new OrarGrupa("I2302(ro)", "disciplinaInfromatica2");
        var orarInfoA2 = new OrarGrupa("IA2302(ro)", "disciplinaInfromaticaAplicata2");
        var toateGrupele = new List<OrarGrupa> { orarInfo, orarInfoA, orarInfo2, orarInfoA2 };

        var listDiscipline = new List<SlotOrar> ();
        listDiscipline.AddRange(orarInfo.Sloturi);
        listDiscipline.AddRange(orarInfoA.Sloturi);
        listDiscipline.AddRange(orarInfo2.Sloturi);
        listDiscipline.AddRange(orarInfoA2.Sloturi);


        var groupPlanMap = new Dictionary<string, int>
        {
            { "I2301(ro)", 1 },
            { "I2302(ro)", 1 },
            { "IA2301(ro)", 2 },
            { "IA2302(ro)", 2 }
            // Add all groups and their IdPlan
        };

        foreach (var grupa in toateGrupele)
        {
            if (groupPlanMap.TryGetValue(grupa.Grupa, out int planId))
            {
                var disciplineForGroup = allDiscipline
                    .Where(d => d.IdPlan == planId)
                    .ToList();

                grupa.GenereazaOrar(disciplineForGroup, 15, toateGrupele);
            }
        }

        return listDiscipline;
    }


}


