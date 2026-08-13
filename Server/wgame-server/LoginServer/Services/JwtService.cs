using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LoginServer.Services;

/// <summary>JWT 令牌生成与校验</summary>
public class JwtService
{
    private readonly SymmetricSecurityKey _key;
    private readonly int _expireMinutes;

    public JwtService(IConfiguration config)
    {
        // 密钥来自配置 Jwt:Secret（appsettings.json），生产环境应放到环境变量
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("缺少配置 Jwt:Secret");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _expireMinutes = int.TryParse(config["Jwt:ExpireMinutes"], out var m) ? m : 60 * 24;
    }

    /// <summary>生成 token</summary>
    public string GenerateToken(int playerId, string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, playerId.ToString()),
            new Claim(ClaimTypes.Name, username),
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expireMinutes),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 校验 token，成功返回 (playerId, username)，失败返回 null
    /// </summary>
    public (int PlayerId, string Username)? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,      // 校验过期时间
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ClockSkew = TimeSpan.FromSeconds(30), // 允许 30 秒时钟偏差
            };

            var principal = handler.ValidateToken(token, validationParams, out _);

            var idStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            if (idStr == null)
                return null;

            return (int.Parse(idStr), username ?? "");
        }
        catch
        {
            // token 无效、过期、签名错误都会走到这里
            return null;
        }
    }
}
