using System;
using System.Collections.Generic;
using System.Threading;

public class ConsoleInterface
{
    private List<ActiveUser> activeUsers; // Liste des utilisateurs actifs
    private DataTransferService dataTransferService; // Référence au module de transfert

    public ConsoleInterface(List<ActiveUser> users, DataTransferService transferService)
    {
        activeUsers = users;
        dataTransferService = transferService;
    }

    public void StartInterface()
    {
        Console.WriteLine("Bienvenue dans le système de partage de contenu en réseau local !");
        Console.WriteLine("Commandes disponibles :");
        Console.WriteLine("  show - Afficher la liste des utilisateurs connectés");
        Console.WriteLine("  exit - Quitter le programme");

        Thread commandThread = new Thread(new ThreadStart(ListenForCommands));
        commandThread.Start();
    }

    private void ListenForCommands()
    {
        while (true)
        {
            string command = Console.ReadLine()?.ToLower();
            switch (command)
            {
                case "show":
                    ShowActiveUsers();
                    break;
                case "exit":
                    Console.WriteLine("Arrêt du programme...");
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Commande non reconnue. Essayez 'show' ou 'exit'.");
                    break;
            }
        }
    }

    private void ShowActiveUsers()
    {
        Console.WriteLine("Utilisateurs actifs :");
        if (activeUsers.Count == 0)
        {
            Console.WriteLine("  Aucun utilisateur actif.");
        }
        else
        {
            foreach (var user in activeUsers)
            {
                Console.WriteLine($"  - {user.UserName} ({user.IPAddress})");
            }
        }
    }

    public void NotifyUserConnected(ActiveUser user)
    {
        Console.WriteLine($"Nouvel utilisateur détecté : {user.UserName} ({user.IPAddress})");
    }

    public void NotifyUserDisconnected(ActiveUser user)
    {
        Console.WriteLine($"Utilisateur déconnecté : {user.UserName} ({user.IPAddress})");
    }

    public void NotifyContentReceived(string senderInfo, ClipboardContentEventArgs content)
    {
        Console.WriteLine($"Contenu reçu de {senderInfo} :");
        Console.WriteLine($"Type : {content.ContentType}");
        Console.WriteLine($"Données : {content.Content}");
    }
}
