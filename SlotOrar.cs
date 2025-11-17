namespace OrarUniver
{
    public class SlotOrar
    {
        public int Id { get; set; }
        public string Ziua { get; set; }
        public int Perechea { get; set; }
        public string Disciplina { get; set; }

        // Pentru săptămâni
        public Activitate? ActivitateSaptamanal { get; set; }
        // NOU: al doilea “buzunar” săptămânal (ex. a doua subgrupă la laborator)
        public Activitate? ActivitateSaptamanal2 { get; set; }
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
            ActivitateSaptamanal2 != null ||
            ActivitatePar != null ||
            ActivitateImpar != null;

        private static string FormatActivitate(Activitate act, string freqLabel)
        {
            // Subgrupa: afisăm sufixul după '-' (ex: I2301-1 => 1)
            string? shortSub = null;
            if (!string.IsNullOrWhiteSpace(act.SubgroupName))
            {
                var idx = act.SubgroupName.LastIndexOf('-');
                shortSub = idx >= 0 && idx < act.SubgroupName.Length - 1
                    ? act.SubgroupName[(idx + 1)..]
                    : act.SubgroupName;
            }

            var prof = string.IsNullOrWhiteSpace(act.Professor) ? "" : $" Prof:{act.Professor}";
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

            if (ActivitateSaptamanal2 != null)
                lines.Add($"{Ziua} P{Perechea}: " + FormatActivitate(ActivitateSaptamanal2, "săpt"));

            if (ActivitateImpar != null)
                lines.Add($"{Ziua} P{Perechea}: " + FormatActivitate(ActivitateImpar, "impar"));

            if (ActivitatePar != null)
                lines.Add($"{Ziua} P{Perechea}: " + FormatActivitate(ActivitatePar, "par"));

            return string.Join("\n", lines);
        }

        public bool EsteLiber()
        {
            return ActivitateSaptamanal == null
                && ActivitateSaptamanal2 == null
                && ActivitateImpar == null
                && ActivitatePar == null;
        }

        // Dacă vrei și API-ul auxiliar să accepte al doilea laborator săptămânal:
        public bool PlaseazaActivitate(Activitate activitate, bool peSaptamani = false, bool impar = true)
        {
            if (!peSaptamani) // Activitate săptămânală
            {
                if (ActivitateSaptamanal == null)
                {
                    ActivitateSaptamanal = activitate;
                    return true;
                }

                // Permitem a doua activitate săptămânală DOAR pentru laborator (subgrupe)
                if (activitate.Tip == TipActivitate.Laborator
                    && ActivitateSaptamanal.Tip == TipActivitate.Laborator
                    && ActivitateSaptamanal2 == null)
                {
                    ActivitateSaptamanal2 = activitate;
                    return true;
                }
            }
            else // Activitate alternantă (săptămâni impare/pare)
            {
                if (impar && ActivitateImpar == null)
                {
                    ActivitateImpar = activitate;
                    return true;
                }
                else if (!impar && ActivitatePar == null)
                {
                    ActivitatePar = activitate;
                    return true;
                }
            }

            return false; // dacă e ocupat deja
        }
    }
}
