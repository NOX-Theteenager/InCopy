using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class DiscoveryService
{
    private const int DiscoveryPort = 5000; // Port utilisé pour la découverte
    private readonly UdpClient udpClient;
    private readonly List<ActiveUser> activeUsers;
    private readonly string localUserName;

    public event EventHandler<ActiveUser> UserConnected;
    public event EventHandler<ActiveUser> UserDisconnected;

    public DiscoveryService(string userName)
    {
        localUserName = userName;
        activeUsers = new List<ActiveUser>();

        udpClient = new UdpClient
        {
            EnableBroadcast = true // Autoriser l'envoi en broadcast
        };
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort)); // Écouter sur tous les IPs pour le port
    }

    public void StartDiscovery()
    {
        // Lancer l'écoute des messages de découverte
        Task.Run(() => ListenForDiscoveryRequests());

        // Envoyer des requêtes de découverte à intervalle régulier
        Task.Run(() => SendDiscoveryRequests());
    }

    private void OnUserConnected(ActiveUser user)
    {
        UserConnected?.Invoke(this, user);
    }

    private void OnUserDisconnected(ActiveUser user)
    {
        UserDisconnected?.Invoke(this, user);
    }

    private void SendDiscoveryRequests()
    {
        while (true)
        {
            try
            {
                string discoveryMessage = "DISCOVERY_REQUEST";
                byte[] data = Encoding.UTF8.GetBytes(discoveryMessage);

                // Envoyer le message en broadcast
                udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                //Console.WriteLine("Broadcast discovery request sent");

                // Pause de 5 secondes avant de renvoyer une requête
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi de la découverte : {ex.Message}");
            }
        }
    }

    private void ListenForDiscoveryRequests()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, DiscoveryPort);

        while (true)
        {
            try
            {
                // Recevoir les données UDP
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);

                if (message == "DISCOVERY_REQUEST")
                {
                    // Répondre avec les informations de l'utilisateur
                    SendDiscoveryResponse(remoteEndPoint);
                }
                else if (message.StartsWith("DISCOVERY_RESPONSE"))
                {
                    // Ajouter l'utilisateur à la liste des utilisateurs actifs
                    string[] parts = message.Split(':');
                    if (parts.Length == 3)
                    {
                        string userName = parts[1];
                        string userIp = parts[2];

                        ActiveUser user = new ActiveUser(userName, userIp);
                        UpdateActiveUsers(user);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la réception de la découverte : {ex.Message}");
            }
        }
    }

    private void SendDiscoveryResponse(IPEndPoint remoteEndPoint)
    {
        string responseMessage = $"DISCOVERY_RESPONSE:{localUserName}:{GetLocalIPAddress()}";
        byte[] data = Encoding.UTF8.GetBytes(responseMessage);
        udpClient.Send(data, data.Length, remoteEndPoint);
    }

    private void UpdateActiveUsers(ActiveUser user)
    {
        lock (activeUsers)
        {
            if (!activeUsers.Exists(u => u.IPAddress == user.IPAddress))
            {
                activeUsers.Add(user);
                OnUserConnected(user);
            }
        }
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Aucune adresse IP réseau trouvée !");
    }
}
