using OrarUniver;
using Npgsql;

namespace OrarUniver;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMemoryCache();
        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder=>
            {
                builder.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
        builder.Services.AddScoped<IDb, Db>();
        builder.Services.AddScoped<NpgsqlConnection>(el => new NpgsqlConnection(builder.Configuration.GetConnectionString("LocalConnection")));
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        var app = builder.Build();

        app.UseCors("AllowAll");
        app.MapControllers();
        app.Run();
    }
}


