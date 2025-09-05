namespace OrarUniver
{
    public class SlotOrar
    {
        public string Ziua { get; set; }
        public int Perechea { get; set; }

        // Pentru săptămâni
        public Activitate ActivitateSaptamanal { get; set; }
        public Activitate ActivitateImpar { get; set; }
        public Activitate ActivitatePar { get; set; }

        public SlotOrar(string ziua, int perechea)
        {
            Ziua = ziua;
            Perechea = perechea;
        }

        public bool AreActivitate =>
            ActivitateSaptamanal != null ||
            ActivitatePar != null ||
            ActivitateImpar != null;

        public override string ToString()
        {
            List<string> descrieri = new();

            if (ActivitateSaptamanal != null)
                descrieri.Add($"{Ziua} P{Perechea}: {ActivitateSaptamanal.Disciplina} ({ActivitateSaptamanal.Tip}, saptamanal)");

            if (ActivitateImpar != null)
                descrieri.Add($"{Ziua} P{Perechea}: {ActivitateImpar.Disciplina} ({ActivitateImpar.Tip}, impar)");

            if (ActivitatePar != null)
                descrieri.Add($"{Ziua} P{Perechea}: {ActivitatePar.Disciplina} ({ActivitatePar.Tip}, par)");

            if (descrieri.Count == 0)
                return $"{Ziua} P{Perechea}: liber";

            return string.Join("\n", descrieri);
        }

        public bool EsteLiber()
        {
            return ActivitateSaptamanal == null && ActivitateImpar == null && ActivitatePar == null;
        }

        public bool PlaseazaActivitate(Activitate activitate, bool peSaptamani = false, bool impar = true)
        {
            if (!peSaptamani) // Activitate săptămânală
            {
                if (ActivitateSaptamanal == null)
                {
                    ActivitateSaptamanal = activitate;
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
