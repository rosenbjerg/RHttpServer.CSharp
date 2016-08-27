# RHttpServer.CSharp or rhs.cs

A C# alternative to nodejs and similar server bundles
Some of the use patterns has been inspired by nodejs and expressjs

Here is a simple example that shows how to use the server to handle request
In this example, the public folder is in the current working directory
And we listen on port 3000 and respond using 4 threads, without security on

```csharp
var server = new SimpleHttpServer("./public", 3000, 4);

server.Get("/", (req, res) =>
{
    res.SendString("Welcome");
});

server.Get("/file", (req, res) =>
{
    res.SendFile("./public/index.html");
});

server.Get("/:test", (req, res) =>
{
    var pars = server.CreateRenderParams();
    pars.Add("data1", req.Params["test"]);
    pars.Add("data2", 42);

    res.RenderPage("./public/index.ecs", pars);
});

server.Get("/*", (req, res) =>
{
    res.Redirect("/404");
});

server.InitializeDefaultPlugins();
server.Start(true);
```
