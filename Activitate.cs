namespace OrarUniver
{
    public enum ActivityFrequency
    {
        Weekly,        // apare în fiecare săptămână
        BiWeeklySplit, // împărțită pe Par / Impar
        EvenOnly,      // doar săptămâni pare
        OddOnly        // doar săptămâni impare
    }

    public class Activitate
    {
        public int Id { get; }
        public int LessonId { get; }
        public string? LessonName { get; }

        public TipActivitate Tip { get; }
        public int? ProfessorId { get; }
        public string? Professor { get; }

        public List<int> GroupIds { get; } = new();
        public bool IsCommon => GroupIds.Count > 1;
        public int? SingleGroupId => IsCommon ? null : GroupIds.FirstOrDefault();

        public int? SubgroupId { get; }
        public string? SubgroupName { get; }   // NOU: pentru afișare (ex: I2301-1)
        public double SlotUnits { get; }
        public ActivityFrequency Frequency { get; }
        public string ConflictKey { get; }

        public Activitate(
            int id,
            int lessonId,
            string? lessonName,
            TipActivitate tip,
            int? professorId,
            string? professor,
            IEnumerable<int> groupIds,
            ActivityFrequency frequency,
            string? subgroupName = null)
        {
            Id = id;
            LessonId = lessonId;
            LessonName = lessonName;
            Tip = tip;
            ProfessorId = professorId;
            Professor = professor;
            GroupIds = groupIds?.Distinct().ToList() ?? new List<int>();
            Frequency = frequency;
            SubgroupName = subgroupName;
            ConflictKey = BuildConflictKey();
        }

        public Activitate(
            int id,
            int lessonId,
            string? lessonName,
            TipActivitate tip,
            int? professorId,
            string? professor,
            int groupId,
            int? subgroupId,
            ActivityFrequency frequency,
            string? subgroupName = null)
            : this(id, lessonId, lessonName, tip, professorId, professor, new[] { groupId }, frequency, subgroupName)
        {
            SubgroupId = subgroupId;
        }

        private string BuildConflictKey()
        {
            if (IsCommon)
                return $"COMMON:{Tip}:{LessonId}:{ProfessorId}";
            return $"G{SingleGroupId}:{Tip}:{LessonId}:{ProfessorId}:{SubgroupId}";
        }

        public override string ToString() => $"{LessonName} ({Tip})";
    }
}
