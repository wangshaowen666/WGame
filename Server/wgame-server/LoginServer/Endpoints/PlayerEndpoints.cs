using System.Security.Claims;
using LoginServer.Data;
using LoginServer.Models;

namespace LoginServer.Endpoints;

/// <summary>养成数据相关路由</summary>
public static class PlayerEndpoints
{
    public static void MapPlayerEndpoints(this WebApplication app)
    {
        // 获取养成数据（需要登录）
        app.MapGet("/data", async (HttpContext context, PlayerProfileRepository repo) =>
        {
            var playerId = GetPlayerId(context);
            if (playerId == null)
                return Results.Unauthorized();

            var profile = await repo.GetByPlayerId(playerId.Value);
            if (profile == null)
                return Results.Json(new { error = "养成数据不存在" }, statusCode: 404);

            return Results.Ok(new
            {
                gold = profile.Gold,
                stageProgress = profile.StageProgress,
                towerLevels = profile.TowerLevels ?? "{}",
            });
        }).RequireAuthorization();

        // 保存养成数据（需要登录）
        app.MapPost("/data", async (HttpContext context, SaveProfileReq req, PlayerProfileRepository repo) =>
        {
            var playerId = GetPlayerId(context);
            if (playerId == null)
                return Results.Unauthorized();

            await repo.Update(playerId.Value, req.Gold, req.StageProgress, req.TowerLevels);
            return Results.Ok("保存成功");
        }).RequireAuthorization();
    }

    /// <summary>从已认证的 token 中提取玩家 Id</summary>
    private static int? GetPlayerId(HttpContext context)
    {
        var idStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return idStr != null && int.TryParse(idStr, out var id) ? id : null;
    }
}
