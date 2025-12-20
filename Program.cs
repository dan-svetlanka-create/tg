using TelegramBot_Dan;
using TelegramBot_Dan.Classes;

var builder = Host.CreateApplicationBuilder(args);

// Добавьте эту строку:
builder.Services.AddDbContext<DbConfig>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
