namespace LoginServer.Models;

/// <summary>注册/登录请求</summary>
public record RegisterReq(string Username, string Password);

/// <summary>玩家数据（对应 Player 表）</summary>
public class PlayerData
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? CreatedAt { get; set; }
}
