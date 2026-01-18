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

    public IEnumerable<Entity> GetEntities()
    {
        string strQ = "SELECT * FROM entity";
        var res = _dbConnect.Query<Entity>(strQ);
        return res;
    }
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
        // Lista de camere (cabinete) din entity unde Filt1 = 'cab'
        var rooms = GetEntities()
            .Where(e => string.Equals(e.Filt1, "cab", StringComparison.OrdinalIgnoreCase))
            .ToList();


        // === An academic global (ex.: 2023) ===
        int currentAcademicYear = 2023;

        // === Grupe din BD (entity, Filt1 = 'grupa') ===
        var toateGrupele = GetEntities()
            .Where(e => string.Equals(e.Filt1, "grupa", StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                if (e.Id == 0 || string.IsNullOrWhiteSpace(e.Name))
                    return null;

                var studyYear = GetStudyYearFromGroupName(e.Name!, currentAcademicYear);
                if (studyYear is null)
                    return null;

                // info suplimentar doar pentru afişare (poţi schimba după nevoie)
                var displayInfo = e.Name!;

                return new OrarGrupa(e.Id, studyYear.Value, e.Name!, displayInfo);
            })
            .Where(g => g != null)
            .Cast<OrarGrupa>()
            .ToList();


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

        // Selectăm disciplinele legate de fiecare grupă după ID
        foreach (var grupa in toateGrupele)
        {
            var disciplineForGroup = assignments
                .Select(a => ProjectForGroup(a, grupa.Id))
                .Where(a => a != null)
                .Cast<Disciplina>()
                .ToList();

            grupa.GenereazaOrar(disciplineForGroup, toateGrupele, rooms);
        }
        

        // Returnăm doar sloturile (cum era înainte)
        var listDiscipline = new List<SlotOrar>();
        foreach (var g in toateGrupele)
            listDiscipline.AddRange(g.Sloturi);

        return listDiscipline;
    }

    // simplu: caută primele 2 cifre consecutive din denumire, le interpretează ca YY
    // şi le mapează la anul de studii, pe baza anului academic curent
    private static int? GetStudyYearFromGroupName(string name, int currentAcademicYear)
    {
        name = name.Trim();

        int firstDigitIndex = -1;
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsDigit(name[i]))
            {
                firstDigitIndex = i;
                break;
            }
        }

        if (firstDigitIndex < 0 || firstDigitIndex + 1 >= name.Length)
            return null;

        string yyText = $"{name[firstDigitIndex]}{name[firstDigitIndex + 1]}";
        if (!int.TryParse(yyText, out int yy))
            return null;

        int formationYear = 2000 + yy;
        int studyYear = currentAcademicYear - formationYear + 1;

        // limitezi la 1–3 ani, modifică dacă ai programe mai lungi
        return studyYear is >= 1 and <= 3 ? studyYear : null;
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


