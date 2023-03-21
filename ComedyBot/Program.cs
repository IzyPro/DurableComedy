using ComedyBot;
using System.Text.Json;

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();

await TellAJoke(client);
await RaiseCompletedEvent(client, "DurableComedyInstanceID", "Job_Finished_Event");

static async Task TellAJoke(HttpClient client)
{
    var error = "I am unable to tell any jokes at the moment. How about you tell me one";
    try
    {
        var response = await ProcessHTTPRequestAsync(client, HttpMethod.Get, "https://v2.jokeapi.dev/joke/Any?blacklistFlags=nsfw,religious,political,racist,sexist,explicit");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(error);
            return;
        }

        var joke = await JsonSerializer.DeserializeAsync<JokeModel>(await response.Content.ReadAsStreamAsync()) ?? new();
        if (joke == null || joke.error)
        {
            Console.WriteLine(error);
            return;
        }

        if (joke.type == "twopart")
        {
            Console.WriteLine($"Q: {joke.setup}");
            await Task.Delay(3000);
            Console.WriteLine($"A: {joke.delivery}");
        }
        else
            Console.WriteLine($"Q: {joke.joke}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message}\n\n{error}");
    }
}

static async Task<HttpResponseMessage> ProcessHTTPRequestAsync(HttpClient client, HttpMethod method, string url)
{
    try
    {
        return await client.SendAsync(new HttpRequestMessage(method, url));
    }
    catch (Exception ex) 
    { 
        Console.WriteLine("Error: " + ex.Message);
        return new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.BadRequest
        };
    }
}


static async Task RaiseCompletedEvent(HttpClient client, string instanceId, string eventName)
{
    string baseURL = "https://durablecomedy.azurewebsites.net"; //"http://localhost:7116";
    var response = await ProcessHTTPRequestAsync(client, HttpMethod.Post, baseURL + $"/admin/extensions/DurableTaskExtension/instances/{instanceId}/raiseEvent/{eventName}?taskHub=TestHubName&connection=Storage&code=cI5f7vA1vszjjnQGLpCBEKCEBvx1nZ6VNDLGnViGZn8iAzFuf5WiRg==");
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"\nFailed to Raise Event {eventName}. Press Enter to try again or any other key to exit");
        if (Console.ReadKey().Key == ConsoleKey.Enter)
        {
            Console.WriteLine("Retrying...");
            await RaiseCompletedEvent(client, instanceId, eventName);
        }
        return;
    }
    return;
}