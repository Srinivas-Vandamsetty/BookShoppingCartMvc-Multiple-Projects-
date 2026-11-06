using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace BookShoppingCart.WebAPI.Security;
public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options) : IAccessTokenIssuer
{
    public IssuedAccessToken Issue(string userId, string name, IEnumerable<string> roles, Guid sessionId, DateTime now)
    {
        var settings=options.Value;
        var expires=now.AddMinutes(settings.AccessTokenMinutes);
        var claims=new List<Claim>
        {
            new("sub",userId), new("name",name), new("sid",sessionId.ToString("N")),
            new("jti",Guid.NewGuid().ToString("N")),
            new("iat",new DateTimeOffset(now).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(r=>new Claim("role",r)));
        var jwt=new JwtSecurityToken(settings.Issuer,settings.Audience,claims,now,expires,
            new SigningCredentials(JwtOptions.Key(settings.SigningKey),SecurityAlgorithms.HmacSha256));
        jwt.Header["typ"]="at+jwt";
        return new(new JwtSecurityTokenHandler().WriteToken(jwt),expires);
    }
}
