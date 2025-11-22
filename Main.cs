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
        string strQ = "SELECT * FROM v_prof_lect_cluster_4";
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
                            case "prel.":
                                var target = g.First();
                                bool isCluster = string.Equals(target.TargetType, "cluster", StringComparison.OrdinalIgnoreCase);

                                if (isCluster)
                                {
                                    var groupRows = infoPlanEx
                                        .Where(x =>
                                            string.Equals(x.TargetType, "grupa", StringComparison.OrdinalIgnoreCase) &&
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

                            case "sem.":
                                assignment.Group = g.Select(x => x.TargetName).FirstOrDefault();
                                assignment.GroupId = g.Select(x => (int?)x.TargetId).FirstOrDefault();
                                break;

                            case "lab.":
                                assignment.LabGroups = g
                                    .GroupBy(x => new { x.ParentName, x.ParentId })
                                    .Select(gr => new LabGroup
                                    {
                                        Group = gr.FirstOrDefault(x => x.TargetType == "grupa")?.TargetName
                                        ?? gr.FirstOrDefault(x => x.TargetType == "subgrupa")?.ParentName
                                        ?? gr.Key.ParentName,
                                        /*GroupId = gr.Select(x => (int?)x.TargetId).FirstOrDefault()  
                                                  ?? gr.Key.ParentId                                 
                                                  ?? 0,  */
                                        GroupId = gr.FirstOrDefault(x => x.TargetType == "grupa")?.TargetId
                                        ?? gr.FirstOrDefault(x => x.TargetType == "subgrupa")?.ParentId
                                        ?? gr.Key.ParentId,
                                        Subgroups = gr.Where(x => x.TargetType == "subgrupa").Select(x => new LabSubgroup
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
        /*var IA2304 = new OrarGrupa(56, 1, "IA2304", "Info_Aplicat_4");
        var DJ2301 = new OrarGrupa(61, 1, "DJ2301", "Game_Design_1");
        var DJ2302 = new OrarGrupa(64, 1, "DJ2302", "Game_Design_2");
        var DJ2303 = new OrarGrupa(65, 1, "DJ2303", "Game_Design_3");*/
        var I2301  = new OrarGrupa(68, 1, "I2301", "Info_1");
        /*var IA2301 = new OrarGrupa(69, 1, "IA2301", "Into_Aplicat_1");
        var IA2302 = new OrarGrupa(70, 1, "IA2302", "Into_Aplicat_2");
        var I2302  = new OrarGrupa(71, 1, "I2302", "Info_2");
        var IA2303 = new OrarGrupa(72, 1, "IA2303", "Info_Aplicat_3");*/


        //var toateGrupele = new List<OrarGrupa> { IA2304, DJ2301, DJ2302, DJ2303, I2301, IA2301, IA2302, I2302, IA2303 };
        var toateGrupele = new List<OrarGrupa> { I2301 };
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
            case "prel.":
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

            case "sem.":
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

            case "lab.":
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


