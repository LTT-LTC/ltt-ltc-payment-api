using Microsoft.IdentityModel.Tokens;

namespace LTC.Shared.Hosting.Microservices.Authentication
{
    public class TokenAuthOption
    {
        public static string AuthenticationJwtBearer = "AuthenticationJwtBearer";
        public SymmetricSecurityKey SecurityKey { get; set; }

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public SigningCredentials SigningCredentials { get; set; }

        public int Expiration { get; set; } = 1;
        public int RefreshExpiration { get; set; } = 24 * 365;
    }
}
