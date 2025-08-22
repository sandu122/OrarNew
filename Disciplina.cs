namespace OrarUniver
{
    public class Disciplina
    {
        public string Denumire { get; set; }
        public int OreCurs { get; set; }
        public int OreSeminar { get; set; }
        public int OreLaborator { get; set; }

        public Disciplina(string nume, int oreCurs, int oreSeminar, int oreLaborator)
        {
            Denumire = nume;
            OreCurs = oreCurs;
            OreSeminar = oreSeminar;
            OreLaborator = oreLaborator;
        }
    }
}
