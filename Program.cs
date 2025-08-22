using OrarUniver;
using System.Text.Encodings.Web;

var discipline = new List<Disciplina>
{
    /*new Disciplina("Fundamentele programarii", 30, 30, 30),
    new Disciplina("Sisteme de Operare", 30, 0, 45),
    new Disciplina("HtmlEncoder si CSS", 30, 0, 30),
    new Disciplina("Algoritmi si structuri de date", 30, 0, 30),
    new Disciplina("Matematica", 30, 45 , 0),
    new Disciplina("Limba Straina", 0, 60, 0),
    new Disciplina("Educatia Fizica", 0, 15, 0)*/
    new Disciplina("Programarea Orientata pe Obiect", 30, 0, 30),
    new Disciplina("JavaScrypt", 30, 0, 30),
    new Disciplina("Retele de Calculatoare", 30, 0, 46),
    new Disciplina("Python pentru aplicatii", 30, 0, 44),
    new Disciplina("Matematica Discreta", 44, 0, 46),
    new Disciplina("Etica Profesionala", 30, 30, 0)
};

var orar = new OrarGrupa("I2301(ro)");
orar.GenereazaOrar(discipline, 15);
orar.Afiseaza();
