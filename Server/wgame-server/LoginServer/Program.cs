using LoginServer.Data;
using LoginServer.Endpoints;
using LoginServer.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册服务（依赖注入）
builder.Services.AddSingleton<PlayerRepository>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtService>();

var app = builder.Build();

// 初始化数据库（建表）
app.Services.GetRequiredService<PlayerRepository>().InitDatabase();
Console.WriteLine("数据库初始化完成");

// 注册路由
app.MapAuthEndpoints();

app.Run();
