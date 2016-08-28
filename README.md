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

# The .ecs file format
The .ecs file format is merely an extension used for html pages with ecs-tags.
ecs-tags have the form <%TAG%>, so if i wanted a tag named 'foo' on my page, 
so that it could be replaced later, the tag would look like this: <%foo%>.

The file extension is enforced by the default page renderer to avoid confusion with regular html files.

The format is inspired by the ejs format.

