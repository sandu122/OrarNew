using System.Diagnostics.Metrics;

namespace OrarUniver
{
    public class Disciplina
    {
        public int LessonId { get; set; }
        public string? LessonName { get; set; }
        public string? LessonType { get; set; }
        public int ProfessorId { get; set; }
        public string? Professor { get; set; }
        public int? RoomId { get; set; }//
        public string? RoomName { get; set; }//
        public int HoursPerWeek { get; set; }
        public List<string> Groups { get; set; } = new();// Prelegere: denumirile grupelor (legacy)
        public List<int> GroupIds { get; set; } = new();// Prelegere: ID‑urile grupelor
        public string? Group { get; set; }// Seminar: denumirea grupei
        public int? GroupId { get; set; }// Seminar: ID grupa
        public List<LabGroup> LabGroups { get; set; } = new();// Laborator
        public List<LectureCluster> LectureClusters { get; set; } = new();
    }

    public class LabGroup
    {
        public string? Group { get; set; }
        public int? GroupId { get; set; }
        public List<LabSubgroup> Subgroups { get; set; } = new();
    }

    public class LabSubgroup
    {
        public string? Subgroup { get; set; }
        public int? SubgroupId { get; set; }
        public string? Professor { get; set; }
        public int ProfessorId { get; set; }
    }

    public class LectureCluster
    {
        public string? ClusterName { get; set; }
        public List<string> Groups { get; set; } = new();
    }

}
