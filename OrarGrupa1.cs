namespace OrarUniver
{
    public partial class OrarGrupa
    {

        public void GenereazaOrar(List<Disciplina> discipline, List<OrarGrupa> toateGrupele, List<Entity> rooms)
        {
            foreach (var disc in discipline)
            {
                var perechiCant = disc.HoursPerWeek / 2.0;

                switch (disc.LessonType?.ToLower())
                {
                    case "prel.":
                        bool esteComuna = disc.GroupIds.Count > 1; // cluster real
                        if (esteComuna)
                            PlaseazaComunaCuCoeficient(disc, TipActivitate.Prelegere, perechiCant, toateGrupele, rooms);
                        else
                            PlaseazaCuCoeficient(disc, TipActivitate.Prelegere, perechiCant, rooms);
                        break;

                    case "sem.":
                        PlaseazaCuCoeficient(disc, TipActivitate.Seminar, perechiCant, rooms);
                        break;

                    case "lab.":
                        bool subgrupaPrezent = false;
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
                                    perechiCant,
                                    rooms);
                            }
                        }

                        if (!subgrupaPrezent)
                        {
                            PlaseazaCuCoeficient(disc, TipActivitate.Laborator, perechiCant, rooms);
                        }
                        break;
                }
            }
        }
    }
}
