using OrarUniver;

var discipline = new List<Disciplina>
{
    new Disciplina("Fundamentele programarii", 30, 15, 15),
    new Disciplina("Analiza matematica", 30, 30, 0),
    new Disciplina("Fizica", 15, 15, 15)
};

var orar = new OrarGrupa("I2301(ro)");
orar.GenereazaOrar(discipline);
orar.Afiseaza();
