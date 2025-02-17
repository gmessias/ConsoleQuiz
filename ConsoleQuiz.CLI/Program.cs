using ConsoleQuiz.CLI;
using Spectre.Console;

Welcome();

var categories = await Trivia.GetAllCategories();
if (categories is null)
{
    AnsiConsole.Markup("[maroon]Desculpe, tivemos problema para buscar as categorias[/]");
    return;
}

string[] categoryArray = categories.Select(c => c.Name).ToArray();
var categorySelect = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Escolha uma [navy]categoria[/]:")
        .PageSize(10)
        .MoreChoicesText("[grey]Movimente para cima ou para baixo para exibir mais categorias[/]")
        .AddChoices(categoryArray));

AnsiConsole.Write(new Rule($"[olive]{categorySelect}[/]").Justify(Justify.Left));

var category = categories.Find(c => c.Name.Equals(categorySelect));

if (category is null)
{
    AnsiConsole.Markup("[maroon]Desculpe, tivemos problema para começar o Quiz[/]");
    return;
}

await Trivia.Begin(category.Id);

static void Welcome()
{
    AnsiConsole.MarkupLine("[navy]Bem vindo ao Quiz[/]!!");
    Console.WriteLine();
    AnsiConsole.MarkupLine("[olive]Serão 8 perguntas com dificuldade crescente[/]");
    AnsiConsole.MarkupLine("[navy]Prove seu conhecimento e tente acertar todas!![/]");
    Console.WriteLine();
}
