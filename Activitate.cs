namespace OrarUniver
{
    public class Activitate
    {
        public string Disciplina { get; set; }
        public TipActivitate Tip { get; set; }

        public Activitate(string disciplina, TipActivitate tip)
        {
            Disciplina = disciplina;
            Tip = tip;
        }

        public override string ToString()
        {
            return $"{Disciplina} ({Tip})";
        }
    }
}
