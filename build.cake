#tool nuget:?package=Wyam
#addin nuget:?package=Cake.Wyam


var target = Argument("target", "Default");
var outputDirectory = Directory("./output");

Task("Clean")
.Does(() => 
{
    CleanDirectory(outputDirectory);
});

Task("Build")
.Does(() => 
{
    Wyam(new WyamSettings
    {
        Recipe = "Blog",
        Theme = "CleanBlog",
        UpdatePackages = true
    });
});

Task("Preview")
.Does(() => 
{
            Wyam(new WyamSettings
        {
            Recipe = "Blog",
            Theme = "CleanBlog",
            UpdatePackages = true,
            Preview = true,
            Watch = true
        });        
});

Task("Deploy")
.Does(() => 
{
    string netlifyAccessToken = EnvironmentVariable("netlify_token");

    if (string.IsNullOrWhiteSpace(netlifyAccessToken))
    {
        throw new Exception("The given access token for netlify is invalid!");
    }

    // Upload via curl and zip to netlify
    Zip("./output", "output.zip", "./output/**/*");
    StartProcess("curl", "--header \"Content-Type: application/zip\" --header \"Authorization: Bearer" + netlifyAccessToken + "\" --data-binary \"@output.zip\" --url https://api.netlify.com/api/v1/sites/gstoob-online.netlify.com/deploys");
});


Task("Default")
.IsDependentOn("Clean")
.IsDependentOn("Preview");


Task("AppVeyor")
.IsDependentOn("Clean")
.IsDependentOn("Build")
.IsDependentOn("Deploy");


RunTarget(target);