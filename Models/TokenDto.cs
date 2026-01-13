public class TokenDto
{
    public string GrantType { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty; // 추가
    public long AccessTokenExpiresIn { get; set; } 
} 