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
    public IEnumerable<VClusterGroup> GetClusterGroup();
    public IEnumerable<SlotOrar> Start();
}

public class Db(NpgsqlConnection dbConnect) : IDb
{
    private readonly NpgsqlConnection _dbConnect = dbConnect;

    // Nou: listă simplă de legături group-subgroup
    private static List<GroupSubLink> _groupSubLinks = new();

    // Structură pentru o legătură (părinte -> subgrupă)
    private readonly record struct GroupSubLink(int GroupId, int SubgroupId);

    public IEnumerable<VProfLectCluster> GetProfLectCluster()
    {
        string strQ = "SELECT * FROM v_prof_lect_cluster_1";
        var res = _dbConnect.Query<VProfLectCluster>(strQ);
        return res;
    }

    public IEnumerable<VClusterGroup> GetClusterGroup()
    {
        string strQ = "SELECT * FROM v_cluster_group";
        var res = _dbConnect.Query<VClusterGroup>(strQ);
        return res;
    }

    public IEnumerable<SlotOrar> Start()
    {
        // Încărcăm legăturile pentru grupă–subgrupă o singură dată (Variantă minimală)
        var clusterLinks = GetClusterGroup().ToList();

        // Constrângem la legături cluster->grupă (excludem "sb-gr")
        var clusterGroupsLookup = clusterLinks
            .Where(l => !string.Equals(l.RelationType, "sb-gr", StringComparison.OrdinalIgnoreCase))
            .ToLookup(l => l.IdParent); // IdParent = clusterId, IdEntity = groupId

        // Populăm lista de legături (fără duplicate)
        _groupSubLinks = clusterLinks
            .Where(r => string.Equals(r.RelationType, "sb-gr", StringComparison.OrdinalIgnoreCase))
            .Select(r => new GroupSubLink(r.IdParent, r.IdEntity))
            .Distinct() // record struct are equality structurală
            .ToList();

        int i = 0;
        var infoPlanEx = GetProfLectCluster().ToList();

        var assignments = infoPlanEx
                    .GroupBy(r => new { r.LessonId, r.LessonName, r.LessonType, r.CantPWeek, r.ProfessorId, r.Professor, r.IdEntityC })
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
                                {
                                    var target = g.First();
                                    bool isCluster = string.Equals(target.TargetType, "cluster", StringComparison.OrdinalIgnoreCase);
                                    if (isCluster)
                                    {
                                        // Folosește id‑ul clusterului. Îl poți lua din cheie sau din target.
                                        var clusterId = g.Key.IdEntityC != 0 ? g.Key.IdEntityC : target.TargetId;

                                        var groupsForCluster = clusterGroupsLookup[clusterId];
                                        // Dacă view-ul tău include și alte relații (rare), poți totuși filtra suplimentar aici.

                                        assignment.GroupIds = groupsForCluster
                                            .Select(x => x.IdEntity)
                                            .Distinct()
                                            .ToList();

                                        // Nume din VClusterGroup.EntityName; fallback la Id dacă lipsește
                                        assignment.Groups = groupsForCluster
                                            .Select(x => x.EntityName ?? $"G{x.IdEntity}")
                                            .Distinct()
                                            .ToList();
                                    }
                                    else
                                    {
                                        var groupId = target.TargetId;
                                        var groupName = target.TargetName;
                                        assignment.Groups = new List<string> { groupName! };
                                        assignment.GroupIds = new List<int> { groupId };
                                    }
                                    break;
                                }

                            /*case "prel.":
                                var target = g.First();
                                bool isCluster = string.Equals(target.TargetType, "cluster", StringComparison.OrdinalIgnoreCase);
                                i++;
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
                                    var groupName = target.TargetName;

                                    assignment.Groups = new List<string> { groupName! };
                                    assignment.GroupIds = new List<int> { groupId };
                                }
                                break;*/

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
        var I2301 = new OrarGrupa(116, 1, "I2301", "Info_1");
        var I2302 = new OrarGrupa(119, 1, "I2302", "Info_2");
        var IA2301 = new OrarGrupa(117, 1, "IA2301", "Into_Aplicat_1");
        var IA2302 = new OrarGrupa(118, 1, "IA2302", "Into_Aplicat_2");
        var IA2303 = new OrarGrupa(120, 1, "IA2303", "Info_Aplicat_3");
        var IA2304 = new OrarGrupa(121, 1, "IA2304", "Info_Aplicat_4");
        var DJ2301 = new OrarGrupa(146, 1, "DJ2301", "Game_Design_1");
        var DJ2302 = new OrarGrupa(139, 1, "DJ2302", "Game_Design_2");
        var DJ2303 = new OrarGrupa(140, 1, "DJ2303", "Game_Design_3");
        // Anul 2
        var I2201 = new OrarGrupa(135, 2, "I2201", "Info_1");
        var I2202 = new OrarGrupa(106, 2, "I2202", "Info_2");
        var IA2201 = new OrarGrupa(136, 2, "IA2201", "Info_Aplicat_1");
        var IA2202 = new OrarGrupa(138, 2, "IA2202", "InfoInfo_Aplicat_2");
        var DJ2201 = new OrarGrupa(137, 2, "DJ2201", "Game_Design_1");
        var DJ2202 = new OrarGrupa(124, 2, "DJ2202", "Game_Design_2");
        var DJ2203 = new OrarGrupa(102, 2, "DJ2203", "Game_Design_3");
        var DJ2204 = new OrarGrupa(103, 2, "DJ2204", "Game_Design_4");
        // Anul 3
        var I2101 = new OrarGrupa(115, 3, "I2101", "Info_1");
        var I2102 = new OrarGrupa(126, 3, "I2102", "Info_1R");
        var IA2101 = new OrarGrupa(125, 3, "IA2101", "Info_Aplicat_1");
        var IA2102 = new OrarGrupa(127, 3, "I2101", "Info_Aplicat_1R");

        var toateGrupele = new List<OrarGrupa> { I2301, I2302, IA2301, IA2302, IA2303, IA2304, DJ2301, DJ2302, DJ2303,
                                                 I2201, I2202, IA2201, IA2202, DJ2201, DJ2202, DJ2203, DJ2204,
                                                 I2101, IA2101, I2102, IA2102};
        // var toateGrupele = new List<OrarGrupa> { I2201, I2202, IA2201, IA2202, DJ2201, DJ2202, DJ2203, DJ2204 };
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
                // Direct grupa sau orice subgrupă a grupei apare în src.GroupIds
                bool matches = src.GroupIds.Contains(groupId) ||
                               _groupSubLinks.Any(l => l.GroupId == groupId && src.GroupIds.Contains(l.SubgroupId));

                if (!matches) return null;
                
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



            /*case "prel.":
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
                }*/

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


