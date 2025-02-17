using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications; // Windows Community Toolkit pour les notifications
using Newtonsoft.Json; // JSON.NET pour la sérialisation
using System.Collections.Specialized;

public class ClipboardWatcher
{
    public event EventHandler<ClipboardContentEventArgs> ClipboardChanged;

    private bool isUpdatingClipboard;
    private string lastContent; // Nouveau : Stocke le dernier contenu capturé pour éviter les doublons

    public ClipboardWatcher()
    {
        ClipboardNotification.ClipboardUpdate += OnClipboardUpdate;
    }

    private void OnClipboardUpdate(object sender, EventArgs e)
    {
        if (isUpdatingClipboard) return;

        Console.WriteLine("Clipboard update detected.");

        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();

                    // Vérifie si le contenu est le même que le précédent
                    if (text == lastContent) return;

                    lastContent = text; // Met à jour le dernier contenu capturé
                    ClipboardChanged?.Invoke(this, new ClipboardContentEventArgs(ContentType.Text, text));
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    List<string> filePaths = new List<string>();

                    foreach (string file in files)
                    {
                        // Convertir le contenu du fichier en base64
                        byte[] fileData = File.ReadAllBytes(file);
                        string base64FileData = Convert.ToBase64String(fileData);

                        // Ajoute le nom du fichier et le contenu encodé en base64
                        filePaths.Add(JsonConvert.SerializeObject(new { FileName = Path.GetFileName(file), FileContent = base64FileData }));
                    }

                    string serializedFiles = JsonConvert.SerializeObject(filePaths);

                    if (serializedFiles == lastContent) return;

                    lastContent = serializedFiles;
                    ClipboardChanged?.Invoke(this, new ClipboardContentEventArgs(ContentType.Files, serializedFiles));
                }


                break;
            }
            catch (System.Runtime.InteropServices.ExternalException ex)
            {
                retryCount++;
                Console.WriteLine($"Erreur d'accès au presse-papiers, tentative {retryCount} : {ex.Message}");
                System.Threading.Thread.Sleep(100);
            }
        }
    }

    public void UpdateClipboard(ClipboardContentEventArgs content)
    {
        isUpdatingClipboard = true;

        if (Application.OpenForms.Count > 0)
        {
            var mainForm = Application.OpenForms[0];
            mainForm.Invoke(new Action(() =>
            {
                try
                {
                    if (content.ContentType == ContentType.Text)
                    {
                        Clipboard.SetText(content.Content);
                        Console.WriteLine("Texte mis dans le presse-papiers.");
                    }
                    else if (content.ContentType == ContentType.Files)
                    {
                        // Désérialisation en une liste d'objets avec FileName et FileContent
                        var files = JsonConvert.DeserializeObject<List<dynamic>>(content.Content);

                        if (files != null && files.Count > 0)
                        {
                            StringCollection validFilePaths = new StringCollection();
                            string tempFolderPath = Path.Combine(Path.GetTempPath(), "ClipboardFiles");

                            // Créer le dossier temporaire si besoin
                            if (!Directory.Exists(tempFolderPath))
                            {
                                Directory.CreateDirectory(tempFolderPath);
                            }

                            foreach (var fileInfo in files)
                            {
                                string fileName = fileInfo.FileName;
                                string fileContent = fileInfo.FileContent;

                                if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(fileContent))
                                {
                                    // Construire le chemin du fichier
                                    string filePath = Path.Combine(tempFolderPath, fileName);

                                    // Convertir le contenu de base64 en bytes
                                    byte[] fileBytes = Convert.FromBase64String(fileContent);

                                    // Enregistrer le fichier
                                    File.WriteAllBytes(filePath, fileBytes);
                                    validFilePaths.Add(filePath);

                                    Console.WriteLine($"Fichier temporaire créé : {filePath}");
                                }
                            }

                            if (validFilePaths.Count > 0)
                            {
                                Clipboard.SetFileDropList(validFilePaths);
                                Console.WriteLine("Fichiers mis dans le presse-papiers." + validFilePaths);
                            }
                            else
                            {
                                Console.WriteLine("Aucun fichier valide à ajouter au presse-papiers.");
                            }
                        }
                    }

                    // Met à jour le dernier contenu après modification
                    lastContent = content.Content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la mise à jour du presse-papiers: {ex.Message}");
                }
                finally
                {
                    isUpdatingClipboard = false;
                }
            }));
        }
    }


}


public enum ContentType
{
    Text,
    Files
}

public class ClipboardContentEventArgs : EventArgs
{
    public ContentType ContentType { get; }
    public string Content { get; set; }

    public ClipboardContentEventArgs(ContentType contentType, string content)
    {
        ContentType = contentType;
        Content = content;
    }
}


public static class ThreadHelper
{
    public static void RunOnSTAThread(Action action)
    {
        var thread = new Thread(() =>
        {
            action();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(); // Attendez que l'opération se termine
    }
}


public class FileInfo
{
    public string FileName { get; set; }
    public string FileContent { get; set; } // Le contenu du fichier en base64
}


