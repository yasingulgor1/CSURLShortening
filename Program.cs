using Microsoft.EntityFrameworkCore;
using System;
using URLShortening.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionStr = builder.Configuration.GetConnectionString(name: "DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connectionStr));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region short url endpoint
app.MapPost("/shorturl", async (UrlDto url, ApiDbContext db, HttpContext context) =>
{
    // Checking the url valid or not
    if (Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
    {
        return Results.BadRequest(error: "Invalid url has been provided");
    }

    // Creating much shorter url
    var random = new Random();
    const string chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890";
    var randomStr = new string(Enumerable.Repeat(chars, 6).Select(x => x[random.Next(x.Length)]).ToArray()); //Maximum character length for the hash portion of the URL is 6

    //Mapping the short and long url
    var sUrl = new UrlManagement()
    {
        Url = url.Url,
        ShortUrl = randomStr
    };

    //Save the database
    db.Urls.Add(sUrl);
    db.SaveChangesAsync();

    var result = $"{context.Request.Scheme}://{context.Request.Host}/{sUrl.ShortUrl}";

    return Results.Ok(new UrlShortResponseDto()
    {
        Url = result
    });
});
#endregion


#region redirect url endpoint
app.MapFallback(async (ApiDbContext db, HttpContext context) =>  //Redirect
{
    var path = context.Request.Path.ToUriComponent().Trim('/');
    var urlMatch = await db.Urls.FirstOrDefaultAsync(x => x.ShortUrl.ToLower().Trim() == path.Trim());

    if (urlMatch == null) return Results.BadRequest(error: "Invalid short url");

    return Results.Redirect(urlMatch.Url);

});
#endregion


#region custom url endpoint
app.MapPost("/customShorturl", async (CustomUrlDto dto, ApiDbContext db, HttpContext context) =>
{
    // Checking the url valid or not
    if (Uri.TryCreate(dto.Url, UriKind.Absolute, out var inputUrl))
    {
        return Results.BadRequest(error: "Invalid url has been provided");
    }

    if (string.IsNullOrEmpty(dto.CustomUrl)) return Results.BadRequest("Custom Url must be defined");
    if (dto.CustomUrl.Length <= 6) 
    {
        //Mapping the short and long url
        var sUrl = new UrlManagement()
        {
            Url = dto.Url,
            ShortUrl = dto.CustomUrl
        };

        //Save the database
        db.Urls.Add(sUrl);
        db.SaveChangesAsync();

        var result = $"{context.Request.Scheme}://{context.Request.Host}/{sUrl.ShortUrl}";

        return Results.Ok(new UrlShortResponseDto()
        {
            Url = result
        });
    }

    return Results.BadRequest("Custom Url must be max 6 chars");
});
#endregion

#region customUrl and randomUrl common endpoint
app.MapPost("/commonEndPoint", async (CustomUrlDto dto, ApiDbContext db, HttpContext context) => // customShortUrl or randomShortURL
{
    // Checking the url valid or not
    if (Uri.TryCreate(dto.Url, UriKind.Absolute, out var inputUrl))
    {
        return Results.BadRequest(error: "Invalid url has been provided");
    }

    var shortUrl = string.Empty;
    if (!string.IsNullOrEmpty(dto.CustomUrl) && dto.CustomUrl.Length <= 6)
    {
        shortUrl = dto.CustomUrl;
    }
    else
    { 
        // Creating much shorter url
        var random = new Random();
        const string chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890";
        shortUrl = new string(Enumerable.Repeat(chars, 6).Select(x => x[random.Next(x.Length)]).ToArray());
    }

    //Mapping the short and long url
    var sUrl = new UrlManagement()
    {
        Url = dto.Url,
        ShortUrl = shortUrl
    };

    //Save the database
    db.Urls.Add(sUrl);
    db.SaveChangesAsync();

    var result = $"{context.Request.Scheme}://{context.Request.Host}/{sUrl.ShortUrl}";

    return Results.Ok(new UrlShortResponseDto()
    {
        Url = result
    });
});
#endregion


app.Run();

class ApiDbContext : DbContext
{
    public virtual DbSet<UrlManagement> Urls { get; set; }

    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {

    }
}