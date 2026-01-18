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

        // room info
        public int? RoomId { get; set; }
        public string? RoomName { get; set; }

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
            int? roomId,
            string? roomName,
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

            RoomId = roomId;
            RoomName = roomName;

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
            int? roomId,
            string? roomName,
            int groupId,
            int? subgroupId,
            ActivityFrequency frequency,
            string? subgroupName = null)
            : this(id, lessonId, lessonName, tip, professorId, professor, roomId, roomName, new[] { groupId }, frequency, subgroupName)
        {
            SubgroupId = subgroupId;
        }

        private string BuildConflictKey()
        {
            var groupsPart = string.Join(",", GroupIds);
            var freqPart = Frequency switch
            {
                ActivityFrequency.Weekly => "W",
                ActivityFrequency.BiWeeklySplit => "BI",
                ActivityFrequency.EvenOnly => "EV",
                ActivityFrequency.OddOnly => "OD",
                _ => "X"
            };
            var subgroupPart = SubgroupId?.ToString() ?? "0";

            if (IsCommon)
                return $"COMMON:{LessonId}:{Tip}:{ProfessorId}:{freqPart}:{groupsPart}";

            return $"G{SingleGroupId}:{LessonId}:{Tip}:{ProfessorId}:{freqPart}:{subgroupPart}";
        }

        public override string ToString() => $"{LessonName} ({Tip})";
    }
}
