# RHttpServer.CSharp or rhs.cs

A C# alternative to nodejs and similar server bundles


Some of the use patterns has been inspired by nodejs and expressjs

Here is a simple example that shows how to use the server to handle request

In this example, we listen on port 3000 and respond using 4 threads, without security on. 

The public folder is placed in the current working directory

```csharp
var server = new SimpleHttpServer(3000, 4, "./public");

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

# Plug-ins
RHttpServer is created to be easy to build on top of. 
The server supports plug-ins, and offer a method to easily add new functionality.
The plugin system works by registering plug-ins before starting the server, so all plug-ins are ready when serving requests.
Some of the default functionality is implemented through plug-ins, and can easily customized or changed entirely.
The server comes with default handlers for json (Newtonsoft), page renderering (ecs) and basic security
You use a non-default

# The .ecs file format
The .ecs file format is merely an extension used for html pages with ecs-tags.
ecs-tags have the form <%TAG%>, so if i wanted a tag named 'foo' on my page, 
so that it could be replaced later, the tag would look like this: <%foo%>.

The file extension is enforced by the default page renderer to avoid confusion with regular html files.

The format is inspired by the ejs format.

The file extension is enforced by the default page renderer to avoid confusion with regular html files without tags.

The format is inspired by the ejs format, though you cannot embed JavaScript or C# for that matter, in the pages.
This was chosen because i did NOT like the idea behind it.

Send your dynamic content as RenderParams instead of embedding the code for generation of the content in the html. Please, separation of concerns.


# Why?
Because i like C#, the .NET framework and type-safety, but i also like the use-patterns of nodejs, with expressjs especially.
