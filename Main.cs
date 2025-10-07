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
    public IEnumerable<VProfLectCluster> GetProfLectCluster();
    public IEnumerable<SlotOrar> Start();
}

public class Db(NpgsqlConnection dbConnect) : IDb
{
    private readonly NpgsqlConnection _dbConnect = dbConnect;

    public IEnumerable<VProfLectCluster> GetProfLectCluster()
    {
        string strQ = "SELECT * FROM v_prof_lect_cluster_2";
        var res = _dbConnect.Query<VProfLectCluster>(strQ);
        return res;
    }

    public IEnumerable<SlotOrar> Start()
    {
        var infoPlanEx = GetProfLectCluster().ToList();
        var assignments = infoPlanEx
                    .GroupBy(r => new { r.LessonId, r.LessonName, r.LessonType, r.CantPWeek, r.ProfessorId, r.Professor })
                    .Select(g =>
                    {
                        var assignment = new Disciplina
                        {
                            LessonId = g.Key.LessonId,
                            LessonName = g.Key.LessonName,
                            LessonType = g.Key.LessonType,
                            HoursPerWeek = g.Key.CantPWeek,
                            ProfessorId = g.Key.ProfessorId,
                            Professor = g.Key.Professor
                        };

                        switch (g.Key.LessonType?.ToLower())
                        {
                            case "prelegere":
                                var target = g.First();
                                bool isCluster = string.Equals(target.TargetType, "cluster", StringComparison.OrdinalIgnoreCase);

                                if (isCluster)
                                {
                                    var groupRows = infoPlanEx
                                        .Where(x =>
                                            string.Equals(x.TargetType, "group", StringComparison.OrdinalIgnoreCase) &&
                                            x.ParentName == target.TargetName)
                                        .Distinct()
                                        .ToList();

                                    assignment.Groups = groupRows
                                        .Select(x => x.TargetName!)
                                        .Distinct()
                                        .ToList();

                                    assignment.GroupIds = groupRows
                                        .Select(x => x.TargetId)
                                        .Distinct()
                                        .ToList();
                                }
                                else
                                {
                                    // este grupă: folosim propriul ID și (dacă dorești) numele părintelui ca afișare
                                    var groupId = target.TargetId;
                                    var groupName = target.ParentName ?? target.TargetName;

                                    assignment.Groups = new List<string> { groupName! };
                                    assignment.GroupIds = new List<int> { groupId };
                                }
                                break;

                            case "seminar":
                                assignment.Group = g.Select(x => x.TargetName).FirstOrDefault();
                                assignment.GroupId = g.Select(x => (int?)x.TargetId).FirstOrDefault();
                                break;

                            case "laborator":
                                assignment.LabGroups = g
                                    .GroupBy(x => new { x.ParentName, x.ParentId })
                                    .Select(gr => new LabGroup
                                    {
                                        Group = gr.Key.ParentName,
                                        GroupId = gr.Key.ParentId,
                                        Subgroups = gr.Select(x => new LabSubgroup
                                        {
                                            Subgroup = x.TargetName,
                                            SubgroupId = x.TargetId,
                                            Professor = x.Professor,
                                            ProfessorId = x.ProfessorId
                                        }).ToList()
                                    })
                                    .ToList();
                                break;
                        }

                        return assignment;
                    })
                    .ToList();

        // Instanțiere grupe cu ID-urile reale din entity (30,31,32 etc.)
        var orarInfo = new OrarGrupa(30, "I2301(ro)", "disciplinaInformatica");
        var orarInfoA = new OrarGrupa(31, "IA2301(ro)", "disciplinaInformaticaAplicata");
        var orarInfoA2 = new OrarGrupa(32, "IA2302(ro)", "disciplinaInformaticaAplicata2");

        var toateGrupele = new List<OrarGrupa> { orarInfo, orarInfoA, orarInfoA2 };

        // Selectăm disciplinele legate de fiecare grupă după ID
        foreach (var grupa in toateGrupele)
        {
            var disciplineForGroup = assignments
                .Select(a => ProjectForGroup(a, grupa.Id))
                .Where(a => a != null)
                .Cast<Disciplina>()
                .ToList();

            //grupa.Discipline.AddRange(disciplineForGroup);
            grupa.GenereazaOrar(disciplineForGroup, toateGrupele);
        }

        // Returnăm doar sloturile (cum era înainte)
        var listDiscipline = new List<SlotOrar>();
        foreach (var g in toateGrupele)
            listDiscipline.AddRange(g.Sloturi);

        return listDiscipline;
    }


    private static Disciplina? ProjectForGroup(Disciplina src, int groupId)
    {
        if (src.LessonType == null) return null;

        switch (src.LessonType.ToLower())
        {
            case "prelegere":
                // Dacă prelegerea nu atinge această grupă => ignorăm
                if (!src.GroupIds.Contains(groupId)) return null;

                bool multiGroup = src.GroupIds.Count > 1;

                if (multiGroup)
                {
                    // Prelegere comună (cluster): păstrăm TOATE grupele (nu filtrăm)
                    return new Disciplina
                    {
                        LessonId = src.LessonId,
                        LessonName = src.LessonName,
                        LessonType = src.LessonType,
                        HoursPerWeek = src.HoursPerWeek,
                        ProfessorId = src.ProfessorId,
                        Professor = src.Professor,
                        Groups = new List<string>(src.Groups),
                        GroupIds = new List<int>(src.GroupIds)
                    };
                }
                else
                {
                    // Prelegere pentru o singură grupă: doar dacă e chiar aceasta
                    return new Disciplina
                    {
                        LessonId = src.LessonId,
                        LessonName = src.LessonName,
                        LessonType = src.LessonType,
                        HoursPerWeek = src.HoursPerWeek,
                        ProfessorId = src.ProfessorId,
                        Professor = src.Professor,
                        Groups = new List<string>(src.Groups),
                        GroupIds = new List<int>(src.GroupIds)
                    };
                }

            case "seminar":
                if (src.GroupId != groupId) return null;
                return new Disciplina
                {
                    LessonId = src.LessonId,
                    LessonName = src.LessonName,
                    LessonType = src.LessonType,
                    HoursPerWeek = src.HoursPerWeek,
                    ProfessorId = src.ProfessorId,
                    Professor = src.Professor,
                    Group = src.Group,
                    GroupId = src.GroupId
                };

            case "laborator":
                // Găsim doar LabGroup-ul relevant
                var labGroup = src.LabGroups.FirstOrDefault(lg => lg.GroupId == groupId);
                if (labGroup == null) return null;

                return new Disciplina
                {
                    LessonId = src.LessonId,
                    LessonName = src.LessonName,
                    LessonType = src.LessonType,
                    HoursPerWeek = src.HoursPerWeek,
                    ProfessorId = src.ProfessorId,
                    Professor = src.Professor,
                    LabGroups = new List<LabGroup>
                    {
                        new LabGroup
                        {
                            Group = labGroup.Group,
                            GroupId = labGroup.GroupId,
                            Subgroups = labGroup.Subgroups
                                .Select(sg => new LabSubgroup
                                {
                                    Subgroup = sg.Subgroup,
                                    SubgroupId = sg.SubgroupId,
                                    Professor = sg.Professor,
                                    ProfessorId = sg.ProfessorId
                                })
                                .ToList()
                        }
                    }
                };

            default:
                return null;
        }
    }

}


