using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

string key = args[0];

var app = builder.Build();

app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    /*
    if (!ctx.Request.IsHttps)
    {
        await ctx.Response.WriteAsync("request is not using https");
        return;
    }
    */
    if (!ctx.Request.Query.ContainsKey("key"))
    {
        await ctx.Response.WriteAsync("key not provided");
        return;
    }
    if (ctx.Request.Query["key"] != key)
    {
        await ctx.Response.WriteAsync("key invalid");
        return;
    }
    ctx.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = null;
    await next(ctx);
});

app.MapPost("/upload", async (HttpContext ctx, string dest) =>
{
    Console.WriteLine($"Got an upload request, dest: {dest}");
    using FileStream fo = File.Create(dest);
    using (GZipStream fg = new(ctx.Request.Body, CompressionMode.Decompress))
    {
        await fg.CopyToAsync(fo);
    }
    return "200";
});

app.MapGet("/delete", (string files) =>
{
    Console.WriteLine($"Got a delete request, files: {files}");
    string[] paths = files.Split(',');
    foreach (string path in paths)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Console.WriteLine($"Deleted file at {path}");
        }
        else
        {
            Console.WriteLine($"Tried to delete file at {path}");
        }
    }
    return "200";
});

app.MapGet("/execute", async (string script) =>
{
    Console.WriteLine($"Got an execute request, script: {script}");
    if (File.Exists(script))
    {
        Execute($"/root/perms.sh {script}");
        Process? process = Execute(script);
        await process!.WaitForExitAsync();
        Console.WriteLine($"Executed script at {script}");
    }
    else
    {
        Console.WriteLine($"Tried to execute script at {script}");
    }
    return "200";
});

static Process? Execute(string script)
{
    return Process.Start(new ProcessStartInfo()
    {
        UseShellExecute = false,
        FileName = "sh",
        Arguments = script,
    });
}

app.Run();