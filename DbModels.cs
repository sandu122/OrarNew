namespace OrarUniver
{
    public class Entity
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

    public class VStdPlanD : StdPlanD
    {
        public string? ObjName { get; set; }
        public string? Comun { get; set; }
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
    }
}
