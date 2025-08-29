using System;
using System.Collections.Generic;
using System.Linq;

namespace OrarUniver;

public class OrarGrupa
{
    public string Grupa { get; set; }
    public List<SlotOrar> Sloturi { get; set; } = new();

    private static readonly string[] Zile = { "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };

    // Generăm 5 sloturi/zi cu preferinta de utilizare doar 4
    private const int NrPerechiPeZi = 5;
    private const int PreferatMaxPerechiPeZi = 4;
    private const int AbsolutMaxPerechiPeZi = 5;

    // Pentru alternarea par/impar
    private bool punePePar = true;

    public OrarGrupa(string grupa)
    {
        Grupa = grupa;

        // generăm sloturile goale
        foreach (var zi in Zile)
        {
            for (int p = 1; p <= NrPerechiPeZi; p++)
            {
                Sloturi.Add(new SlotOrar(zi, p));
            }
        }
    }

    public void GenereazaOrar(List<Disciplina> discipline, int nrSaptamani)
    {
        var indexSlot = 0;

        foreach (var disciplina in discipline)
        {
            int perechiCurs = disciplina.OreCurs / (2 * nrSaptamani);
            int perechiSeminar = disciplina.OreSeminar / (2 * nrSaptamani);
            int perechiLab = disciplina.OreLaborator / (2 * nrSaptamani);

            // --- Cursuri ---
            if (perechiCurs > 0)
            {
                for (int i = 0; i < perechiCurs; i++)
                    PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Curs), false, ref indexSlot);
            }
            else if (disciplina.OreCurs > 0)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Curs), true, ref indexSlot);
            }

            // --- Seminare ---
            if (perechiSeminar > 0)
            {
                for (int i = 0; i < perechiSeminar; i++)
                    PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Seminar), false, ref indexSlot);
            }
            else if (disciplina.OreSeminar > 0)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Seminar), true, ref indexSlot);
            }

            // --- Laboratoare ---
            if (perechiLab > 0)
            {
                for (int i = 0; i < perechiLab; i++)
                    PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Laborator), false, ref indexSlot);
            }
            else if (disciplina.OreLaborator > 0)
            {
                PlaseazaActivitate(new Activitate(disciplina.Denumire, TipActivitate.Laborator), true, ref indexSlot);
            }
        }
    }

    // ====== NOU: helperi pentru limitarea la 4 (preferat) sau 5 (absolut) perechi/zi ======

    private int PerechiOcupateInZi(string zi)
    {
        // un slot contează ocupat dacă are ceva saptamanal SAU are Par/Impar (oricare)
        return Sloturi.Where(s => s.Ziua == zi)
                      .Count(s => s.ActivitateSaptamanal != null
                               || s.ActivitatePar != null
                               || s.ActivitateImpar != null);
    }

    // Verifică dacă putem plasa în slotul dat și dacă asta va consuma un slot nou (crește ocuparea zilei)
    private bool PoatePlasaInSlot(SlotOrar slot, bool cuParitate, out bool vaOcupaSlotNou)
    {
        vaOcupaSlotNou = false;

        if (cuParitate)
        {
            // nu putem pune paritate peste ceva saptamanal
            if (slot.ActivitateSaptamanal != null) return false;

            // putem pune dacă măcar una din par/impar e liberă
            if (slot.ActivitatePar == null || slot.ActivitateImpar == null)
            {
                // dacă ambele sunt libere, ocupăm un slot nou
                vaOcupaSlotNou = (slot.ActivitatePar == null && slot.ActivitateImpar == null);
                return true;
            }
            return false;
        }
        else
        {
            // saptamanal: slot complet gol
            if (slot.ActivitateSaptamanal == null
                && slot.ActivitatePar == null
                && slot.ActivitateImpar == null)
            {
                vaOcupaSlotNou = true;
                return true;
            }
            return false;
        }
    }

    private void ExecutaPlasare(SlotOrar slot, Activitate activitate, bool cuParitate)
    {
        if (cuParitate)
        {
            // alternăm pentru echilibru
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
                // dacă alternanța nu se potrivește (ex. Par ocupat, Impar liber), punem unde e liber
                if (slot.ActivitatePar == null) slot.ActivitatePar = activitate;
                else if (slot.ActivitateImpar == null) slot.ActivitateImpar = activitate;
            }
        }
        else
        {
            slot.ActivitateSaptamanal = activitate;
        }
    }

    // Verifică dacă slotul dat e liber
    private bool SlotLiber(SlotOrar slot)
    {
        return slot.ActivitateSaptamanal == null
            && slot.ActivitatePar == null
            && slot.ActivitateImpar == null;
    }

    // Verifică dacă, după plasare, ziua ar avea "gol izolat" (1 ocupat, 2 liber, 3 ocupat etc.)
    private bool CreeazaGolIzolat(string zi, int pereche)
    {
        var sloturiZi = Sloturi.Where(s => s.Ziua == zi).OrderBy(s => s.Perechea).ToList();

        // verificăm pentru poziția curentă dacă ar lăsa liber între două ocupate
        foreach (var (s, idx) in sloturiZi.Select((s, idx) => (s, idx)))
        {
            if (s.Perechea == pereche)
            {
                // verificăm vecinii
                var stangaOcupat = idx > 0 && !SlotLiber(sloturiZi[idx - 1]);
                var dreaptaOcupat = idx < sloturiZi.Count - 1 && !SlotLiber(sloturiZi[idx + 1]);

                // dacă ar fi liber și între doi ocupați => gol izolat
                return stangaOcupat && dreaptaOcupat;
            }
        }
        return false;
    }

    private void PlaseazaActivitate(Activitate activitate, bool cuParitate, ref int indexSlot)
    {
        // PASS 1: plasare fără goluri izolate și max 4/zi
        for (int i = indexSlot; i < Sloturi.Count; i++)
        {
            var slot = Sloturi[i];
            if (!PoatePlasaInSlot(slot, cuParitate, out bool vaOcupaNou))
                continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= PreferatMaxPerechiPeZi && !CreeazaGolIzolat(slot.Ziua, slot.Perechea))
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                indexSlot = i + 1;
                return;
            }
        }

        // PASS 2: acceptăm și gol izolat dar max 4/zi
        for (int i = indexSlot; i < Sloturi.Count; i++)
        {
            var slot = Sloturi[i];
            if (!PoatePlasaInSlot(slot, cuParitate, out bool vaOcupaNou))
                continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= PreferatMaxPerechiPeZi)
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                indexSlot = i + 1;
                return;
            }
        }

        // PASS 3: permitem al 5-lea slot, indiferent de goluri
        for (int i = 0; i < Sloturi.Count; i++)
        {
            var slot = Sloturi[i];
            if (!PoatePlasaInSlot(slot, cuParitate, out bool vaOcupaNou))
                continue;

            int ocupate = PerechiOcupateInZi(slot.Ziua);
            int dupaPlasare = ocupate + (vaOcupaNou ? 1 : 0);

            if (dupaPlasare <= AbsolutMaxPerechiPeZi)
            {
                ExecutaPlasare(slot, activitate, cuParitate);
                indexSlot = i + 1;
                return;
            }
        }

        Console.WriteLine($"⚠ Nu mai sunt sloturi libere pentru {activitate}");
    }


    public void Afiseaza()
    {
        Console.WriteLine($"Orar pentru grupa {Grupa}:\n");
        foreach (var slot in Sloturi)
        {
            Console.WriteLine(slot.ToString());
        }
    }
}
