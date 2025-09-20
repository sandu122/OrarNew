namespace OrarUniver
{
    public class Activitate
    {
        public int Id { get; set; }
        public int IdEntity { get; set; }
        public string? Disciplina { get; set; }
        public TipActivitate Tip { get; set; }

        public Activitate(int id, int idEntity, string disciplina, TipActivitate tip)
        {
            Id = id;
            IdEntity = idEntity;
            Disciplina = disciplina;
            Tip = tip;
        }

        public override string ToString()
        {
            return $"{Disciplina} ({Tip})";
        }
    }
}
