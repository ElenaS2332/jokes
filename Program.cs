using System.Text;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string[] jokes = new[]
{
    "Why don't scientists trust atoms? Because they make up everything!",
    "Why did the developer go broke? Because he used up all his cache.",
    "Why do Java developers wear glasses? Because they don't C#.",
    "I told my computer I needed a break, and it said 'No problem, I’ll go to sleep.'",
    "Why was the function sad after a breakup? It lost its closure.",
    "Why did the programmer quit his job? Because he didn't get arrays.",
    "There are only 10 kinds of people in the world: those who understand binary and those who don’t.",
    "How many programmers does it take to change a light bulb? None, that's a hardware problem.",
    "What's a programmer's favorite hangout place? The Foo Bar.",
    "Why do programmers prefer dark mode? Because the light attracts bugs.",
    "A SQL query walks into a bar, walks up to two tables and asks: 'Can I join you?'",
    "Why did the frontend developer storm out of the restaurant? Because he didn’t like the menu layout.",
    "Why can’t you trust JavaScript developers? Because they let undefined things happen.",
    "What’s the object-oriented way to become wealthy? Inheritance.",
    "Why did the developer go to therapy? Too many unresolved issues.",
    "Why was the JavaScript file so sad? Because it didn’t know how to ‘null’ its feelings.",
    "Why are computers so smart? They listen to their motherboards.",
    "Knock knock. Who’s there? Recursion. Recursion who? Knock knock.",
    "Why did the software engineer cross the road? Because he read it in the requirements.",
    "Why do Python developers wear glasses? Because they can't C.",
    "Why don’t skeletons fight each other? They don’t have the guts.",
    "What do you call fake spaghetti? An impasta.",
    "Why did the scarecrow win an award? Because he was outstanding in his field.",
    "What did one wall say to the other wall? I’ll meet you at the corner.",
    "Why don’t eggs tell jokes? They’d crack each other up.",
    "I used to play piano by ear... But now I use my hands.",
    "What did the fish say when it hit the wall? Dam.",
    "What do you call a factory that makes okay products? A satisfactory.",
    "Why did the coffee file a police report? It got mugged.",
    "Why don’t some couples go to the gym? Because some relationships don’t work out."
};


app.MapGet("/", () =>
{
    var random = new Random();
    int index = random.Next(jokes.Length);
    return new { joke = jokes[index] };
});

app.MapGet("/ping", () => new { message = "pong", timestamp = DateTime.UtcNow });

app.MapGet("/export-jokes", async () =>
{
    var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var storageAccountName = builder.Configuration["BlobStorageAccountName"];
    var storageAccountKey = builder.Configuration["BlobStorageKey"];
    var containerName = builder.Configuration["BlobContainerName"];

    if (string.IsNullOrWhiteSpace(storageAccountName) || string.IsNullOrWhiteSpace(storageAccountKey))
        return Results.Problem("Missing blob storage credentials in configuration.");

    using var connection = new SqlConnection(sqlConnectionString);
    await connection.OpenAsync();

    var sql = "SELECT Content FROM Jokes ORDER BY CreatedAt DESC";
    var jokesList = await connection.QueryAsync<string>(sql);

    if (!jokesList.Any())
        return Results.NotFound(new { message = "No jokes found to export." });

    var sb = new StringBuilder();
    int count = 1;
    foreach (var joke in jokesList)
    {
        sb.AppendLine($"{count++}. {joke}");
    }
    var jokesText = sb.ToString();
    var fileName = $"jokes-export-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

    var blobUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
    var credential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);
    var blobServiceClient = new BlobServiceClient(blobUri, credential);
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    await containerClient.CreateIfNotExistsAsync();

    var blobClient = containerClient.GetBlobClient(fileName);
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jokesText));
    await blobClient.UploadAsync(stream, overwrite: true);

    var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(10));

    return Results.Ok(new { url = sasUri.ToString() });
});

app.Run();

record JokeDto(string Content);
record Joke(int Id, string Content, DateTime CreatedAt);