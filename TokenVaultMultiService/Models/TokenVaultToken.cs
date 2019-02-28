namespace TokenVaultMultiService.Models
{
    public class TokenVaultToken
    {
        public string name { get; set; }
        public string displayName { get; set; }
        public TokenVaultTokenParameterValues parameterValues { get; set; }
        public string tokenUri { get; set; }
        public string loginUri { get; set; }
        public TokenVaultTokenValue value { get; set; }
        public TokenVaultTokenStatus status { get; set; }
        public string location { get; set; }

        public bool IsStatusOk()
        {
            return this.status.state == "Ok";
        }
    }

    public class TokenVaultTokenParameterValues
    {
    }

    public class TokenVaultTokenValue
    {
        public string accessToken { get; set; }
    }

    public class TokenVaultTokenStatus
    {
        public string state { get; set; }
        public Error error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}