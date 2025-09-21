// See https://aka.ms/new-console-template for more information

HttpClient http = new HttpClient
{
    BaseAddress = new Uri("https://localhost:7084")
};

var result = await http.GetAsync("/");
var result2 = await http.GetAsync("/");

Console.WriteLine(await result.Content.ReadAsStringAsync());
Console.WriteLine(await result2.Content.ReadAsStringAsync());

Console.ReadLine();
