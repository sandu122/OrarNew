using System.Diagnostics.Metrics;

namespace OrarUniver
{
    public class Disciplina
    {
        public int Id { get; set; }
        public int IdPlan { get; set; }
        public int IdEntity { get; set; }
        public string? Denumire { get; set; }
        public double? OreCurs { get; set; }
        public double? OreSeminar { get; set; }
        public double? OreLaborator { get; set; }
        public bool EsteComuna { get; set; } = false;
        public List<Cluster?>? Clusters { get; set; }
    }

    public class Cluster
    {
        public string? Name { get; set; }
        public List<string?>? Groups { get; set; }
    }
}
