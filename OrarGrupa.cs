namespace OrarUniver;

public class OrarGrupa
{
    public string Grupa { get; set; }
    public List<SlotOrar> Sloturi { get; set; } = new();

    private static readonly string[] Zile = { "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };
    private const int NrPerechiPeZi = 6; // ridicat la 6 pentru 36 sloturi

    private readonly Random rnd = new();
    private readonly int workWeeks = 15;

    public OrarGrupa(string grupa)
    {
        Grupa = grupa;

        foreach (var zi in Zile)
        {
            for (int p = 1; p <= NrPerechiPeZi; p++)
            {
                Sloturi.Add(new SlotOrar(zi, p));
            }
        }
    }

    public void GenereazaOrar(List<Disciplina> discipline)
    {
        foreach (var disciplina in discipline)
        {
            int perechiCurs = disciplina.OreCurs / workWeeks;
            for (int i = 0; i < perechiCurs; i++)
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Curs));

            int perechiSeminar = disciplina.OreSeminar / workWeeks;
            for (int i = 0; i < perechiSeminar; i++)
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Seminar));

            int perechiLab = disciplina.OreLaborator / workWeeks;
            for (int i = 0; i < perechiLab; i++)
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Laborator));
        }
    }

    private void PlaseazaActivitate(Activitate activitate)
    {
        // încercăm random până găsim un slot valid
        var sloturiLibere = Sloturi
            .Where(s => s.ActivitateSaptamanal == null)
            .GroupBy(s => s.Ziua)
            .SelectMany(g =>
            {
                // numărăm câte ore are deja disciplina în acea zi
                int countInZi = g.Count(s => s.ActivitateSaptamanal?.Disciplina == activitate.Disciplina);
                if (countInZi < 3) return g; // max 3/zi
                return Array.Empty<SlotOrar>();
            })
            .ToList();

        if (sloturiLibere.Any())
        {
            var slotAles = sloturiLibere[rnd.Next(sloturiLibere.Count)];
            slotAles.ActivitateSaptamanal = activitate;
        }
        else
        {
            Console.WriteLine($"⚠ Nu mai sunt sloturi libere pentru {activitate}");
        }
    }

    public void Afiseaza()
    {
        Console.WriteLine($"\n📅 Orar pentru grupa {Grupa}:\n");
        foreach (var slot in Sloturi)
        {
            Console.WriteLine(slot.ToString());
        }
    }
}
