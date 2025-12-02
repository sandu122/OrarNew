namespace OrarUniver;

public class OrarGrupa
{
    public int Id { get; set; }          // ID numeric (entity.id pentru grupa)
    public string Grupa { get; set; }
    public int Year { get; }
    public List<SlotOrar> Sloturi { get; set; }
    public List<Disciplina> Discipline { get; set; } = new();

    private static readonly string[] Zile = { "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };

    private const int NrPerechiPeZi = 6;
    private const int PreferatMaxPerechiPeZi = 4;
    private const int AbsolutMaxPerechiPeZi = 6;

    // Config: intervalele permise pe an (start, end)
    private static readonly Dictionary<int, (int start, int end)> YearPairRanges = new()
    {
        { 1, (1, 4) },
        { 2, (1, 4) },
        { 3, (3, 6) }
    };

    private readonly int _startPair;
    private readonly int _endPair;

    private bool punePePar = true;

    // Generator simplu de id pentru Activitate
    private static int _nextActivityId = 1;
    private static int NextActivityId() => _nextActivityId++;

    public OrarGrupa(int id, int year, string grupa, string name)
    {
        Id = id;
        Year = year;
        Grupa = grupa;
        Sloturi = new List<SlotOrar>();

        // Determină intervalul permis pentru anul curent; fallback la tot dacă nu există
        if (!YearPairRanges.TryGetValue(year, out var rng))
            rng = (1, NrPerechiPeZi);
        _startPair = rng.start;
        _endPair = rng.end;
        
        foreach (var zi in Zile)
        {
            for (int p = 1; p <= NrPerechiPeZi; p++)
                Sloturi.Add(new SlotOrar(zi, p, name));
        }
    }

    private bool EstePerechePermisa(int pereche) => pereche >= _startPair && pereche <= _endPair;

    private IEnumerable<SlotOrar> SloturiPermisePeZi(string zi) =>
        Sloturi.Where(s => s.Ziua == zi && EstePerechePermisa(s.Perechea));

    private int NumarPerechiZi(string ziua) =>
        Sloturi.Where(s => s.Ziua == ziua).Count(s => s.AreActivitate);

    private string AlegeZiuaCuIncarcareMinima() =>
        Sloturi.Select(s => s.Ziua)
               .Distinct()
               .OrderBy(z => NumarPerechiZi(z))
               .ThenBy(_ => Guid.NewGuid())
               .First();

    private int PerechiOcupateInZi(string zi) =>
        SloturiPermisePeZi(zi).Count(s =>
            s.ActivitateSaptamanal != null ||
            s.ActivitateSaptamanal2 != null ||
            s.ActivitatePar != null ||
            s.ActivitatePar2 != null ||
            s.ActivitateImpar != null ||
            s.ActivitateImpar2 != null);

    // Helper: verifică dacă subgrupa candidat există deja în oricare activitate din slot
    private static bool SlotContineSubgrupa(SlotOrar slot, Activitate act) =>
        act.SubgroupId != null &&
        new[]
        {
            slot.ActivitateSaptamanal, slot.ActivitateSaptamanal2,
            slot.ActivitatePar, slot.ActivitatePar2,
            slot.ActivitateImpar, slot.ActivitateImpar2
        }.Any(a => a?.SubgroupId == act.SubgroupId);

    // Helper: verifică dacă weekly ocupă complet slotul (cluster sau grupă întreagă - fără SubgroupId)
    private static bool OcupaCompletSlotWeekly(Activitate? a) =>
        a != null && a.SubgroupId == null;

    // Helpers noi pentru prelegeri
    private static bool SlotAreLectieSubgrupa(SlotOrar s) =>
        (s.ActivitateSaptamanal?.Tip == TipActivitate.Prelegere && s.ActivitateSaptamanal.SubgroupId != null) ||
        (s.ActivitateSaptamanal2?.Tip == TipActivitate.Prelegere && s.ActivitateSaptamanal2.SubgroupId != null) ||
        (s.ActivitatePar?.Tip == TipActivitate.Prelegere && s.ActivitatePar.SubgroupId != null) ||
        (s.ActivitatePar2?.Tip == TipActivitate.Prelegere && s.ActivitatePar2.SubgroupId != null) ||
        (s.ActivitateImpar?.Tip == TipActivitate.Prelegere && s.ActivitateImpar.SubgroupId != null) ||
        (s.ActivitateImpar2?.Tip == TipActivitate.Prelegere && s.ActivitateImpar2.SubgroupId != null);

    private static bool SlotAreLectieGrupa(SlotOrar s) =>
        (s.ActivitateSaptamanal?.Tip == TipActivitate.Prelegere && s.ActivitateSaptamanal.SubgroupId == null) ||
        (s.ActivitateSaptamanal2?.Tip == TipActivitate.Prelegere && s.ActivitateSaptamanal2.SubgroupId == null) ||
        (s.ActivitatePar?.Tip == TipActivitate.Prelegere && s.ActivitatePar.SubgroupId == null) ||
        (s.ActivitatePar2?.Tip == TipActivitate.Prelegere && s.ActivitatePar2.SubgroupId == null) ||
        (s.ActivitateImpar?.Tip == TipActivitate.Prelegere && s.ActivitateImpar.SubgroupId == null) ||
        (s.ActivitateImpar2?.Tip == TipActivitate.Prelegere && s.ActivitateImpar2.SubgroupId == null);

    private static bool SlotAreOriceLectie(SlotOrar s) => SlotAreLectieGrupa(s) || SlotAreLectieSubgrupa(s);


    private bool PoatePlasaInSlot(SlotOrar slot, Activitate activitate, bool cuParitate, out bool vaOcupaSlotNou)
    {
        vaOcupaSlotNou = false;

        if (!EstePerechePermisa(slot.Perechea))
            return false;

        // ------ REGULI SPECIFICE PRELEGERE ------
        if (activitate.Tip == TipActivitate.Prelegere)
        {
            bool eSub = activitate.SubgroupId != null;

            if (!cuParitate)
            {
                // Weekly prelegere
                if (eSub)
                {
                    // Subgrupă weekly
                    // Nu permitem dacă există prelegere de grupă sau aceeași subgrupă deja
                    if (SlotAreLectieGrupa(slot)) return false;
                    if (SlotContineSubgrupa(slot, activitate)) return false;

                    // Permitem dacă slotul e complet gol -> ocupă slot nou
                    bool slotGolPrelegere = !SlotAreOriceLectie(slot) &&
                                            slot.ActivitateSaptamanal == null &&
                                            slot.ActivitateSaptamanal2 == null &&
                                            slot.ActivitatePar == null &&
                                            slot.ActivitatePar2 == null &&
                                            slot.ActivitateImpar == null &&
                                            slot.ActivitateImpar2 == null;

                    if (slotGolPrelegere)
                    {
                        vaOcupaSlotNou = true;
                        return true;
                    }

                    // Dacă există deja o altă prelegere weekly de subgrupă (Saptamanal) putem pune a doua (Saptamanal2) dacă e altă subgrupă
                    if (slot.ActivitateSaptamanal != null &&
                        slot.ActivitateSaptamanal.Tip == TipActivitate.Prelegere &&
                        slot.ActivitateSaptamanal.SubgroupId != null &&
                        slot.ActivitateSaptamanal2 == null &&
                        slot.ActivitateSaptamanal.SubgroupId != activitate.SubgroupId)
                    {
                        vaOcupaSlotNou = false;
                        return true;
                    }

                    // Nu permitem colocare cu par/impar de grupă
                    if (SlotAreLectieGrupa(slot)) return false;

                    // Permitem dacă există doar par/impar de alte subgrupe și buzunar weekly primar liber
                    if (slot.ActivitateSaptamanal == null &&
                        (slot.ActivitatePar != null || slot.ActivitateImpar != null))
                    {
                        // verif să nu existe aceeași subgrupă deja
                        if (!SlotContineSubgrupa(slot, activitate))
                        {
                            vaOcupaSlotNou = false;
                            return true;
                        }
                    }

                    return false;
                }
                else
                {
                    // Prelegere de grupă weekly
                    // Blochează dacă există orice subgrupă (weekly sau parity)
                    if (SlotAreLectieSubgrupa(slot)) return false;

                    // Necesită slot complet gol (fără alte prelegeri/laboratoare)
                    bool slotGol = slot.ActivitateSaptamanal == null &&
                                   slot.ActivitateSaptamanal2 == null &&
                                   slot.ActivitatePar == null &&
                                   slot.ActivitatePar2 == null &&
                                   slot.ActivitateImpar == null &&
                                   slot.ActivitateImpar2 == null;

                    if (slotGol)
                    {
                        vaOcupaSlotNou = true;
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                // Paritate (BiWeeklySplit) prelegere
                if (eSub)
                {
                    // Paritate pentru subgrupă
                    // Nu permitem dacă există prelegere de grupă weekly sau parity
                    if (SlotAreLectieGrupa(slot)) return false;
                    // Nu permitem dacă buzunarul (par/impar) conține deja aceeași subgrupă
                    if (punePePar)
                    {
                        if (slot.ActivitatePar?.Tip == TipActivitate.Prelegere &&
                            slot.ActivitatePar.SubgroupId == activitate.SubgroupId) return false;
                        if (slot.ActivitatePar2?.Tip == TipActivitate.Prelegere &&
                            slot.ActivitatePar2.SubgroupId == activitate.SubgroupId) return false;
                    }
                    else
                    {
                        if (slot.ActivitateImpar?.Tip == TipActivitate.Prelegere &&
                            slot.ActivitateImpar.SubgroupId == activitate.SubgroupId) return false;
                        if (slot.ActivitateImpar2?.Tip == TipActivitate.Prelegere &&
                            slot.ActivitateImpar2.SubgroupId == activitate.SubgroupId) return false;
                    }

                    // Permitem dacă buzunarul primar/par secund sau impar primar/secund este liber și nu conflict de subgrupă
                    bool parPrimLiber = punePePar && slot.ActivitatePar == null;
                    bool parSecLiber = punePePar && slot.ActivitatePar != null && slot.ActivitatePar2 == null &&
                                       slot.ActivitatePar.SubgroupId != activitate.SubgroupId;
                    bool imparPrimLiber = !punePePar && slot.ActivitateImpar == null;
                    bool imparSecLiber = !punePePar && slot.ActivitateImpar != null && slot.ActivitateImpar2 == null &&
                                         slot.ActivitateImpar.SubgroupId != activitate.SubgroupId;

                    if (parPrimLiber || parSecLiber || imparPrimLiber || imparSecLiber)
                    {
                        vaOcupaSlotNou = slot.EsteLiber();
                        return true;
                    }

                    return false;
                }
                else
                {
                    // Paritate de grupă
                    // Nu permitem dacă există orice prelegere (weekly subgrupă sau grupă)
                    if (SlotAreOriceLectie(slot)) return false;

                    // Necesită buzunar par/impar liber (cel puțin unul)
                    bool parLiber = slot.ActivitatePar == null;
                    bool imparLiber = slot.ActivitateImpar == null;

                    if (parLiber || imparLiber)
                    {
                        vaOcupaSlotNou = slot.EsteLiber();
                        return true;
                    }
                    return false;
                }
            }
        }

        if (cuParitate)
        {
            // Par/impar
            if (slot.ActivitateSaptamanal != null || slot.ActivitateSaptamanal2 != null)
            {

                if (OcupaCompletSlotWeekly(slot.ActivitateSaptamanal) || OcupaCompletSlotWeekly(slot.ActivitateSaptamanal2))
                    return false;
                //  permite par/impar dacă weekly(urile) sunt laborator(e) ale altei subgrupe
                bool eLabSubgrupa = activitate.Tip == TipActivitate.Laborator && activitate.SubgroupId != null;
                if (!eLabSubgrupa) return false;

                // Dacă oricare weekly are aceeași subgrupă -> interzis
                if (SlotContineSubgrupa(slot, activitate)) return false;
            }

            bool eLabSub = activitate.Tip == TipActivitate.Laborator && activitate.SubgroupId != null;

            bool parPrimLiber = slot.ActivitatePar == null;
            bool parSecEligibil = eLabSub &&
                                  slot.ActivitatePar != null &&
                                  slot.ActivitatePar2 == null &&
                                  slot.ActivitatePar.SubgroupId != activitate.SubgroupId;

            bool imparPrimLiber = slot.ActivitateImpar == null;
            bool imparSecEligibil = eLabSub &&
                                    slot.ActivitateImpar != null &&
                                    slot.ActivitateImpar2 == null &&
                                    slot.ActivitateImpar.SubgroupId != activitate.SubgroupId;

            if (punePePar)
            {
                if (parPrimLiber)
                {
                    vaOcupaSlotNou = slot.EsteLiber();
                    return true;
                }
                if (parSecEligibil) return true;
            }
            else
            {
                if (imparPrimLiber)
                {
                    vaOcupaSlotNou = slot.EsteLiber();
                    return true;
                }
                if (imparSecEligibil) return true;
            }
            return false;
        }
        else
        {
            // Weekly
            bool slotGol = slot.ActivitateSaptamanal == null
                           && slot.ActivitateSaptamanal2 == null
                           && slot.ActivitatePar == null
                           && slot.ActivitatePar2 == null
                           && slot.ActivitateImpar == null
                           && slot.ActivitateImpar2 == null;

            if (slotGol)
            {
                vaOcupaSlotNou = true;
                return true; // buzunar primar weekly
            }

            // Weekly + existent par/impar: permis DOAR pentru laborator subgrupă diferită
            bool eLabSubgrupa = activitate.Tip == TipActivitate.Laborator && activitate.SubgroupId != null;

            if (eLabSubgrupa)
            {
                // Dacă subgrupa există deja în oricare activitate -> refuz
                if (SlotContineSubgrupa(slot, activitate))
                    return false;

                // Caz 1: primar weekly liber (nu există ActivitateSaptamanal)
                if (slot.ActivitateSaptamanal == null)
                {
                    vaOcupaSlotNou = false; // slot era deja ocupat de par/impar
                    return true;
                }

                // Caz 2: secund weekly (ActivitateSaptamanal2) liber și weekly primar e laborator altă subgrupă
                if (slot.ActivitateSaptamanal != null
                    && slot.ActivitateSaptamanal.Tip == TipActivitate.Laborator
                    && slot.ActivitateSaptamanal2 == null
                    && slot.ActivitateSaptamanal.SubgroupId != null
                    && slot.ActivitateSaptamanal.SubgroupId != activitate.SubgroupId)
                {
                    vaOcupaSlotNou = false;
                    return true;
                }
            }

            // EXCEPȚIE existentă (al doilea weekly fără par/impar) – menținută
            if (activitate.Tip == TipActivitate.Laborator
                && slot.ActivitatePar == null
                && slot.ActivitatePar2 == null
                && slot.ActivitateImpar == null
                && slot.ActivitateImpar2 == null
                && slot.ActivitateSaptamanal != null
                && slot.ActivitateSaptamanal.Tip == TipActivitate.Laborator
                && slot.ActivitateSaptamanal2 == null
                && slot.ActivitateSaptamanal.SubgroupId != null
                && activitate.SubgroupId != null
                && slot.ActivitateSaptamanal.SubgroupId != activitate.SubgroupId)
            {
                vaOcupaSlotNou = false;
                return true;
            }

            return false;
        }
    }

    private bool SlotLiber(SlotOrar slot) => slot.EsteLiber();

    private void ExecutaPlasare(SlotOrar slot, Activitate activitate, bool cuParitate)
    {
        if (cuParitate)
        {
            // Par / Impar branch
            bool eLabSub = activitate.Tip == TipActivitate.Laborator && activitate.SubgroupId != null;

            // Încercăm exact o dată pe par, apoi o dată pe impar (evităm recursia care poate crea loop)
            for (int attempt = 0; attempt < 2; attempt++)
            {
                bool targetPar = punePePar; // prima încercare folosește valoarea curentă
                if (attempt == 1) targetPar = !punePePar;

                if (targetPar)
                {
                    // PAR
                    if (slot.ActivitatePar == null)
                    {
                        slot.ActivitatePar = activitate;
                        if (attempt == 0) punePePar = false; // schimbăm direcția doar dacă a fost plasare primară la prima încercare
                        return;
                    }
                    if (eLabSub &&
                        slot.ActivitatePar.SubgroupId != activitate.SubgroupId &&
                        slot.ActivitatePar2 == null)
                    {
                        slot.ActivitatePar2 = activitate; // secundar; nu schimbăm punePePar
                        return;
                    }
                }
                else
                {
                    // IMPAR
                    if (slot.ActivitateImpar == null)
                    {
                        slot.ActivitateImpar = activitate;
                        if (attempt == 0) punePePar = true;
                        return;
                    }
                    if (eLabSub &&
                        slot.ActivitateImpar.SubgroupId != activitate.SubgroupId &&
                        slot.ActivitateImpar2 == null)
                    {
                        slot.ActivitateImpar2 = activitate;
                        return;
                    }
                }
            }

            // Nimic plasat
            return;
        }
        else
        {
            // ===== WEEKLY =====
            bool eLabSub = activitate.Tip == TipActivitate.Laborator && activitate.SubgroupId != null;

            bool slotGol =
                slot.ActivitateSaptamanal == null &&
                slot.ActivitateSaptamanal2 == null &&
                slot.ActivitatePar == null &&
                slot.ActivitatePar2 == null &&
                slot.ActivitateImpar == null &&
                slot.ActivitateImpar2 == null;

            // 1. Slot complet liber
            if (slotGol)
            {
                slot.ActivitateSaptamanal = activitate;
                return;
            }

            // 2. Nu plasăm a doua instanță pentru aceeași subgrupă
            if (eLabSub &&
                slot.ActivitateSaptamanal != null &&
                slot.ActivitateSaptamanal.Tip == TipActivitate.Laborator &&
                slot.ActivitateSaptamanal.SubgroupId == activitate.SubgroupId)
            {
                return;
            }

            // 3. Plasăm weekly primar într‑un slot deja ocupat de par/impar (laborator subgrupă diferită)
            if (eLabSub &&
                slot.ActivitateSaptamanal == null &&
                (slot.ActivitatePar != null || slot.ActivitatePar2 != null ||
                 slot.ActivitateImpar != null || slot.ActivitateImpar2 != null))
            {
                // Verifică să nu existe aceeași subgrupă deja în par/impar
                bool existaSubgrupa = new[]
                {
                    slot.ActivitatePar, slot.ActivitatePar2,
                    slot.ActivitateImpar, slot.ActivitateImpar2
                }.Any(a => a?.SubgroupId == activitate.SubgroupId);

                if (!existaSubgrupa)
                {
                    slot.ActivitateSaptamanal = activitate;
                    return;
                }
            }

            // 4. Weekly secund (Saptamanal2) doar dacă primul weekly e laborator altă subgrupă
            if (eLabSub &&
                slot.ActivitateSaptamanal != null &&
                slot.ActivitateSaptamanal.Tip == TipActivitate.Laborator &&
                slot.ActivitateSaptamanal2 == null &&
                slot.ActivitateSaptamanal.SubgroupId != null &&
                activitate.SubgroupId != null &&
                slot.ActivitateSaptamanal.SubgroupId != activitate.SubgroupId)
            {
                slot.ActivitateSaptamanal2 = activitate;
                return;
            }

            // 5. Nu plasăm nimic în alte cazuri (non-lab într-un slot parțial ocupat)
        }
    }

    private bool CreeazaGolIzolat(string zi, int pereche)
    {
        var sloturiZi = Sloturi.Where(s => s.Ziua == zi).OrderBy(s => s.Perechea).ToList();
        foreach (var (s, idx) in sloturiZi.Select((s, idx) => (s, idx)))
        {
            if (s.Perechea != pereche) continue;
            var stangaOcupat = idx > 0 && !SlotLiber(sloturiZi[idx - 1]);
            var dreaptaOcupat = idx < sloturiZi.Count - 1 && !SlotLiber(sloturiZi[idx + 1]);
            return stangaOcupat && dreaptaOcupat;
        }
        return false;
    }


    // =================== PUNCT PRINCIPAL: folosim noua structură Disciplina + Activitate ===================
    public void GenereazaOrar(List<Disciplina> discipline, List<OrarGrupa> toateGrupele)
    {
        foreach (var disc in discipline)
        {
            var perechiCant = disc.HoursPerWeek / 2.0;

            switch (disc.LessonType?.ToLower())
            {
                case "prel.":
                    bool esteComuna = disc.GroupIds.Count > 1; // cluster real
                    if (esteComuna)
                        PlaseazaComunaCuCoeficient(disc, TipActivitate.Prelegere, perechiCant, toateGrupele);
                    else
                        PlaseazaCuCoeficient(disc, TipActivitate.Prelegere, perechiCant);
                    break;

                case "sem.":
                    PlaseazaCuCoeficient(disc, TipActivitate.Seminar, perechiCant);
                    break;

                case "lab.":
                    bool subgrupaPrezent = false;
                    // fiecare subgrupă devine Activitate separată (par/impar logic identic)
                    foreach (var labGroup in disc.LabGroups)
                    {
                        foreach (var sg in labGroup.Subgroups)
                        {
                            subgrupaPrezent = true;
                            PlaseazaLaboratorSubgrupa(
                                disc,
                                labGroup.GroupId ?? 0,
                                sg.SubgroupId,
                                sg.ProfessorId,
                                sg.Professor,
                                sg.Subgroup,
                                perechiCant);
                        }
                    }

                    if (!subgrupaPrezent)
                    {
                        // fallback: laborator normal, pentru grupa întreagă
                        PlaseazaCuCoeficient(disc, TipActivitate.Laborator, perechiCant);
                    }
                    break;
            }
        }
    }

    // ========= NOU: PlaseazaCuCoeficient refăcut pentru noul model =========
    private void PlaseazaCuCoeficient(Disciplina disc, TipActivitate tip, double? coef)
    {
        if (coef == null || coef <= 0) return;

        int parteaIntreaga = (int)Math.Floor(coef.Value);
        double fract = coef.Value - parteaIntreaga;

        // Weekly (săptămânal) activități
        for (int i = 0; i < parteaIntreaga; i++)
        {
            var act = new Activitate(
                id: NextActivityId(),
                lessonId: disc.LessonId,
                lessonName: disc.LessonName,
                tip: tip,
                professorId: disc.ProfessorId,
                professor: disc.Professor,
                groupIds: new[] { Id }, // această instanță a orarului
                frequency: ActivityFrequency.Weekly
            );

            PlaseazaActivitate(act, cuParitate: false, toateGrupele: null);
        }

        // Fracțiune: par/impar
        if (fract > 0.0001)
        {
            var actBi = new Activitate(
                id: NextActivityId(),
                lessonId: disc.LessonId,
                lessonName: disc.LessonName,
                tip: tip,
                professorId: disc.ProfessorId,
                professor: disc.Professor,
                groupIds: new[] { Id },
                frequency: ActivityFrequency.BiWeeklySplit
            );

            PlaseazaActivitate(actBi, cuParitate: true, toateGrupele: null);
        }
    }

    // ========= NOU: laborator per subgrupă =========
    private void PlaseazaLaboratorSubgrupa(
        Disciplina disc,
        int groupId,
        int? subgroupId,
        int professorId,
        string? professor,
        string? subgroupName,
        double? coef)
    {
        if (coef == null || coef <= 0) return;
        int parteaIntreaga = (int)Math.Floor(coef.Value);
        double fract = coef.Value - parteaIntreaga;

        for (int i = 0; i < parteaIntreaga; i++)
        {
            var act = new Activitate(
                id: NextActivityId(),
                lessonId: disc.LessonId,
                lessonName: disc.LessonName,
                tip: TipActivitate.Laborator,
                professorId: professorId,
                professor: professor,
                groupId: groupId,
                subgroupId: subgroupId,
                frequency: ActivityFrequency.Weekly,
                subgroupName: subgroupName);
            PlaseazaActivitate(act, false, null);
        }

        if (fract > 0.0001)
        {
            var actBi = new Activitate(
                id: NextActivityId(),
                lessonId: disc.LessonId,
                lessonName: disc.LessonName,
                tip: TipActivitate.Laborator,
                professorId: professorId,
                professor: professor,
                groupId: groupId,
                subgroupId: subgroupId,
                frequency: ActivityFrequency.BiWeeklySplit,
                subgroupName: subgroupName);
            PlaseazaActivitate(actBi, true, null);
        }
    }

    // ========= NOU: prelegere comună (cluster) pe mai multe grupe simultan =========
    private void PlaseazaComunaCuCoeficient(Disciplina disc, TipActivitate tip, double? coef, List<OrarGrupa> toateGrupele)
    {
        if (coef == null || coef <= 0 || disc.GroupIds.Count <= 1) return;

        int parteaIntreaga = (int)Math.Floor(coef.Value);
        double fract = coef.Value - parteaIntreaga;

        for (int i = 0; i < parteaIntreaga; i++)
        {
            var act = new Activitate(
                id: NextActivityId(),
                lessonId: disc.LessonId,
                lessonName: disc.LessonName,
                tip: tip,
                professorId: disc.ProfessorId,
                professor: disc.Professor,
                groupIds: disc.GroupIds,   // toate grupele implicate
                frequency: ActivityFrequency.Weekly
            );
            PlaseazaComunaActivitate(act, cuParitate: false, disc.GroupIds, toateGrupele);
        }

        if (fract > 0.0001)
        {
            var actBi = new Activitate(
                id: NextActivityId(),
                lessonId: disc.LessonId,
                lessonName: disc.LessonName,
                tip: tip,
                professorId: disc.ProfessorId,
                professor: disc.Professor,
                groupIds: disc.GroupIds,
                frequency: ActivityFrequency.BiWeeklySplit
            );
            PlaseazaComunaActivitate(actBi, cuParitate: true, disc.GroupIds, toateGrupele);
        }
    }

    // Înlocuiește vechiul mecanism bazat pe Clusters
    private void PlaseazaComunaActivitate(Activitate activitate, bool cuParitate, List<int> groupIds, List<OrarGrupa> toateGrupele)
    {
        // Selectăm obiectele OrarGrupa vizate
        var grupeTarget = toateGrupele.Where(g => groupIds.Contains(g.Id)).ToList();
        if (!grupeTarget.Any()) return;

        // Evităm duplicarea: dacă oricare grupă are deja această activitate (după ConflictKey)
        bool already = grupeTarget.Any(g =>
            g.Sloturi.Any(s =>
                (s.ActivitateSaptamanal != null && s.ActivitateSaptamanal.ConflictKey == activitate.ConflictKey && s.ActivitateSaptamanal.Frequency == activitate.Frequency) ||
                (s.ActivitatePar != null && s.ActivitatePar.ConflictKey == activitate.ConflictKey && s.ActivitatePar.Frequency == activitate.Frequency) ||
                (s.ActivitateImpar != null && s.ActivitateImpar.ConflictKey == activitate.ConflictKey && s.ActivitateImpar.Frequency == activitate.Frequency)
            ));
        if (already) return;

        // Perechi comune permise tuturor grupelor implicate
        var perechiComune = Enumerable.Range(1, NrPerechiPeZi)
            .Where(p => grupeTarget.All(g => g.EstePerechePermisa(p)))
            .ToList();

        // Heuristic: încercăm zile în ordinea încărcării minime a primei grupe
        var zileInOrdine = Zile
            .OrderBy(z => grupeTarget.Sum(g => g.Sloturi.Count(s =>
                s.Ziua == z && (s.ActivitateSaptamanal != null || s.ActivitateSaptamanal2 != null || s.ActivitatePar != null || s.ActivitateImpar != null))))
            .ToList();

        foreach (var zi in zileInOrdine)
        {
            foreach (var pereche in perechiComune)
            {
                // Colectăm sloturile corespunzătoare
                var slotSet = new List<SlotOrar>(grupeTarget.Count);
                bool ok = true;

                foreach (var g in grupeTarget)
                {
                    var slot = g.Sloturi.First(s => s.Ziua == zi && s.Perechea == pereche);

                    // Verificare compatibilitate identică cu PoatePlasaInSlot
                    if (cuParitate)
                    {
                        if (slot.ActivitateSaptamanal != null ||
                            slot.ActivitateSaptamanal2 != null ||
                            (slot.ActivitatePar != null && slot.ActivitateImpar != null))
                        {
                            ok = false;
                            break;
                        }
                    }
                    else
                    {
                        // NOU: tratăm și buzunarele secundare parity ca ocupate
                        if (slot.ActivitateSaptamanal != null ||
                            slot.ActivitateSaptamanal2 != null ||
                            slot.ActivitatePar != null ||
                            slot.ActivitatePar2 != null ||
                            slot.ActivitateImpar != null ||
                            slot.ActivitateImpar2 != null)
                        {
                            ok = false;
                            break;
                        }
                    }
                    slotSet.Add(slot);
                }

                if (!ok) continue;

                // Plasăm aceeași instanță în toate grupele (pentru conflict comun)
                foreach (var slot in slotSet)
                {
                    if (cuParitate)
                    {
                        if (punePePar && slot.ActivitatePar == null)
                            slot.ActivitatePar = activitate;
                        else if (!punePePar && slot.ActivitateImpar == null)
                            slot.ActivitateImpar = activitate;
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

                if (cuParitate) punePePar = !punePePar;
                return;
            }
        }

        Console.WriteLine($"⚠ Nu am găsit slot comun pentru {activitate} (lecture comună).");
    }

    // ================== EXISTENT (nemodificat semnificativ, doar adaptat) ==================
    private void PlaseazaActivitate(Activitate activitate, bool cuParitate, List<OrarGrupa>? toateGrupele)
    {
        var ziAleasa = AlegeZiuaCuIncarcareMinima();

        // --- adaugă buzunarele secundare în verificările de conflict din PlaseazaActivitate ---
        bool AlreadyPlacedInOtherGroupSameSlot(SlotOrar candidat)
        {
            if (toateGrupele == null) return false;
            return toateGrupele
                .Where(g => g != this)
                .Any(g => g.Sloturi.Any(s =>
                    s.Ziua == candidat.Ziua &&
                    s.Perechea == candidat.Perechea &&
                    (
                        s.ActivitateSaptamanal?.ConflictKey == activitate.ConflictKey ||
                        s.ActivitateSaptamanal2?.ConflictKey == activitate.ConflictKey ||
                        s.ActivitatePar?.ConflictKey == activitate.ConflictKey ||
                        s.ActivitatePar2?.ConflictKey == activitate.ConflictKey ||
                        s.ActivitateImpar?.ConflictKey == activitate.ConflictKey ||
                        s.ActivitateImpar2?.ConflictKey == activitate.ConflictKey
                    )));
        }


        // PASS 1
        foreach (var slot in Sloturi.Where(s => s.Ziua == ziAleasa))
        {
            if (AlreadyPlacedInOtherGroupSameSlot(slot)) continue;
            if (!PoatePlasaInSlot(slot, activitate, cuParitate, out bool vaOcupaNou)) continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= PreferatMaxPerechiPeZi && !CreeazaGolIzolat(slot.Ziua, slot.Perechea))
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                return;
            }
        }

        // PASS 2
        foreach (var slot in Sloturi.Where(s => s.Ziua == ziAleasa))
        {
            if (AlreadyPlacedInOtherGroupSameSlot(slot)) continue;
            if (!PoatePlasaInSlot(slot, activitate, cuParitate, out bool vaOcupaNou)) continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= PreferatMaxPerechiPeZi)
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                return;
            }
        }

        // PASS 3
        foreach (var slot in Sloturi.Where(s => s.Ziua == ziAleasa))
        {
            if (AlreadyPlacedInOtherGroupSameSlot(slot)) continue;
            if (!PoatePlasaInSlot(slot, activitate, cuParitate, out bool vaOcupaNou)) continue;

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

    public void Afiseaza()
    {
        Console.WriteLine($"Orar pentru grupa {Grupa}:\n");
        foreach (var slot in Sloturi)
            Console.WriteLine(slot);
    }
}
