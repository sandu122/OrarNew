namespace OrarUniver;

public class OrarGrupa
{
    public string Grupa { get; set; }
    public List<SlotOrar> Sloturi { get; set; } = new();

    private static readonly string[] Zile = { "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };
    private const int NrPerechiPeZi = 6; // ridicat la 6 pentru 36 sloturi

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

    public void GenereazaOrar(List<Disciplina> discipline, int nrSaptamani)
    {
        var indexSlot = 0;

        foreach (var disciplina in discipline)
        {
            // Calculăm perechi/săptămână
            int perechiCurs = disciplina.OreCurs / (2 * nrSaptamani);
            int perechiSeminar = disciplina.OreSeminar / (2 * nrSaptamani);
            int perechiLab = disciplina.OreLaborator / (2 * nrSaptamani);

            // Plasăm cursurile
            for (int i = 0; i < perechiCurs; i++)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Curs), ref indexSlot);
            }

            // Plasăm seminarele
            for (int i = 0; i < perechiSeminar; i++)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Seminar), ref indexSlot);
            }

            // Plasăm laboratoarele
            for (int i = 0; i < perechiLab; i++)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Laborator), ref indexSlot);
            }
        }
    }

    private void PlaseazaActivitate(Activitate activitate, ref int indexSlot)
    {
        while (indexSlot < Sloturi.Count)
        {
            var slotCurent = Sloturi[indexSlot];

            // câte activități din aceeași disciplină există deja în ziua curentă
            int countInZi = Sloturi
                .Where(s => s.Ziua == slotCurent.Ziua && s.ActivitateSaptamanal?.Disciplina == activitate.Disciplina)
                .Count();

            if (slotCurent.ActivitateSaptamanal == null && countInZi < 3)
            {
                slotCurent.ActivitateSaptamanal = activitate;
                indexSlot++;
                return;
            }

            indexSlot++;
        }

        Console.WriteLine($"⚠ Nu mai sunt sloturi libere pentru {activitate}");
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
