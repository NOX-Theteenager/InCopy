using System;
using System.Collections.Generic;

public class Program
{
    private static List<ActiveUser> activeUsers;
    private static DiscoveryService discoveryService;
    private static ClipboardWatcher clipboardWatcher;
    private static DataTransferService dataTransferService;
    private static ConsoleInterface consoleInterface;

    [STAThread]
    public static void Main(string[] args)
    {
        // Nom d'utilisateur pour l'instance locale
        string username = Environment.MachineName; // Utilise le nom de la machine par défaut, peut être personnalisé

        // Initialisation des composants
        activeUsers = new List<ActiveUser>();

        // Initialiser les services principaux
        discoveryService = new DiscoveryService(username); // Passe le nom d'utilisateur local
        clipboardWatcher = new ClipboardWatcher();
        dataTransferService = new DataTransferService(activeUsers);
        consoleInterface = new ConsoleInterface(activeUsers, dataTransferService);

        // Abonnement aux événements
        SubscribeToEvents();

        // Démarrer les services
        discoveryService.StartDiscovery();
        dataTransferService.StartListening();
        //clipboardWatcher.StartWatching();  //pas besoin de ca etant donne que le startwatching se fait en fait a l'intialisation de l'objet
        consoleInterface.StartInterface();

        Console.WriteLine("Système de partage de contenu en réseau local démarré.");

        // Lance l'application avec la fenêtre de notification du presse-papiers
        Application.EnableVisualStyles();
        Application.Run(ClipboardNotification.form);
    }

    private static void SubscribeToEvents()
    {
        // Abonnement aux événements de découverte
        discoveryService.UserConnected += (sender, user) =>
        {
            activeUsers.Add(user);
            consoleInterface.NotifyUserConnected(user);
        };

        discoveryService.UserDisconnected += (sender, user) =>
        {
            activeUsers.Remove(user);
            consoleInterface.NotifyUserDisconnected(user);
        };

        // Abonnement aux événements du presse-papiers
        clipboardWatcher.ClipboardChanged += async (sender, content) =>
        {
            Console.WriteLine($"Contenu capturé : {content.Content}");
            await dataTransferService.SendDataToActiveUsers(content);
        };

        // Abonnement aux événements de réception de données
        dataTransferService.DataReceived += (sender, receivedContent) =>
        {
            string senderInfo = receivedContent.SenderName;

            // Vérifier dynamiquement si le contenu reçu est du texte ou des fichiers
            ClipboardContentEventArgs clipboardContent;

            if (receivedContent.Content.StartsWith("[") && receivedContent.Content.EndsWith("]"))
            {
                // Si le contenu est probablement une liste de fichiers (vérification basique d'un tableau JSON)
                clipboardContent = new ClipboardContentEventArgs(ContentType.Files, receivedContent.Content);
            }
            else
            {
                // Sinon, considérez que c'est du texte
                clipboardContent = new ClipboardContentEventArgs(ContentType.Text, receivedContent.Content);
            }

            consoleInterface.NotifyContentReceived(senderInfo, clipboardContent);
        };



    }
}
