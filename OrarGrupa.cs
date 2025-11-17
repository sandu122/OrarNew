namespace OrarUniver;

public class OrarGrupa
{
    public int Id { get; set; }          // ID numeric (entity.id pentru grupa)
    public string Grupa { get; set; }
    public List<SlotOrar> Sloturi { get; set; }
    public List<Disciplina> Discipline { get; set; } = new();

    private static readonly string[] Zile = { "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };
    private const int NrPerechiPeZi = 5;
    private const int PreferatMaxPerechiPeZi = 4;
    private const int AbsolutMaxPerechiPeZi = 5;
    private bool punePePar = true;

    // Generator simplu de id pentru Activitate
    private static int _nextActivityId = 1;
    private static int NextActivityId() => _nextActivityId++;

    public OrarGrupa(int id, string grupa, string name)
    {
        Id = id;
        Grupa = grupa;
        Sloturi = new List<SlotOrar>();
        foreach (var zi in Zile)
        {
            for (int p = 1; p <= NrPerechiPeZi; p++)
                Sloturi.Add(new SlotOrar(zi, p, name));
        }
    }

    private int NumarPerechiZi(string ziua) =>
        Sloturi.Where(s => s.Ziua == ziua).Count(s => s.AreActivitate);

    private string AlegeZiuaCuIncarcareMinima() =>
        Sloturi.Select(s => s.Ziua)
               .Distinct()
               .OrderBy(z => NumarPerechiZi(z))
               .ThenBy(_ => Guid.NewGuid())
               .First();

    private int PerechiOcupateInZi(string zi) =>
        Sloturi.Where(s => s.Ziua == zi)
               .Count(s => s.ActivitateSaptamanal != null
                        || s.ActivitateSaptamanal2 != null
                        || s.ActivitatePar != null
                        || s.ActivitateImpar != null);

    private bool PoatePlasaInSlot(SlotOrar slot, Activitate activitate, bool cuParitate, out bool vaOcupaSlotNou)
    {
        vaOcupaSlotNou = false;

        if (cuParitate)
        {
            if (slot.ActivitateSaptamanal != null || slot.ActivitateSaptamanal2 != null) return false;

            if (slot.ActivitatePar == null || slot.ActivitateImpar == null)
            {
                vaOcupaSlotNou = (slot.ActivitatePar == null && slot.ActivitateImpar == null);
                return true;
            }
            return false;
        }
        else
        {
            // Săptămânal: dacă slotul e complet gol -> OK (ocupă un slot nou)
            if (slot.ActivitateSaptamanal == null
                && slot.ActivitateSaptamanal2 == null
                && slot.ActivitatePar == null
                && slot.ActivitateImpar == null)
            {
                vaOcupaSlotNou = true;
                return true;
            }

            // EXCEPȚIE: Dacă activitatea este Laborator și există deja UN laborator săptămânal în slot,
            // permitem plasarea celui de-al doilea (nu crește ocuparea zilei).
            if (activitate.Tip == TipActivitate.Laborator
                && slot.ActivitatePar == null
                && slot.ActivitateImpar == null
                && (
                    (slot.ActivitateSaptamanal != null
                        && slot.ActivitateSaptamanal.Tip == TipActivitate.Laborator
                        && slot.ActivitateSaptamanal2 == null)
                 || (slot.ActivitateSaptamanal == null
                        && slot.ActivitateSaptamanal2 != null
                        && slot.ActivitateSaptamanal2.Tip == TipActivitate.Laborator)
                ))
            {
                vaOcupaSlotNou = false;
                return true;
            }

            return false;
        }
    }

    private bool SlotLiber(SlotOrar slot) =>
        slot.ActivitateSaptamanal == null &&
        slot.ActivitateSaptamanal2 == null &&
        slot.ActivitatePar == null &&
        slot.ActivitateImpar == null;

    private void ExecutaPlasare(SlotOrar slot, Activitate activitate, bool cuParitate)
    {
        if (cuParitate)
        {
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
            // Săptămânal: primar sau secundar (pentru laborator)
            if (slot.ActivitateSaptamanal == null)
            {
                slot.ActivitateSaptamanal = activitate;
            }
            else if (activitate.Tip == TipActivitate.Laborator && slot.ActivitateSaptamanal2 == null)
            {
                slot.ActivitateSaptamanal2 = activitate;
            }
            else
            {
                // fallback dacă logica a ajuns aici din greșeală
                slot.ActivitateSaptamanal ??= activitate;
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
                case "prelegere":
                    bool esteComuna = disc.GroupIds.Count > 1; // cluster real
                    if (esteComuna)
                        PlaseazaComunaCuCoeficient(disc, TipActivitate.Prelegere, perechiCant, toateGrupele);
                    else
                        PlaseazaCuCoeficient(disc, TipActivitate.Prelegere, perechiCant);
                    break;

                case "seminar":
                    PlaseazaCuCoeficient(disc, TipActivitate.Seminar, perechiCant);
                    break;

                case "laborator":
                    // fiecare subgrupă devine Activitate separată (par/impar logic identic)
                    foreach (var labGroup in disc.LabGroups)
                    {
                        foreach (var sg in labGroup.Subgroups)
                        {
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
                (s.ActivitateSaptamanal != null && s.ActivitateSaptamanal.ConflictKey == activitate.ConflictKey) ||
                (s.ActivitateSaptamanal2 != null && s.ActivitateSaptamanal2.ConflictKey == activitate.ConflictKey) ||
                (s.ActivitatePar != null && s.ActivitatePar.ConflictKey == activitate.ConflictKey) ||
                (s.ActivitateImpar != null && s.ActivitateImpar.ConflictKey == activitate.ConflictKey)
            ));
        if (already) return;

        // Heuristic: încercăm zile în ordinea încărcării minime a primei grupe
        var zileInOrdine = Zile
            .OrderBy(z => grupeTarget.Sum(g => g.Sloturi.Count(s =>
                s.Ziua == z && (s.ActivitateSaptamanal != null || s.ActivitateSaptamanal2 != null || s.ActivitatePar != null || s.ActivitateImpar != null))))
            .ToList();

        foreach (var zi in zileInOrdine)
        {
            for (int pereche = 1; pereche <= NrPerechiPeZi; pereche++)
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
                        if (slot.ActivitateSaptamanal != null ||
                            slot.ActivitateSaptamanal2 != null ||
                            slot.ActivitatePar != null ||
                            slot.ActivitateImpar != null)
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
                        s.ActivitateImpar?.ConflictKey == activitate.ConflictKey
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
