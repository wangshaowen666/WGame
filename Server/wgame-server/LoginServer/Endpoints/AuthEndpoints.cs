using LoginServer.Data;
using LoginServer.Models;
using LoginServer.Services;

namespace LoginServer.Endpoints;

/// <summary>认证相关路由</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // 注册
        app.MapPost("/register", async (RegisterReq req, PlayerRepository repo, PasswordService pwd) =>
        {
            var existing = await repo.GetByUsername(req.Username);
            if (existing != null)
                return Results.BadRequest("用户名已存在");

            await repo.CreatePlayer(req.Username, pwd.Hash(req.Password));
            Console.WriteLine($"新用户注册: {req.Username}");
            return Results.Ok("注册成功");
        });

        // 登录
        app.MapPost("/login", async (RegisterReq req, PlayerRepository repo, PasswordService pwd, JwtService jwt) =>
        {
            var player = await repo.GetByUsername(req.Username);
            if (player == null || string.IsNullOrEmpty(player.PasswordHash) ||
                !pwd.Verify(req.Password, player.PasswordHash))
                return Results.Json(new { error = "用户名或密码错误" }, statusCode: 401);

            var token = jwt.GenerateToken(player.Id, player.Username ?? "");
            Console.WriteLine($"用户登录: {req.Username}");
            return Results.Ok(new
            {
                token,
                playerId = player.Id,
                username = player.Username,
            });
        });

        // 验证 token
        app.MapGet("/me", async (HttpRequest request, PlayerRepository repo, JwtService jwt) =>
        {
            var authHeader = request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer "))
                return Results.Json(new { error = "未携带 token" }, statusCode: 401);

            var token = authHeader["Bearer ".Length..];

            var identity = jwt.ValidateToken(token);
            if (identity == null)
                return Results.Json(new { error = "token 无效或已过期" }, statusCode: 401);

            var player = await repo.GetById(identity.Value.PlayerId);
            if (player == null)
                return Results.Json(new { error = "玩家不存在" }, statusCode: 404);

            return Results.Ok(new
            {
                playerId = player.Id,
                username = player.Username,
                createdAt = player.CreatedAt,
            });
        });
    }
}
