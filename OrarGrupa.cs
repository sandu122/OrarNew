namespace OrarUniver;

public class OrarGrupa
{
    public string Grupa { get; set; }
    public List<SlotOrar> Sloturi { get; set; }
    public List<Disciplina> Discipline { get; set; }

    private static readonly string[] Zile = { "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };

    // Logica/Preferinta pentru nr de sloturi
    private const int NrPerechiPeZi = 5;
    private const int PreferatMaxPerechiPeZi = 4;
    private const int AbsolutMaxPerechiPeZi = 5;

    // Pentru alternarea par/impar
    private bool punePePar = true;

    public OrarGrupa(string grupa, List<Disciplina> discipline)
    {
        Grupa = grupa;
        Discipline = discipline;
        Sloturi = new List<SlotOrar>();

        // Generearea sloturi goale pentru fiecare zi si pereche
        foreach (var zi in Zile)
        {
            for (int p = 1; p <= NrPerechiPeZi; p++)
            {
                Sloturi.Add(new SlotOrar(zi, p));
            }
        }

        Discipline = discipline;
    }

    // Numarul de activitati pe o zi
    private int NumarPerechiZi(string ziua)
    {
        return Sloturi
            .Where(s => s.Ziua == ziua)
            .Count(s => s.AreActivitate);
    }

    // Ziua cu cele mai putine perechi
    private string AlegeZiuaCuIncarcareMinima()
    {
        var zile = Sloturi.Select(s => s.Ziua).Distinct().ToList();

        return zile
            .OrderBy(z => NumarPerechiZi(z))
            .ThenBy(z => Guid.NewGuid()) // mică randomizare ca să nu fie mereu Luni
            .First();
    }

    // ====== NOU: helperi pentru limitarea la 4 (preferat) sau 5 (absolut) perechi/zi ======

    private int PerechiOcupateInZi(string zi)
    {
        // un slot contează ocupat dacă are ceva saptamanal SAU are Par/Impar (oricare)
        return Sloturi.Where(s => s.Ziua == zi)
                      .Count(s => s.ActivitateSaptamanal != null
                               || s.ActivitatePar != null
                               || s.ActivitateImpar != null);
    }

    // Verifică dacă putem plasa în slotul dat și dacă asta va consuma un slot nou (crește ocuparea zilei)
    private bool PoatePlasaInSlot(SlotOrar slot, bool cuParitate, out bool vaOcupaSlotNou)
    {
        vaOcupaSlotNou = false;

        if (cuParitate)
        {
            // nu putem pune paritate peste ceva saptamanal
            if (slot.ActivitateSaptamanal != null) return false;

            // putem pune dacă măcar una din par/impar e liberă
            if (slot.ActivitatePar == null || slot.ActivitateImpar == null)
            {
                // dacă ambele sunt libere, ocupăm un slot nou
                vaOcupaSlotNou = (slot.ActivitatePar == null && slot.ActivitateImpar == null);
                return true;
            }
            return false;
        }
        else
        {
            // saptamanal: slot complet gol
            if (slot.ActivitateSaptamanal == null
                && slot.ActivitatePar == null
                && slot.ActivitateImpar == null)
            {
                vaOcupaSlotNou = true;
                return true;
            }
            return false;
        }
    }

    // Verifică dacă slotul dat e liber
    private bool SlotLiber(SlotOrar slot)
    {
        return slot.ActivitateSaptamanal == null
            && slot.ActivitatePar == null
            && slot.ActivitateImpar == null;
    }

    private void ExecutaPlasare(SlotOrar slot, Activitate activitate, bool cuParitate)
    {
        if (cuParitate)
        {
            // alternăm pentru echilibru
            if (punePePar && slot.ActivitatePar == null)
            {
                slot.ActivitatePar = activitate;
                punePePar = false;
            }
            else if (!punePePar && slot.ActivitateImpar == null)
            {
                slot.ActivitateImpar = activitate;
                punePePar = true;
            }
            else
            {
                // dacă alternanța nu se potrivește (ex. Par ocupat, Impar liber), punem unde e liber
                if (slot.ActivitatePar == null) slot.ActivitatePar = activitate;
                else if (slot.ActivitateImpar == null) slot.ActivitateImpar = activitate;
            }
        }
        else
        {
            slot.ActivitateSaptamanal = activitate;
        }
    }



    // Verifică dacă, după plasare, ziua ar avea "gol izolat" (1 ocupat, 2 liber, 3 ocupat etc.)
    private bool CreeazaGolIzolat(string zi, int pereche)
    {
        var sloturiZi = Sloturi.Where(s => s.Ziua == zi).OrderBy(s => s.Perechea).ToList();

        // verificăm pentru poziția curentă dacă ar lăsa liber între două ocupate
        foreach (var (s, idx) in sloturiZi.Select((s, idx) => (s, idx)))
        {
            if (s.Perechea == pereche)
            {
                // verificăm vecinii
                var stangaOcupat = idx > 0 && !SlotLiber(sloturiZi[idx - 1]);
                var dreaptaOcupat = idx < sloturiZi.Count - 1 && !SlotLiber(sloturiZi[idx + 1]);

                // dacă ar fi liber și între doi ocupați => gol izolat
                return stangaOcupat && dreaptaOcupat;
            }
        }
        return false;
    }

    public void GenereazaOrar(List<Disciplina> discipline, int nrSaptamani, List<OrarGrupa>? toateGrupele = null)
    {
        foreach (var disc in discipline)
        {
            double perechiCurs = disc.OreCurs / (2 * nrSaptamani);
            double perechiSeminar = disc.OreSeminar / (2 * nrSaptamani);
            double perechiLab = disc.OreLaborator / (2 * nrSaptamani);

                // Plasăm întâi Cursurile
                if (disc.OreCurs > 0)
                {
                    if (disc.EsteComuna)
                    {
                        // Plasare comună pentru mai multe grupe
                        PlaseazaComunaCuCoeficient(disc, TipActivitate.Curs, perechiCurs, toateGrupele);
                    }
                    else
                    {
                        PlaseazaCuCoeficient(disc, TipActivitate.Curs, perechiCurs);
                    }
                }

                // Plasăm Seminarii
                if (disc.OreSeminar > 0)
                    PlaseazaCuCoeficient(disc, TipActivitate.Seminar, perechiSeminar);

                // Plasăm Laboratoare
                if (disc.OreLaborator > 0)
                    PlaseazaCuCoeficient(disc, TipActivitate.Laborator, perechiLab);
            
        }
    }
    private void PlaseazaCuCoeficient(Disciplina disc, TipActivitate tip, double coef)
    {
        int parteaIntreaga = (int)Math.Floor(coef);
        double parteaFractionara = coef - parteaIntreaga;

        // 1) Plasăm partea întreagă ca "săptămânal"
        for (int i = 0; i < parteaIntreaga; i++)
        {
            PlaseazaActivitate(new Activitate(disc.Denumire, tip), false); // false = saptamanal
        }

        // 2) Dacă există fracțiune (>0), adăugăm o activitate par/impar
        if (parteaFractionara > 0.0001) // toleranță la erori floating point
        {
            PlaseazaActivitate(new Activitate(disc.Denumire, tip), true); // true = par/impar
        }
    }

    private void PlaseazaComunaCuCoeficient(Disciplina disc, TipActivitate tip, double coef, List<OrarGrupa> toateGrupele)
    {
        int parteaIntreaga = (int)Math.Floor(coef);
        double parteaFractionara = coef - parteaIntreaga;

        // 1) plasăm partea întreagă ca săptămânal
        for (int i = 0; i < parteaIntreaga; i++)
        {
            PlaseazaComunaActivitate(new Activitate(disc.Denumire, tip), false, disc, toateGrupele);
        }

        // 2) fracțiunea → par/impar
        if (parteaFractionara > 0.0001)
        {
            PlaseazaComunaActivitate(new Activitate(disc.Denumire, tip), true, disc, toateGrupele);
        }
    }

    private void PlaseazaActivitate(Activitate activitate, bool cuParitate)
    {
        var ziAleasa = AlegeZiuaCuIncarcareMinima();

        // PASS 1: fără goluri izolate + max 4/zi
        foreach (var slot in Sloturi.Where(s => s.Ziua == ziAleasa))
        {
            if (!PoatePlasaInSlot(slot, cuParitate, out bool vaOcupaNou))
                continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= PreferatMaxPerechiPeZi && !CreeazaGolIzolat(slot.Ziua, slot.Perechea))
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                return;
            }
        }

        // PASS 2: acceptăm și gol izolat dar max 4/zi
        foreach (var slot in Sloturi.Where(s => s.Ziua == ziAleasa))
        {
            if (!PoatePlasaInSlot(slot, cuParitate, out bool vaOcupaNou))
                continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= PreferatMaxPerechiPeZi)
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                return;
            }
        }

        // PASS 3: permitem al 5-lea slot, indiferent de goluri
        foreach (var slot in Sloturi.Where(s => s.Ziua == ziAleasa))
        {
            if (!PoatePlasaInSlot(slot, cuParitate, out bool vaOcupaNou))
                continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= AbsolutMaxPerechiPeZi)
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                return;
            }
        }

        Console.WriteLine($"⚠ Nu am găsit loc pentru {activitate}");
    }


    // Varianta de bază: plasează efectiv o activitate comună în același slot pentru toate grupele
    private void PlaseazaComunaActivitate(Activitate activitate, bool cuParitate, Disciplina disc, List<OrarGrupa> toateGrupele)
    {
        // selectăm grupele care participă la disciplina comună
        var grupeTarget = toateGrupele
            .Where(g => g.Grupa == this.Grupa || disc.GrupeComune.Contains(g.Grupa))
            .ToList();

        foreach (var zi in Zile)
        {
            foreach (var pereche in Enumerable.Range(1, NrPerechiPeZi))
            {
                // verificăm dacă TOATE grupele au liber în slotul curent
                bool toateLibere = grupeTarget.All(g =>
                    g.Sloturi.Any(s => s.Ziua == zi && s.Perechea == pereche && s.EsteLiber())
                );

                if (toateLibere)
                {
                    foreach (var g in grupeTarget)
                    {
                        var slot = g.Sloturi.First(s => s.Ziua == zi && s.Perechea == pereche && s.EsteLiber());
                        if (cuParitate)
                        {
                            // par/impar (alternanță)
                            if (punePePar && slot.ActivitatePar == null)
                            {
                                slot.ActivitatePar = activitate;
                                punePePar = false;
                            }
                            else if (!punePePar && slot.ActivitateImpar == null)
                            {
                                slot.ActivitateImpar = activitate;
                                punePePar = true;
                            }
                            else
                            {
                                if (slot.ActivitatePar == null) slot.ActivitatePar = activitate;
                                else if (slot.ActivitateImpar == null) slot.ActivitateImpar = activitate;
                            }
                        }
                        else
                        {
                            slot.ActivitateSaptamanal = activitate;
                        }
                    }
                    return; // am plasat cursul comun → ieșim
                }
            }
        }

        Console.WriteLine($"⚠ Nu am găsit loc pentru activitatea comună {activitate}");
    }

    public void Afiseaza()
    {
        Console.WriteLine($"Orar pentru grupa {Grupa}:\n");
        foreach (var slot in Sloturi)
        {
            Console.WriteLine(slot.ToString());
        }
    }
}
