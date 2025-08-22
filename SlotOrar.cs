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

        public override string ToString()
        {
            if (ActivitateSaptamanal != null)
                return $"{Ziua} P{Perechea}: {ActivitateSaptamanal}";

            if (ActivitateImpar != null && ActivitatePar != null)
                return $"{Ziua} P{Perechea}: {ActivitateImpar} [impar] / {ActivitatePar} [par]";

            if (ActivitateImpar != null)
                return $"{Ziua} P{Perechea}: {ActivitateImpar} [impar]";

            if (ActivitatePar != null)
                return $"{Ziua} P{Perechea}: {ActivitatePar} [par]";

            return $"{Ziua} P{Perechea}: liber";
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
