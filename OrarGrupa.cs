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

            // Regula pentru paritate (automat)
            bool cuParitateCurs = disciplina.OreCurs <= 15;
            bool cuParitateSeminar = disciplina.OreSeminar <= 15;
            bool cuParitateLab = disciplina.OreLaborator <= 15;

            // --- Cursuri ---
            if (perechiCurs > 0)
            {
                for (int i = 0; i < perechiCurs; i++)
                    PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Curs), false, ref indexSlot);
            }
            else if (disciplina.OreCurs > 0)
            {
                // Paritate
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Curs), true, ref indexSlot);
            }

            // --- Seminare ---
            if (perechiSeminar > 0)
            {
                for (int i = 0; i < perechiSeminar; i++)
                    PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Seminar), false, ref indexSlot);
            }
            else if (disciplina.OreSeminar > 0)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Seminar), true, ref indexSlot);
            }

            // --- Laboratoare ---
            if (perechiLab > 0)
            {
                for (int i = 0; i < perechiLab; i++)
                    PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Laborator), false, ref indexSlot);
            }
            else if (disciplina.OreLaborator > 0)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Laborator), true, ref indexSlot);
            }
        }
    }

    private bool punePePar = true;

    private void PlaseazaActivitate(Activitate activitate, bool cuParitate, ref int indexSlot)
    {
        while (indexSlot < Sloturi.Count)
        {
            var slotCurent = Sloturi[indexSlot];

            if (cuParitate)
            {
                // Alternăm par/impar ca să fie mai echilibrat
                if (punePePar && slotCurent.ActivitatePar == null)
                {
                    slotCurent.ActivitatePar = activitate;
                    punePePar = false;
                    indexSlot++;
                    return;
                }
                else if (!punePePar && slotCurent.ActivitateImpar == null)
                {
                    slotCurent.ActivitateImpar = activitate;
                    punePePar = true;
                    indexSlot++;
                    return;
                }
            }
            else
            {
                if (slotCurent.ActivitateSaptamanal == null)
                {
                    slotCurent.ActivitateSaptamanal = activitate;
                    indexSlot++;
                    return;
                }
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
