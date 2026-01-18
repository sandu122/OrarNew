namespace OrarUniver
{
    public partial class OrarGrupa
    {
        private void PlaseazaCuCoeficient(Disciplina disc, TipActivitate tip, double? coef, List<Entity> rooms)
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
                    tip: tip,
                    professorId: disc.ProfessorId,
                    professor: disc.Professor,
                    roomId: null,
                    roomName: null,
                    groupIds: new[] { Id },
                    frequency: ActivityFrequency.Weekly
                );

                PlaseazaActivitate(act, cuParitate: false, toateGrupele: null, rooms: rooms);
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
                    roomId: null,
                    roomName: null,
                    groupIds: new[] { Id },
                    frequency: ActivityFrequency.BiWeeklySplit
                );

                PlaseazaActivitate(actBi, cuParitate: true, toateGrupele: null, rooms: rooms);
            }
        }

        private void PlaseazaLaboratorSubgrupa(
            Disciplina disc,
            int groupId,
            int? subgroupId,
            int professorId,
            string? professor,
            string? subgroupName,
            double? coef,
            List<Entity> rooms)
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
                    roomId: null,
                    roomName: null,
                    groupId: groupId,
                    subgroupId: subgroupId,
                    frequency: ActivityFrequency.Weekly,
                    subgroupName: subgroupName);

                PlaseazaActivitate(act, false, null, rooms);
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
                    roomId: null,
                    roomName: null,
                    groupId: groupId,
                    subgroupId: subgroupId,
                    frequency: ActivityFrequency.BiWeeklySplit,
                    subgroupName: subgroupName);

                PlaseazaActivitate(actBi, true, null, rooms);
            }
        }

        private void PlaseazaComunaCuCoeficient(Disciplina disc, TipActivitate tip, double? coef, List<OrarGrupa> toateGrupele, List<Entity> rooms)
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
                    roomId: null,
                    roomName: null,
                    groupIds: disc.GroupIds,
                    frequency: ActivityFrequency.Weekly
                );

                PlaseazaComunaActivitate(act, cuParitate: false, disc.GroupIds, toateGrupele, rooms);
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
                    roomId: null,
                    roomName: null,
                    groupIds: disc.GroupIds,
                    frequency: ActivityFrequency.BiWeeklySplit
                );

                PlaseazaComunaActivitate(actBi, cuParitate: true, disc.GroupIds, toateGrupele, rooms);
            }
        }
    }
}
