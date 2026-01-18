namespace OrarUniver;

public partial class OrarGrupa
{
    private const string RoomFiltCab = "cab";
    private const string RoomTypeCursCommon = "curs-c";
    private const string RoomTypeCurs = "curs";
    private const string RoomTypeLab = "lab";

    private static IEnumerable<(Activitate Act, ActivityFrequency EffectiveFrequency)> GetAllActivitiesWithEffectiveFrequency(SlotOrar s)
    {
        // Weekly pockets
        if (s.ActivitateSaptamanal != null) yield return (s.ActivitateSaptamanal, ActivityFrequency.Weekly);
        if (s.ActivitateSaptamanal2 != null) yield return (s.ActivitateSaptamanal2, ActivityFrequency.Weekly);

        // Par/Impar pockets:
        // Important: în slot, buzunarul "Par" înseamnă efectiv săptămâni pare,
        // iar "Impar" înseamnă săptămâni impare, indiferent de ce scrie în Activitate.Frequency.
        // Asta îți permite să permiți sharing pe even/odd exact cum ai descris.
        if (s.ActivitatePar != null) yield return (s.ActivitatePar, ActivityFrequency.EvenOnly);
        if (s.ActivitatePar2 != null) yield return (s.ActivitatePar2, ActivityFrequency.EvenOnly);

        if (s.ActivitateImpar != null) yield return (s.ActivitateImpar, ActivityFrequency.OddOnly);
        if (s.ActivitateImpar2 != null) yield return (s.ActivitateImpar2, ActivityFrequency.OddOnly);
    }

    private static ActivityFrequency GetCandidateEffectiveFrequency(bool cuParitate, bool punePePar)
    {
        if (!cuParitate) return ActivityFrequency.Weekly;

        // dacă urmează să plasăm în Par => ocupă săptămâni pare, altfel impare
        return punePePar ? ActivityFrequency.EvenOnly : ActivityFrequency.OddOnly;
    }

    private static bool FrequenciesOverlap(ActivityFrequency a, ActivityFrequency b)
    {
        // Weekly se suprapune cu orice
        if (a == ActivityFrequency.Weekly || b == ActivityFrequency.Weekly)
            return true;

        // EvenOnly se suprapune doar cu EvenOnly
        if (a == ActivityFrequency.EvenOnly && b == ActivityFrequency.EvenOnly)
            return true;

        // OddOnly se suprapune doar cu OddOnly
        if (a == ActivityFrequency.OddOnly && b == ActivityFrequency.OddOnly)
            return true;

        // EvenOnly vs OddOnly nu se suprapun
        return false;
    }

    private static bool RoomMatchesActivity(Entity room, Activitate act)
    {
        if (!string.Equals(room.Filt1, RoomFiltCab, StringComparison.OrdinalIgnoreCase))
            return false;

        var roomType = room.Filt2?.Trim();
        if (string.IsNullOrWhiteSpace(roomType))
            return false;

        // Lecție comună => curs-c (indiferent că e prelegere; tu ai spus că ai camere speciale)
        if (act.IsCommon)
            return string.Equals(roomType, RoomTypeCursCommon, StringComparison.OrdinalIgnoreCase);

        // Laborator => lab
        if (act.Tip == TipActivitate.Laborator)
            return string.Equals(roomType, RoomTypeLab, StringComparison.OrdinalIgnoreCase);

        // Prelegere/Seminar => curs
        return string.Equals(roomType, RoomTypeCurs, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RoomBusyInSlot(
        List<OrarGrupa> allGroups,
        string ziua,
        int perechea,
        int roomId,
        ActivityFrequency candidateEffectiveFrequency,
        Activitate candidate)
    {
        foreach (var g in allGroups)
        {
            // fiecare grupă are sloturile, deci îl găsim după zi+pereche
            var slot = g.Sloturi.FirstOrDefault(s => s.Ziua == ziua && s.Perechea == perechea);
            if (slot == null) continue;

            foreach (var (existing, existingEffectiveFrequency) in GetAllActivitiesWithEffectiveFrequency(slot))
            {
                if (existing.RoomId != roomId) continue;

                // dacă e aceeași activitate comună (aceeași instanță sau același ConflictKey), nu e conflict
                if (ReferenceEquals(existing, candidate) || existing.ConflictKey == candidate.ConflictKey)
                    continue;

                if (FrequenciesOverlap(existingEffectiveFrequency, candidateEffectiveFrequency))
                    return true;
            }
        }

        return false;
    }

    private bool TryAssignRoomForSlot(
        Activitate activitate,
        SlotOrar slot,
        bool cuParitate,
        List<OrarGrupa> allGroups,
        List<Entity> rooms)
    {
        // Dacă deja are cameră (de ex. din DB), nu mai căutăm
        if (activitate.RoomId != null)
            return true;

        var candidateEffectiveFrequency = GetCandidateEffectiveFrequency(cuParitate, punePePar);

        foreach (var room in rooms)
        {
            if (room.Id == 0) continue;
            if (!RoomMatchesActivity(room, activitate)) continue;

            if (RoomBusyInSlot(allGroups, slot.Ziua, slot.Perechea, room.Id, candidateEffectiveFrequency, activitate))
                continue;

            activitate.RoomId = room.Id;
            activitate.RoomName = room.Name;
            return true;
        }

        return false;
    }
}