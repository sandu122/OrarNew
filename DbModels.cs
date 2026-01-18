using System.Diagnostics.Metrics;

namespace OrarUniver
{
    public class Entity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Filt1 { get; set; }
        public string? Filt2 { get; set; }
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

    public class  VClusterGroup : ClusterGroup
    {
        public string? EntityName { get; set; }
    }

}
