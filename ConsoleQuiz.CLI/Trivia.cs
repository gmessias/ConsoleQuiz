using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Spectre.Console;

namespace ConsoleQuiz.CLI;

static class Trivia
{
    private static readonly HttpClient client = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static async Task<List<Category>?> GetAllCategories(string filePath = "categories.json")
    {
        try
        {
            if (!File.Exists(filePath))
            {
                AnsiConsole.Markup($"File [maroon]not[/] found: {filePath}");
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var result = JsonSerializer.Deserialize<TriviaCategories>(json, _jsonOptions);

            return result?.Results ?? [];
        }
        catch (JsonException ex)
        {
            AnsiConsole.Markup($"[maroon]JSON parsing error[/]: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            AnsiConsole.Markup($"[maroon]Error reading file[/]: {ex.Message}");
            return null;
        }
    }

    public static async Task Begin(int categoryId)
    {
        try
        {
            var url = $"https://opentdb.com/api.php?amount=8&type=multiple&category={categoryId}&encode=url3986";
            var response = await client.GetFromJsonAsync<TriviaResponse>(url);
            if (response?.Results == null || response.Results.Count == 0)
            {
                AnsiConsole.Markup("[olive]No questions available[/]. Try again later.");
                return;
            }

            response.Results = response.Results.OrderBy(q => q.Difficulty switch
            {
                "easy" => 1,
                "medium" => 2,
                "hard" => 3,
                _ => 4
            }).ToList();

            foreach (var question in response.Results)
            {
                var decodedQuestion = new Question
                {
                    Category = WebUtility.UrlDecode(question.Category),
                    Type = WebUtility.UrlDecode(question.Type),
                    Difficulty = WebUtility.UrlDecode(question.Difficulty),
                    QuestionText = WebUtility.UrlDecode(question.QuestionText),
                    CorrectAnswer = WebUtility.UrlDecode(question.CorrectAnswer),
                    IncorrectAnswers = [.. question.IncorrectAnswers.Select(WebUtility.UrlDecode)]
                };

                var answers = new List<string>(decodedQuestion.IncorrectAnswers)
                {
                    decodedQuestion.CorrectAnswer
                }.OrderBy(a => Guid.NewGuid()).ToList();

                var difficultyColor = decodedQuestion.Difficulty.ToLower() switch
                {
                    "easy" => "green",
                    "medium" => "olive",
                    "hard" => "maroon",
                    _ => "grey"
                };

                AnsiConsole.MarkupLine($"[bold]Dificuldade:[/] [{difficultyColor}]{decodedQuestion.Difficulty}[/]");
                AnsiConsole.WriteLine();

                var panel = new Panel(decodedQuestion.QuestionText.EscapeMarkup())
                    .Header("[bold]Pergunta:[/]")
                    .BorderColor(Color.Grey);

                AnsiConsole.Write(panel);

                var selectedAnswer = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .PageSize(4)
                        .MoreChoicesText("[grey]Movimente para cima ou para baixo para exibir mais opções[/]")
                        .AddChoices(answers)
                        .UseConverter(text => text.EscapeMarkup())
                        .HighlightStyle(new Style(foreground: Color.Black, background: Color.White))
                );

                if (selectedAnswer == decodedQuestion.CorrectAnswer)
                {
                    AnsiConsole.MarkupLine("\n[green]✅ Correct![/]\n");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]❌ Errado![/] Resposta correta é: [green]{decodedQuestion.CorrectAnswer.EscapeMarkup()}[/]\n");
                    AnsiConsole.MarkupLine("[maroon]Você perdeu!![/]");
                    AnsiConsole.Markup("[grey]Aperte algum botão para encerrar...[/]");
                    Console.ReadKey();
                    return;
                }

                AnsiConsole.Markup("[grey]Aperte algum botão para continuar...[/]");
                Console.ReadKey();
            }
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.Markup($"[maroon]Network error[/]: {ex.Message}");
        }
        catch (JsonException ex)
        {
            AnsiConsole.Markup($"[maroon]JSON parsing error[/]: {ex.Message}");
        }
        catch (Exception ex)
        {
            AnsiConsole.Markup($"[maroon]Unexpected error[/]: {ex.Message}");
        }
    }
}
