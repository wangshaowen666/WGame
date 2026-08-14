using System.Security.Claims;
using LoginServer.Data;
using LoginServer.Models;
using LoginServer.Services;

namespace LoginServer.Endpoints;

/// <summary>认证相关路由</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // 注册（无需登录）
        app.MapPost("/register", async (RegisterReq req, PlayerRepository repo,
            PlayerProfileRepository profileRepo, PasswordService pwd) =>
        {
            var existing = await repo.GetByUsername(req.Username);
            if (existing != null)
                return Results.BadRequest("用户名已存在");

            var playerId = await repo.CreatePlayer(req.Username, pwd.Hash(req.Password));
            // 注册时自动创建默认养成数据
            await profileRepo.CreateDefault(playerId);

            Console.WriteLine($"新用户注册: {req.Username}");
            return Results.Ok("注册成功");
        });

        // 登录（无需登录）
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

        // 验证 token（需要登录，由认证中间件统一校验）
        // RequireAuthorization()：无有效 token 的请求会直接被中间件拦截返回 401
        app.MapGet("/me", async (HttpContext context, PlayerRepository repo) =>
        {
            // 能走到这里说明 token 已通过中间件校验，直接从 User 中取玩家信息
            var idStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (idStr == null)
                return Results.Json(new { error = "token 无效" }, statusCode: 401);

            var player = await repo.GetById(int.Parse(idStr));
            if (player == null)
                return Results.Json(new { error = "玩家不存在" }, statusCode: 404);

            return Results.Ok(new
            {
                playerId = player.Id,
                username = player.Username,
                createdAt = player.CreatedAt,
            });
        }).RequireAuthorization();
    }
}
