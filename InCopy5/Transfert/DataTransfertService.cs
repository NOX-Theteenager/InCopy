using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Specialized; // Pour StringCollection


public class DataTransferService
{
    private const int TransferPort = 6000; // Port pour l'envoi de données
    private TcpListener listener;
    private List<ActiveUser> activeUsers; // Liste des utilisateurs actifs du réseau
    public event EventHandler<ReceivedContentEventArgs> DataReceived; // Événement pour la réception de données
    private ClipboardWatcher clipboardWatcher = new();

    public DataTransferService(List<ActiveUser> users)
    {
        activeUsers = users;
        listener = new TcpListener(IPAddress.Any, TransferPort);
    }

    public void StartListening()
    {
        listener.Start();
        Task.Run(() => ListenForData());
    }

    private async Task ListenForData()
    {
        while (true)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleIncomingData(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la réception de données : {ex.Message}");
            }
        }
    }

    private async Task HandleIncomingData(TcpClient client)
    {
        // Définir le chemin du dossier où les fichiers seront enregistrés
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Transferts");

        // Créer le dossier si besoin
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new(stream, Encoding.UTF8))
        {
            string receivedData = await reader.ReadToEndAsync();
            ClipboardContentEventArgs content = JsonConvert.DeserializeObject<ClipboardContentEventArgs>(receivedData);

            // Obtenir le nom de l'utilisateur à partir de l'adresse IP
            string senderName = client.Client.RemoteEndPoint.ToString();

            Console.WriteLine($"Contenu reçu de {senderName}: Type - {content.ContentType}, Data - {content.Content}");

            // Déclencher l'événement DataReceived
            OnDataReceived(new ReceivedContentEventArgs
            {
                SenderName = senderName,
                Content = content.Content
            });

            // Si le contenu est du texte, mettre à jour le presse-papiers immédiatement
            if (content.ContentType == ContentType.Text)
            {
                clipboardWatcher.UpdateClipboard(content);
            }
            // Si le contenu est des fichiers, traiter les fichiers et mettre à jour le presse-papiers après traitement
            else if (content.ContentType == ContentType.Files)
            {
                var fileList = JsonConvert.DeserializeObject<List<string>>(content.Content);
                Console.WriteLine("filelist");
                Console.WriteLine(fileList);
                Console.WriteLine("");
                StringCollection updatedFilePaths = new StringCollection();

                foreach (var fileData in fileList)
                {
                    // Désérialiser chaque fichier en tant que FileInfo
                    FileInfo fileInfo = JsonConvert.DeserializeObject<FileInfo>(fileData);

                    Console.WriteLine("filedata");
                    Console.WriteLine(fileData);
                    Console.WriteLine("");

                    Console.WriteLine("fileinfo");
                    Console.WriteLine(fileInfo.FileContent);
                    Console.WriteLine("");

                    // Extraire les données JSON
                    string fileName = fileInfo.FileName;
                    string fileContent = fileInfo.FileContent;

                    // Construire le chemin du fichier dans le dossier de transfert
                    string filePath = Path.Combine(folderPath, fileName);

                    // Convertir le contenu du fichier de base64 en bytes
                    byte[] fileBytes = Convert.FromBase64String(fileContent);

                    // Écrire les bytes dans le fichier
                    File.WriteAllBytes(filePath, fileBytes);
                    Console.WriteLine($"Fichier reçu et enregistré sous : {filePath}");

                    // Ajouter le chemin du fichier au presse-papiers après l'avoir transféré
                    updatedFilePaths.Add(filePath);
                }

                // Mettre à jour le presse-papiers une seule fois avec la liste des fichiers après traitement
                if (updatedFilePaths.Count > 0)
                {
                    // Vérifier si nous sommes sur le thread UI avant d'appeler Clipboard.SetFileDropList
                    if (Application.OpenForms.Count > 0)
                    {
                        var mainForm = Application.OpenForms[0];
                        mainForm.Invoke(new Action(() =>
                        {
                            Clipboard.SetFileDropList(updatedFilePaths);
                            Console.WriteLine("Fichiers ajoutés au presse-papiers.");
                        }));
                    }
                }
            }
        }
    }








    public async Task SendDataToActiveUsers(ClipboardContentEventArgs content)
    {
        string serializedContent = JsonConvert.SerializeObject(content);

        foreach (var user in activeUsers)
        {
            try
            {
                await SendDataToUser(user, serializedContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi au {user.UserName} ({user.IPAddress}): {ex.Message}");
            }
        }
    }

    private async Task SendDataToUser(ActiveUser user, string data)
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(user.IPAddress, TransferPort);
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                await writer.WriteAsync(data);
            }
        }
    }

    protected virtual void OnDataReceived(ReceivedContentEventArgs e)
    {
        DataReceived?.Invoke(this, e); // Déclenchement de l'événement
    }
}

// Ajoutez une nouvelle classe pour les données reçues
public class ReceivedContentEventArgs : EventArgs
{
    public string SenderName { get; set; }
    public string Content { get; set; }
}
