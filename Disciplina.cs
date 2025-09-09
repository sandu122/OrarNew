namespace OrarUniver
{
    public class Disciplina
    {
        public string Denumire { get; set; }
        public double OreCurs { get; set; }
        public double OreSeminar { get; set; }
        public double OreLaborator { get; set; }

        // Obiecte comune
        public bool EsteComuna { get; set; } = false;
        public List<string> GrupeComune { get; set; } = new();

        public Disciplina(string nume, int oreCurs, int oreSeminar, int oreLaborator,
                          bool esteComuna = false, List<string>? grupeComune = null)
        {
            Denumire = nume;
            OreCurs = oreCurs;
            OreSeminar = oreSeminar;
            OreLaborator = oreLaborator;
            EsteComuna = esteComuna;
            GrupeComune = grupeComune ?? new List<string>();
        }
    }
}
