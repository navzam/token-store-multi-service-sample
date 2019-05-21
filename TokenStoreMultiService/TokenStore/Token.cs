namespace TokenStoreMultiService.TokenStore
{
    public class Token
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string TokenUri { get; set; }
        public string LoginUri { get; set; }
        public TokenValue Value { get; set; }
        public TokenStatus Status { get; set; }
    }

    public class TokenValue
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class TokenStatus
    {
        public string State { get; set; }
        public TokenStatusError Error { get; set; }
    }

    public class TokenStatusError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}