namespace OrarUniver
{
    public class SlotOrar
    {
        public int Id { get; set; }
        public string Ziua { get; set; }
        public int Perechea { get; set; }
        public string Disciplina { get; set; }

        public Activitate? ActivitateSaptamanal { get; set; }
        public Activitate? ActivitateImpar { get; set; }
        public Activitate? ActivitatePar { get; set; }

        public SlotOrar(string ziua, int perechea, string disciplina)
        {
            Ziua = ziua;
            Perechea = perechea;
            Disciplina = disciplina;
        }

        public bool AreActivitate =>
            ActivitateSaptamanal != null ||
            ActivitatePar != null ||
            ActivitateImpar != null;

        private static string FormatActivitate(Activitate act, string freqLabel)
        {
            // Subgrupa: afișăm doar partea după ultimul '-' (ex: I2301-1 => 1)
            string? shortSub = null;
            if (!string.IsNullOrWhiteSpace(act.SubgroupName))
            {
                var idx = act.SubgroupName.LastIndexOf('-');
                shortSub = idx >= 0 && idx < act.SubgroupName.Length - 1
                    ? act.SubgroupName[(idx + 1)..]
                    : act.SubgroupName;
            }

            // Profesor
            var prof = string.IsNullOrWhiteSpace(act.Professor) ? "" : $" Prof:{act.Professor}";

            // Subgrupă (doar dacă există)
            var sg = shortSub != null ? $" SG:{shortSub}" : "";

            return $"{act.LessonName} ({act.Tip}, {freqLabel}){prof}{sg}";
        }

        public override string ToString()
        {
            if (!AreActivitate)
                return $"{Ziua} P{Perechea}: liber";

            var lines = new List<string>();

            if (ActivitateSaptamanal != null)
                lines.Add($"{Ziua} P{Perechea}: " + FormatActivitate(ActivitateSaptamanal, "săpt"));

            if (ActivitateImpar != null)
                lines.Add($"{Ziua} P{Perechea}: " + FormatActivitate(ActivitateImpar, "impar"));

            if (ActivitatePar != null)
                lines.Add($"{Ziua} P{Perechea}: " + FormatActivitate(ActivitatePar, "par"));

            return string.Join("\n", lines);
        }

        public bool EsteLiber() =>
            ActivitateSaptamanal == null && ActivitateImpar == null && ActivitatePar == null;

        public bool PlaseazaActivitate(Activitate activitate, bool peSaptamani = false, bool impar = true)
        {
            if (!peSaptamani)
            {
                if (ActivitateSaptamanal == null)
                {
                    ActivitateSaptamanal = activitate;
                    return true;
                }
            }
            else
            {
                if (impar && ActivitateImpar == null)
                {
                    ActivitateImpar = activitate;
                    return true;
                }
                if (!impar && ActivitatePar == null)
                {
                    ActivitatePar = activitate;
                    return true;
                }
            }
            return false;
        }
    }
}
