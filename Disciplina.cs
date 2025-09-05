namespace OrarUniver
{
    public class Disciplina
    {
        public string Denumire { get; set; }
        public double OreCurs { get; set; }
        public double OreSeminar { get; set; }
        public double OreLaborator { get; set; }

        public Disciplina(string nume, int oreCurs, int oreSeminar, int oreLaborator)
        {
            Denumire = nume;
            OreCurs = oreCurs;
            OreSeminar = oreSeminar;
            OreLaborator = oreLaborator;
        }
    }
}
