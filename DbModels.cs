using System.Diagnostics.Metrics;

namespace OrarUniver
{
    public class Entity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Filt { get; set; }
        public string? Filt1 { get; set; }
    }

    public class Discipline
    {
        public int Id { get; set; }
        public int IdEntity { get; set; }
        public int IdEntityT { get; set; }
        public int CantPWeek { get; set; }
        public string? LessLang { get; set; }
    }

    public class ClusterGroup
    {
        public int Id { get; set; }
        public int IdEntity { get; set; }
        public int IdParent { get; set; }
        public string? RelationType { get; set; }
    }

    public class ProfLectCluster
    {
        public int Id { get; set; }
        public int IdEntityP { get; set; }
        public int IdEntityL { get; set; }
        public int IdEntityC { get; set; }
    }

    public class VProfLectCluster : ProfLectCluster
    {
        public int LessonId { get; set; }
        public int ProfessorId { get; set; }
        public string? Professor { get; set; }
        public string? LessonName { get; set; }
        public string? LessonType { get; set; }
        public int CantPWeek { get; set; }
        public string? TargetName { get; set; }
        public string? ParentName { get; set; }
        public string? TargetType { get; set; }
        public string? RelationType { get; set; }

        // NOI: Id‑urile brute din view
        public int TargetId { get; set; }
        public int? ParentId { get; set; }
    }


    /*public class Entity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Filt { get; set; }
        public string? Filt1 { get; set; }
    }

    public class StdPlanM
    {
        public int Id { get; set; }
        public string? IdPlan { get; set; }
        public string? PlanName { get; set; }
    }

    public class StdPlanD
    {
        public int Id { get; set; }
        public int IdPlan { get; set; }
        public int IdEntity { get; set; }
        public int CursCant { get; set; }
        public int SeminarCant { get; set; }
        public int LaboratorCant { get; set; }
    }

    public class VStdPlanClusterGroupD : StdPlanD
    {
        public string? LessonName { get; set; }
        public string? Comun { get; set; }
        public string? ClusterName { get; set; }
        public string? GroupName { get; set; }
    }



    public class StdPlanW
    {
        public int Id { get; set; }
        public int IdPlan { get; set; }
        public int SemesterNr { get; set; }
        public int NrWWeeks { get; set; }
    }

    public class GCluster
    {
        public int Id { get; set; }
        public int IdCluster { get; set; }
        public int IdGroup { get; set; }
    }

    public class GCurricula
    {
        public int Id { get; set; }
        public int IdGroup { get; set; }
        public int IdCurricula { get; set; }
    }

    public class SlotOrarT
    {
        public string? Ziua { get; set; }
        public string? Perechea { get; set; }
    }*/

}
