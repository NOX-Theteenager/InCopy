public class ActiveUser
{
    public string UserName { get; }
    public string IPAddress { get; }

    public ActiveUser(string userName, string ipAddress)
    {
        UserName = userName;
        IPAddress = ipAddress;
    }
}
