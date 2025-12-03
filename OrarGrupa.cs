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


    // === NOI HELPERI PENTRU NOUA LOGICĂ SIMPLIFICATĂ ===
    private static bool SlotEsteGol(SlotOrar s) =>
        s.ActivitateSaptamanal == null &&
        s.ActivitateSaptamanal2 == null &&
        s.ActivitatePar == null &&
        s.ActivitatePar2 == null &&
        s.ActivitateImpar == null &&
        s.ActivitateImpar2 == null;

    // Slot închis de un weekly de grupă/cluster (SubgroupId == null)
    private static bool SlotInchisDeWeekly(SlotOrar s) =>
        (s.ActivitateSaptamanal != null && s.ActivitateSaptamanal.SubgroupId == null) ||
        (s.ActivitateSaptamanal2 != null && s.ActivitateSaptamanal2.SubgroupId == null);

    //  HELPERI PENTRU EXCLUSIVITATE SUBGRUPĂ WEEKLY ↔ PARITY ===
    private static bool SlotAreOriceSubgrupaWeekly(SlotOrar s) =>
        (s.ActivitateSaptamanal?.SubgroupId != null) ||
        (s.ActivitateSaptamanal2?.SubgroupId != null);

    private static bool SlotAreOriceSubgrupaParity(SlotOrar s) =>
        (s.ActivitatePar?.SubgroupId != null) ||
        (s.ActivitatePar2?.SubgroupId != null) ||
        (s.ActivitateImpar?.SubgroupId != null) ||
        (s.ActivitateImpar2?.SubgroupId != null);

    // Subgrupa specifică deja prezentă oriunde în slot (weekly sau parity)
    private static bool SlotContineAceeasiSubgrupaOriunde(SlotOrar s, int? subgroupId) =>
        subgroupId != null && new[]
        {
            s.ActivitateSaptamanal, s.ActivitateSaptamanal2,
            s.ActivitatePar, s.ActivitatePar2,
            s.ActivitateImpar, s.ActivitateImpar2
        }.Any(a => a?.SubgroupId == subgroupId);

    // Detectează orice Prelegere/Seminar în buzunarele de paritate (primar sau secundar)
    private static bool SlotAreLectieSauSeminarParity(SlotOrar s) =>
        (s.ActivitatePar?.Tip == TipActivitate.Prelegere || s.ActivitatePar?.Tip == TipActivitate.Seminar) ||
        (s.ActivitatePar2?.Tip == TipActivitate.Prelegere || s.ActivitatePar2?.Tip == TipActivitate.Seminar) ||
        (s.ActivitateImpar?.Tip == TipActivitate.Prelegere || s.ActivitateImpar?.Tip == TipActivitate.Seminar) ||
        (s.ActivitateImpar2?.Tip == TipActivitate.Prelegere || s.ActivitateImpar2?.Tip == TipActivitate.Seminar);

    private bool PoatePlasaInSlot(SlotOrar slot, Activitate activitate, bool cuParitate, out bool vaOcupaSlotNou)
    {
        vaOcupaSlotNou = false;
        if (!EstePerechePermisa(slot.Perechea))
            return false;

        bool eSubgrupa = activitate.SubgroupId != null;
        bool eWeekly = !cuParitate;
        bool punePar = punePePar;

        if (SlotInchisDeWeekly(slot))
            return false;

        bool existaSubgrupaWeekly = SlotAreOriceSubgrupaWeekly(slot);
        bool existaSubgrupaParity = SlotAreOriceSubgrupaParity(slot);

        // 1) Exclusivitate între weekly-subgrupă și orice paritate
        if (eWeekly && existaSubgrupaParity) return false;
        if (cuParitate && existaSubgrupaWeekly) return false;

        // 2) Subgrupa nu poate fi dublată oriunde în slot
        if (SlotContineAceeasiSubgrupaOriunde(slot, activitate.SubgroupId)) return false;

        // 3) Blocare explicită: nu plasăm Prelegere/Seminar în paritate dacă weekly are subgrupă
        if (cuParitate && (activitate.Tip == TipActivitate.Prelegere || activitate.Tip == TipActivitate.Seminar))
        {
            if (SlotAreOriceSubgrupaWeekly(slot)) return false;
        }

        // 4) Blocare laborator dacă paritatea conține Prelegere/Seminar (indiferent weekly/parity lab)
        if (activitate.Tip == TipActivitate.Laborator)
        {
            if (SlotAreLectieSauSeminarParity(slot)) return false;
        }

        // PRELEGERE + SEMINAR
        if (activitate.Tip == TipActivitate.Prelegere || activitate.Tip == TipActivitate.Seminar)
        {
            if (eWeekly)
            {
                if (!SlotEsteGol(slot)) return false;
                vaOcupaSlotNou = true;
                return true;
            }
            else
            {
                if (slot.ActivitateSaptamanal != null || slot.ActivitateSaptamanal2 != null) return false;

                if (punePar)
                {
                    if (slot.ActivitatePar != null || slot.ActivitatePar2 != null) return false;
                }
                else
                {
                    if (slot.ActivitateImpar != null || slot.ActivitateImpar2 != null) return false;
                }

                vaOcupaSlotNou = SlotEsteGol(slot);
                return true;
            }
        }

        // LABORATOR
        if (activitate.Tip == TipActivitate.Laborator)
        {
            if (!eSubgrupa)
            {
                // Grupă
                if (eWeekly)
                {
                    if (slot.ActivitateSaptamanal != null || slot.ActivitateSaptamanal2 != null) return false;
                    if (existaSubgrupaParity) return false; // exclusivitate
                    vaOcupaSlotNou = SlotEsteGol(slot);
                    return true;
                }
                else
                {
                    if (slot.ActivitateSaptamanal != null || slot.ActivitateSaptamanal2 != null) return false;
                    if (punePar)
                    {
                        if (slot.ActivitatePar != null || slot.ActivitatePar2 != null) return false;
                    }
                    else
                    {
                        if (slot.ActivitateImpar != null || slot.ActivitateImpar2 != null) return false;
                    }
                    vaOcupaSlotNou = SlotEsteGol(slot);
                    return true;
                }
            }
            else
            {
                // Subgrupă
                if (eWeekly)
                {
                    bool ambeleGrupeParity =
                        (slot.ActivitatePar != null && slot.ActivitatePar.SubgroupId == null) &&
                        (slot.ActivitateImpar != null && slot.ActivitateImpar.SubgroupId == null);
                    if (ambeleGrupeParity) return false;

                    if (slot.ActivitateSaptamanal == null)
                    {
                        vaOcupaSlotNou = SlotEsteGol(slot);
                        return true;
                    }

                    if (slot.ActivitateSaptamanal != null &&
                        slot.ActivitateSaptamanal.SubgroupId != null &&
                        slot.ActivitateSaptamanal.SubgroupId != activitate.SubgroupId &&
                        slot.ActivitateSaptamanal2 == null)
                    {
                        vaOcupaSlotNou = false;
                        return true;
                    }

                    return false;
                }
                else
                {
                    bool ambeleGrupeParity =
                        (slot.ActivitatePar != null && slot.ActivitatePar.SubgroupId == null) &&
                        (slot.ActivitateImpar != null && slot.ActivitateImpar.SubgroupId == null);
                    if (ambeleGrupeParity) return false;

                    if (punePar)
                    {
                        if ((slot.ActivitatePar != null && slot.ActivitatePar.SubgroupId == activitate.SubgroupId) ||
                            (slot.ActivitatePar2 != null && slot.ActivitatePar2.SubgroupId == activitate.SubgroupId))
                            return false;

                        if (slot.ActivitatePar == null)
                        {
                            vaOcupaSlotNou = SlotEsteGol(slot);
                            return true;
                        }
                        if (slot.ActivitatePar.SubgroupId != activitate.SubgroupId && slot.ActivitatePar2 == null)
                        {
                            vaOcupaSlotNou = false;
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        if ((slot.ActivitateImpar != null && slot.ActivitateImpar.SubgroupId == activitate.SubgroupId) ||
                            (slot.ActivitateImpar2 != null && slot.ActivitateImpar2.SubgroupId == activitate.SubgroupId))
                            return false;

                        if (slot.ActivitateImpar == null)
                        {
                            vaOcupaSlotNou = SlotEsteGol(slot);
                            return true;
                        }
                        if (slot.ActivitateImpar.SubgroupId != activitate.SubgroupId && slot.ActivitateImpar2 == null)
                        {
                            vaOcupaSlotNou = false;
                            return true;
                        }
                        return false;
                    }
                }
            }
        }

        return false;
    }

    private bool SlotLiber(SlotOrar slot) => slot.EsteLiber();

    private void ExecutaPlasare(SlotOrar slot, Activitate activitate, bool cuParitate)
    {
        bool eSub = activitate.SubgroupId != null;
        bool eWeekly = !cuParitate;

        // Siguranță pentru subgrupe
        if (eSub)
        {
            if (eWeekly && SlotAreOriceSubgrupaParity(slot)) return;
            if (!eWeekly && SlotAreOriceSubgrupaWeekly(slot)) return;
            if (SlotContineAceeasiSubgrupaOriunde(slot, activitate.SubgroupId)) return;
        }

        // Siguranță: nu scriem laborator dacă paritatea are Prelegere/Seminar
        if (activitate.Tip == TipActivitate.Laborator && SlotAreLectieSauSeminarParity(slot)) return;

        if (activitate.Tip == TipActivitate.Prelegere || activitate.Tip == TipActivitate.Seminar)
        {
            if (eWeekly)
            {
                slot.ActivitateSaptamanal = activitate;
                return;
            }
            else
            {
                // Blocare explicită: weekly conține subgrupă => nu plasăm paritate
                if (SlotAreOriceSubgrupaWeekly(slot)) return;

                if (punePePar)
                    slot.ActivitatePar = activitate;
                else
                    slot.ActivitateImpar = activitate;

                punePePar = !punePePar;
                return;
            }
        }

        if (activitate.Tip == TipActivitate.Laborator)
        {
            if (!eSub)
            {
                if (eWeekly)
                {
                    slot.ActivitateSaptamanal = activitate;
                    return;
                }
                else
                {
                    if (punePePar)
                        slot.ActivitatePar = activitate;
                    else
                        slot.ActivitateImpar = activitate;
                    punePePar = !punePePar;
                    return;
                }
            }
            else
            {
                if (eWeekly)
                {
                    if (slot.ActivitateSaptamanal == null)
                    {
                        slot.ActivitateSaptamanal = activitate;
                        return;
                    }
                    if (slot.ActivitateSaptamanal != null &&
                        slot.ActivitateSaptamanal.SubgroupId != null &&
                        slot.ActivitateSaptamanal.SubgroupId != activitate.SubgroupId &&
                        slot.ActivitateSaptamanal2 == null)
                    {
                        slot.ActivitateSaptamanal2 = activitate;
                        return;
                    }
                    return;
                }
                else
                {
                    if (punePePar)
                    {
                        if (slot.ActivitatePar == null)
                        {
                            slot.ActivitatePar = activitate;
                        }
                        else if (slot.ActivitatePar.SubgroupId != activitate.SubgroupId && slot.ActivitatePar2 == null)
                        {
                            slot.ActivitatePar2 = activitate;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (slot.ActivitateImpar == null)
                        {
                            slot.ActivitateImpar = activitate;
                        }
                        else if (slot.ActivitateImpar.SubgroupId != activitate.SubgroupId && slot.ActivitateImpar2 == null)
                        {
                            slot.ActivitateImpar2 = activitate;
                        }
                        else
                        {
                            return;
                        }
                    }
                    if ((punePePar && slot.ActivitatePar == activitate) ||
                        (!punePePar && slot.ActivitateImpar == activitate))
                        punePePar = !punePePar;
                    return;
                }
            }
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
