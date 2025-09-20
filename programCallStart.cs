using OrarUniver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//namespace OrarNew
/*{
    var disciplineInformatica = new List<Disciplina>
    {
        new Disciplina("Fundamentele Programarii", 30, 30, 30,
                   esteComuna: true, grupeComune: new List<string> { "IA2301(ro)" }),

        new Disciplina("Sisteme de Operare", 30, 0, 30,
                   esteComuna: true, grupeComune: new List<string> { "IA2301(ro)" }),
        new Disciplina("HTML si CSS", 30, 0, 30),
        new Disciplina("Securitatea Cibernetica", 30, 0, 30),
        new Disciplina("Matematica", 30, 46, 0),
        new Disciplina("Limba Straina", 0, 74, 0),
        new Disciplina("Educatia Fizica", 0, 0, 30)
};

    var disciplineInformaticaAplicata = new List<Disciplina>
    {
        new Disciplina("Fundamentele Programarii", 30, 30, 30,
                   esteComuna: true, grupeComune: new List<string> { "I2301(ro)" }),

        new Disciplina("Sisteme de Operare", 30, 0, 30,
                   esteComuna: true, grupeComune: new List<string> { "I2301(ro)" }),
        new Disciplina("HTML si CSS", 30, 0, 30),
        new Disciplina("Securitatea Cibernetica", 30, 0, 30),
        new Disciplina("Matematica", 30, 46, 0),
        new Disciplina("Limba Straina", 0, 74, 0),
        new Disciplina("Educatia Fizica", 0, 0, 30)
};

    var orarInfo = new OrarGrupa("I2301(ro)", disciplineInformatica);
    var orarInfoA = new OrarGrupa("IA2301(ro)", disciplineInformaticaAplicata);

    var toateGrupele = new List<OrarGrupa> { orarInfo, orarInfoA };

    orarInfo.GenereazaOrar(disciplineInformatica, 15, toateGrupele);
    orarInfoA.GenereazaOrar(disciplineInformaticaAplicata, 15, toateGrupele);

    orarInfo.Afiseaza();
    orarInfoA.Afiseaza();

}*/


/*List<Disciplina> disciplinaInfromatica = new();*/

/*foreach (var line in planDCall.Where(m => m.IdPlan == 1))
{
    var disciplina = new Disciplina()
    {
        Denumire = line.LessonName,
        OreCurs = line.CursCant,
        OreSeminar = line.SeminarCant,
        OreLaborator = line.LaboratorCant,
        EsteComuna = line.Comun == "comun",
       // GrupeComune = line.Comun == "comun" ? new List<string> { "IA2301(ro)", "IM2301(ro)", "IF2301(ro)" } : null


    };
    disciplinaInfromatica.Add(disciplina);
}

List<Disciplina> disciplinaInfromaticaAplicata = new();

foreach (var line in planDCall.Where(m => m.IdPlan == 2))
{
    var disciplina = new Disciplina()
    {
        Denumire = line.LessonName,
        OreCurs = line.CursCant,
        OreSeminar = line.SeminarCant,
        OreLaborator = line.LaboratorCant,
        EsteComuna = line.Comun == "comun"

    };
    disciplinaInfromaticaAplicata.Add(disciplina);
}*/