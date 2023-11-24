using TwitchLib.Api;
using TwitchLib.Api.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITwitchAPI>(x =>
{
    var configuration = x.GetService<IConfiguration>();
    return new TwitchAPI
    {
        Settings =
        {
            ClientId = configuration["Twitch_ClientId"],
            Secret = configuration["Twitch_ClientSecret"]
        }
    };
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
