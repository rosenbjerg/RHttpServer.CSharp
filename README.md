# RHttpServer.CSharp or rhs.cs

A C# alternative to nodejs and similar server bundles

Some of the use patterns has been inspired by nodejs and expressjs

# Documentation
Documentation can be found [here](http://rosenbjerg.dk/rhs/docs)

### RHttpServerBuilder (rhsb)
I have created a simple tool that makes it easy to compile the server source files to an executable (sadly not C#6 yet).

The tool will automatically download all missing nuget dependencies, if a packages.config file is provided in the same folder as the source files

rhsb can also be used to start the server in the background, and later-on, stop it again

You can download the build tool installer here: [RHSB-Installer](http://rosenbjerg.dk/rhs/rhsb-installer/download)

The tool requires the [Mono runtime](http://www.mono-project.com/docs/getting-started/install/) to be installed if using Linux or Mac OSX


### Example
In this example, we listen locally on port 3000 and respond using 4 threads, without security on.

This example only handles GET http requests and the public folder is placed in the same folder as the server executable

```csharp
var server = new RHttpServer.HttpServer(3000, 4, "./public");

server.Get("/", (req, res) =>
{
    res.SendString("Welcome");
});

server.Get("/file", (req, res) =>
{
    res.SendFile("./public/index.html");
});

server.Get("/:name", (req, res) =>
{
    var pars = server.CreateRenderParams();
    pars.Add("data1", req.Params["name"]);
    pars.Add("foo", "bar");
    pars.Add("answer", 42);

    res.RenderPage("./public/index.ecs", pars);
});

server.Get("/*", (req, res) =>
{
    res.Redirect("/404");
});

server.Get("/404", (req, res) =>
{
    res.SendString("Nothing found", HttpStatusCode.NotFound);
});

server.InitializeDefaultPlugins();
server.Start(true);
```

## Plug-ins
RHttpServer is created to be easy to build on top of. 
The server supports plug-ins, and offer a method to easily add new functionality.
The plugin system works by registering plug-ins before starting the server, so all plug-ins are ready when serving requests.
Some of the default functionality is implemented through plug-ins, and can easily customized or changed entirely.
The server comes with default handlers for json (ServiceStack), page renderering (ecs) and basic security.
You can easily replace the default plugins with your own, just implement the interface of the default plugin you want to replace, and 
register it before initializing default plugins and/or starting the server.

## The .ecs file format
The .ecs file format is merely an extension used for html pages with ecs-tags.
ecs-tags have the form <%TAG%>, so if i wanted a tag named 'foo' on my page, 
so that it could be replaced later, the tag would look like this: <%foo%>.

You can also embed files containing page content, like a header, or a footer.

An ecs tag for a file have the following format: <¤PATH¤>.

The PATH should either be relative to the server executable, or the full path of the file.

PATH example using relative path: <¤./public/header.html¤>.


- The file extension is enforced by the default page renderer to avoid confusion with regular html files without tags.
- The format is inspired by the ejs format, though you cannot embed JavaScript or C# for that matter, in the pages.
This was chosen because i did NOT like the idea behind it.

Embed your dynamic content using RenderParams instead of embedding the code for generation of the content in the html. Please, separation of concerns.


## Why?
Because i like C#, the .NET framework and type-safety, but i also like the use-patterns of nodejs, with expressjs especially.
