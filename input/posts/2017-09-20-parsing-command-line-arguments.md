Title: Parsing Command Line Arguments With the Microsoft.Extensions.CommandLineUtils Package
Tags:
  - Command Line Application
  - .NET Core
  - Argument Parsing
  - CSharp
---
A few months ago, I was asked to write a small .NET Core console application, which should be able to do some CRUD operations against a [NoSQL](https://en.wikipedia.org/wiki/NoSQL) database. These operations should be controllable through different command line arguments.
However parsing command line args by hand is a painful thing and can cost a lot of time until it works. In this blog post, I'd like to show you an easy and quick way how you can parse command line args by using the [Microsoft.Extensions.CommandLineUtils](https://www.nuget.org/packages/Microsoft.Extensions.CommandLineUtils/) package.

We are going to write a small .NET core command line application, which makes some simple GET requests against the free [Star Wars API](https://swapi.co/) and displays its data to the console. For the sake of simplicity, we will just display information of different characters from the movies and will skip other information like planets, vehicles and so on.
This is how we want to use the app:
- We want to retrieve information about a specific character by passing an id (i.e. "StarWars.exe characters -i="1")
- We want to search for a character in case we do not know the unique id to access the character information by passing a search term (i.e. "StarWars.exe characters -s="luke")
- If we do not pass any parameter, then a help should be displayed containing the most important information on how to use the app. The help should also be adressable if we pass the flags "-? |-h |--help".

OK, now I think it's time to implement the whole thing.

First, we should add the required NuGet packages. Since we want to write a command line application, and also want to retrieve data from a Rest Web API, w need the Microsoft.Extensions.CommandLineUtils package and the NewtonSoft.Json package. So in your csproj file, add the following to download them:

```
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.CommandLineUtils" Version="1.1.1" />
    <PackageReference Include="Newtonsoft.Json" Version="10.0.2" />
  </ItemGroup>
  ```

  Now we have to add a POCO, which later contains the deserialized character information:

  ```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Samples.CommandLineUtils
{
    public class StarWarsCharacter
    {
        public string Name { get; set; }
        public string Height { get; set; }
        public string Mass { get; set; }

        [JsonProperty("hair_color")]
        public string HairColor { get; set; }

        [JsonProperty("skin_color")]
        public string SkinColor { get; set; }

        [JsonProperty("eye_color")]
        public string EyeColor { get; set; }

        [JsonProperty("birth_year")]
        public string BirthYear { get; set; }
        public string Gender { get; set; }
        public string Homeworld { get; set; }
        public List<string> Films { get; set; }
        public List<string> Species { get; set; }
        public List<string> Vehicles { get; set; }
        public List<string> Starships { get; set; }
        public string Created { get; set; }
        public string Edited { get; set; }
        public string Url { get; set; }
    }
}
```

Everything should be clear so far, so I won't explain much here. The only thing you may have noticed are the [JsonProperty()] attributes, I've added them just to use my naming conventions, It would also work very well without them, but I don't like underscores in my property names. :-)

OK, now we're comming to the interesting part, the program.cs which contains the whole logic of the application:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Samples.CommandLineUtils
{
    static class Program
    {
        const string GetByIdFlag = "-i |--id";
        const string SearchForCharacterFlag = "-s |--search";
        const string HelpFlag = "-? |-h |--help";
        const string ApiBaseUrl = "https://swapi.co/api";

        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception e)
            {
                var message = e.GetBaseException().Message;
                Console.Error.WriteLine(message);
                return 0xbad;
            }
        }
```

First, some constants are defined, which hold our option names. With the | sign, we're able to split between long and short terms. This means, that we can call our application with short and long option names (i.e. StarWars.exe characters -i="1" (short term), StarWars.exe characters --id="1" (long term))
There's also another constant defined, which holds the value of the base URL of the Star Wars web API.
In our Main method, we just simply added a try / catch block which calls a private method named Run(string[] args).
Of course the workflow can also be directly written in to the Main method itself, but I don't like to blow up the Main and thus I moved it to a private static method.

Now let's implement the Run() method:

```csharp
private static int Run(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            app.HelpOption(HelpFlag);

            app.Command("characters", c =>
            {
                c.Description = "With this command, you can retrieve information about characters from Star Wars using the Star Wars Web API.";

                                var GetByIdOption = c.Option(GetByIdFlag, "Get a specific character by ID.", CommandOptionType.SingleValue);
                var searchForCharacterOption = c.Option(SearchForCharacterFlag, "Search a character in the star wars API. Use this option if you don't know the unique ID of a specific character.", CommandOptionType.SingleValue);

                // more code
                return 0;
            });
```

In the Run method, we created an instance of the CommandLineApplication class. This class is responsible for parsing our command line args.

__Note__: the throwOnUnexpectedArg: false parameter, that we set to false, just means, that we don't want to let the whole application crash when someone passes an argument which does not exist. We will display the help instead.

With the call of app.HelpOption(HelpOptionFlag) the help option flags have been added to our CommandLineApplication object.
After that, we added a "Command" object.

Now it's time for some short theory:

The CommandLineUtils package differetiates between the following available constructs:
- Arguments
- Options
- Commands

An argument is simply a string that can be passed as parameter to an application (e.g. program.exe "1").

```csharp
public CommandArgument Argument(string name, string description, bool multipleValues = false);
```

You can use the Argument.Value property to access the value of an argument:

```csharp
var argument = app.Argument("arg name", "arg description");

app.OnExecute(() =>
{
    if (!string.IsNullOrWhiteSpace(argument.Value))
    {
        Console.WriteLine($"Argument value: {argument.Value}");
    }
    return 0;
});
```

An option is very similar to an argument, just with the difference that it has a parameter name which has to be passed as well (i.e. program.exe --id="1"). An option can also be used to create switch parameters (i.e. program.exe --enable-debug).

```csharp
public CommandOption Option(string template, string description, CommandOptionType optionType);
```

You can use the Option.HasValue() and Option.Value() methods to validate and access the option values:

```csharp
var option1 = app.Option("-o|--option", "option description", CommandOptionType.SingleValue); // option which accepts a single value
var option2 = app.Option("--enable-option2", "option description", CommandOptionValue.NoValue); // a switch parameter

app.OnExecute(() =>
{
    if (option1.HasValue())
    {
        Console.WriteLine($"value of option 1: {option1.Value()}");
    }
    if (option2.HasValue())
    {
        Console.WriteLine("The switch: option2 is set.");
    }
    return 0;
});
```

A command is actually anything else than another instance of the CommandLineApplication class which can contain Options, Arguments and more commands. You can use commands to have a kind of grouping.

```csharp
public CommandLineApplication Command(string name, Action<CommandLineApplication> configuration, bool throwOnUnexpectedArg = true);
```

In our app, we've created a command named "characters" and provided a short description of that command (this is used in the help text when calling the app without arguments or with the help flags).
After that, we added our two required options to retrieve the Star Wars API information. To prevent users from passing as many values as they want, we set the command type option to single value.

Now let's implement the rest of this method including the API calls:

```csharp
                c.OnExecute(async () =>
                {
                    if (GetByIdOption.HasValue())
                    {
                        var character = await GetStarWarsCharacterById(GetByIdOption.Value());
                        DisplayStarWarsCharacter(character);
                        return 0;
                    }
                    else if (searchForCharacterOption.HasValue())
                    {
                        var characters = await SearchStarWarsCharacter(searchForCharacterOption.Value());

                        foreach (var character in characters)
                        {
                            DisplayStarWarsCharacter(character);
                            Console.WriteLine();
                        }
                        return 0;
                    }
                    else
                    {
                        c.ShowHelp();
                        return 0;
                    }
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });
            return app.Execute(args);
        }

        private static async Task<StarWarsCharacter> GetStarWarsCharacterById(string characterId)
        {
            using (var client = new HttpClient())
            {
                var request = await client.GetAsync($"{ApiBaseUrl}/people/{characterId}");

                request.EnsureSuccessStatusCode();

                var result = await request.Content.ReadAsStringAsync();

                var deserializedCharacter = JsonConvert.DeserializeObject<StarWarsCharacter>(result);

                if (deserializedCharacter == null) throw new Exception("An error occured while deserializing the result received from the Star Wars API!");

                return deserializedCharacter;
            }
        }

        private static async Task<IEnumerable<StarWarsCharacter>> SearchStarWarsCharacter(string searchTerm)
        {
            using (var client = new HttpClient())
            {
                var request = await client.GetAsync($"{ApiBaseUrl}/people/?search={searchTerm}");

                request.EnsureSuccessStatusCode();

                var result = await request.Content.ReadAsStringAsync();

                // We are just interested in the search results, so we have to extract the specific node ('results') from the rest of this json string.
                var jObject = JObject.Parse(result);
                var jToken = jObject.GetValue("results");
                var characters = (List<StarWarsCharacter>)jToken.ToObject(typeof(List<StarWarsCharacter>));

                if (!characters.Any()) throw new Exception("An error occured while deserializing the result received from the Star Wars API!");

                return characters;
            }
        }

        private static void DisplayStarWarsCharacter(StarWarsCharacter character)
        {
            Console.WriteLine($"{nameof(character.Name)}: {character.Name}");
            Console.WriteLine($"Birth year: {character.BirthYear}");
            Console.WriteLine($"{nameof(character.Height)}: {character.Height}");
            Console.WriteLine($"Eye color: {character.EyeColor}");
        }
    }
}
```

The most important part here is the OnExecute() method. It accepts a ```Func<int>``` delegate (or a ```Func<Task<int>>``` delegate if we have to use asynchronous operations like in our example). This method contains the whole logic of a particular command and is called as soon as the Execute() method with the right parameters is called, which  we do at the end of the Run() method:

```csharp
return app.Execute(args);
```

The logic of the OnExecute() method should actually be quite self explanatory, we check if one of our options has a value. If this is the case, we call the corresponding private method which processes the required API call. After we got back the result, we display the result to the console.
If none of our options has a value, then we display the help to the user.
If the application is called without any parameters, the help will be displayed as well.
Note that we do not have to write any custom code for displaying the help - the CommandUtils package will handle this for us using the descriptions we've added to the command and options objects.

So let's test our application:

```
dotnet run
dotnet run characters
dotnet run characters -i="1"
dotnet run characters --search="anak"
```

To summarize, with the Microsoft Extensions CommandUtilsPackage, you have a small and powerfull parser for command line arguments. Wether you want to parse simple strings (Arguments), named arguments or switches (Options), the package provides possibilities for both argument types!

You can find the source code of this sample application on [GitHub](https://github.com/GStoob/commandline-utils-sample)
