public class SmtpConfig
{
    public string to { get; set; }
    public string from { get; set; }
    public string server { get; set; }
    public int port { get; set; }
    public bool ssl { get; set; }
    public bool defaultCredentials { get; set; }
    public string password { get; set; }
    public int intervaloMinutosEmail { get; set; }
}
