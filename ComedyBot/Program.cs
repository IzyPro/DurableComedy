using ComedyBot;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();

await TellAJoke(client);
await RaiseCompletedEvent(client, "testInstance", "Job_Finished_Event");

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
            Console.WriteLine($"Joke: {joke.joke}");

        await Task.Delay(10000);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message}\n\n{error}");
    }
}

static async Task<HttpResponseMessage> ProcessHTTPRequestAsync(HttpClient client, HttpMethod method, string url, [Optional] StringContent? content)
{
    try
    {
        var request = new HttpRequestMessage(method, url);
        if(content != null)
            request.Content = content;
        return await client.SendAsync(request);
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
    string baseURL = "https://durablecomedy.azurewebsites.net";
    var response = await ProcessHTTPRequestAsync(client, HttpMethod.Post, baseURL + $"/runtime/webhooks/durabletask/instances/{instanceId}/raiseEvent/{eventName}?taskHub=durablecomedy&connection=Storage&code=5XIu6WghnBJJ-H3Bms5T8ReToVZSBcv491_-teLJ09tBAzFunUAd0Q==", new StringContent(eventName, Encoding.UTF8, "application/json"));
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"\n\nStatus Code: {(int)response.StatusCode} {response.StatusCode}");
    Console.WriteLine($"Response Message: {response.RequestMessage}");
    Console.WriteLine($"Response Content: {content}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"\nFailed to Raise Event {eventName}. Press Enter to try again or any other key to exit");
        await Task.Delay(3000);
        Console.WriteLine("Retrying...");
        await RaiseCompletedEvent(client, instanceId, eventName);
        return;
    }
    return;
}