namespace OrarUniver
{
    public class SlotOrar
    {
        public int Id { get; set; }
        public string Ziua { get; set; }
        public int Perechea { get; set; }
        public string Disciplina { get; set; }
        public Activitate? ActivitateSaptamanal { get; set; }
        // NOU: al doilea “buzunar” săptămânal (ex. a doua subgrupă la laborator)
        public Activitate? ActivitateSaptamanal2 { get; set; }
        public Activitate? ActivitateImpar { get; set; }
        public Activitate? ActivitateImpar2 { get; set; }
        public Activitate? ActivitatePar { get; set; }
        public Activitate? ActivitatePar2 { get; set; }

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
            ActivitateImpar != null ||
            ActivitatePar2 != null ||
            ActivitateImpar2 != null;

        public bool EsteLiber() =>
            ActivitateSaptamanal == null &&
            ActivitateSaptamanal2 == null &&
            ActivitatePar == null &&
            ActivitatePar2 == null &&
            ActivitateImpar == null &&
            ActivitateImpar2 == null;

        private static string FormatActivitate(Activitate act, string freqLabel) =>
            $"{freqLabel}:{act.LessonName}({act.Tip}){(act.SubgroupName != null ? "-" + act.SubgroupName : "")}";


        public override string ToString()
        {
            var parts = new List<string>();
            if (ActivitateSaptamanal != null) parts.Add(FormatActivitate(ActivitateSaptamanal, "S"));
            if (ActivitateSaptamanal2 != null) parts.Add(FormatActivitate(ActivitateSaptamanal2, "S2"));
            if (ActivitatePar != null) parts.Add(FormatActivitate(ActivitatePar, "P"));
            if (ActivitatePar2 != null) parts.Add(FormatActivitate(ActivitatePar2, "P2"));
            if (ActivitateImpar != null) parts.Add(FormatActivitate(ActivitateImpar, "I"));
            if (ActivitateImpar2 != null) parts.Add(FormatActivitate(ActivitateImpar2, "I2"));
            var content = parts.Count == 0 ? "(gol)" : string.Join(" | ", parts);
            return $"{Ziua} #{Perechea}: {content}";
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
