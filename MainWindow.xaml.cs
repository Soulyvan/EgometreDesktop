using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;

namespace egometre
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // On déclare les valeurs ego et exo centrique.
        string[] pronomsEgocentrique = new string[] { "m'", "j'" , "je", "ma", "me", "mes", "mien", "mienne",
                "miens", "miennes", "moi", "mon", "nous", "nos", "notre", "nôtre", "notres", "nôtres", "soi" };

        //  "l'" - à vérifier après s'il est réellement pronoms
        string[] pronomsExocentrique = new string[] { "t'", "tu", "te", "toi", "il", "elle", "lui",
                "vous", "ils", "ta", "ton", "sa", "son", "ses", "vos", "votre", "vôtre", "votres", "vôtres", "leur", "s'", "se" };

        string html, resultatEgo;
        public MainWindow()
        {
            // On met les pronoms par ordre croissant
            Array.Sort(pronomsEgocentrique);
            Array.Sort(pronomsExocentrique);

            InitializeComponent();
        }

        // L'évènement qui se déclanche à chaque fois que l'utilisateur modifie quelque chose dans le texte qu'il entre
        private void textEntre_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textEntre.Text.Trim() != "")
            {
                // On active le bouton téléchargement s'il n'y a rien dans le texte entré
                telecharger.IsEnabled = true;

                // à chaque fois que le texte est modifié, on retire les résultats.
                egocentrique.Text = "";
                exocentrique.Text = "";
                egometre.Text = "";

                string texteUtilisateur = textEntre.Text; // On met le contenu du texte, dans cette variable

                int sommeEgo = 0, sommeExo = 0; // Pour calculer le %total

                // On récupère la somme et on affiche le résultat
                sommeExo = AfficheResultatEgometre(exocentrique, texteUtilisateur, pronomsExocentrique);
                sommeEgo = AfficheResultatEgometre(egocentrique, texteUtilisateur, pronomsEgocentrique); 

                float pourcentageEgo = 0;
                float pourcentageExo = 0;
                if (sommeEgo != 0 || sommeExo != 0)
                {
                    pourcentageEgo = (float)(sommeEgo * 100) / (sommeEgo + sommeExo);
                    pourcentageExo = (float)(sommeExo * 100) / (sommeEgo + sommeExo);
                }

                // On affiche leur taux d'ego
                egocentrique.Text += "\nLe pourcentage d'égocentrisme : " + pourcentageEgo.ToString("0.00") + "%";
                exocentrique.Text += "\nLe pourcentage d'exocentrisme : " + pourcentageExo.ToString("0.00") + "%";

                // On affiche le résultat de l'égomètre
                if (pourcentageEgo > pourcentageExo)
                    egometre.Text = "Votre texte est égocentrique.";
                else if (pourcentageEgo < pourcentageExo)
                    egometre.Text = "Votre texte est exocentrique.";
                else
                    egometre.Text = "Votre texte est autant egocentrique qu'exocentrique";

                resultatEgo = egocentrique.Text + "\n\n" + exocentrique.Text + "\n\n\n" + egometre.Text;
            }
            else
            {
                // On rend inactif le bouton téléchargement s'il n'y a rien dans le texte entré
                telecharger.IsEnabled = false;

                // Vider le contenu
                webBrowser.Navigate((Uri)null);
                html = "";
                resultatEgo = "";

                egocentrique.Text = "";
                exocentrique.Text = "";
                egometre.Text = "";
            }
        }

        /// <summary>
        /// Cette méthode écrit le résultat des pronoms ainsi que leur ocurence,
        /// et la somme de toutes les ocurences.
        /// </summary>
        /// <param name="textBlock"></param>
        /// <param name="pronoms"></param>
        /// <returns></returns>
        private int AfficheResultatEgometre(TextBlock textBlock, string texte, string[] pronoms)
        {
            int somme = 0; // On retourne la somme
            int[] nbPronoms = new int[pronoms.Length]; // Pour le nombre d'ocurences de chaque mot

            // Encodage html pour la sécurité, pour prendre en compte l'affichage des caractères spéciaux
            string htmlContent = "<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'></head><body>";

            for (int i = 0; i < pronoms.Length; i++)
            {
                // Dans la regex suivante, on cherche combien de fois apparait le mot à la position i
                // Puis on le met dans IntEgo
                nbPronoms[i] = NbsMotsDansPhrase(texte, pronoms[i]);
                somme += nbPronoms[i];
                textBlock.Text += pronoms[i] + " : " + nbPronoms[i] + "\n";

                Regex regex = new Regex("\\b" + pronoms[i] + "\\b", RegexOptions.IgnoreCase);
                string replacement = "<span style='background-color: red;'>" + pronoms[i] + "</span>";
                texte = regex.Replace(texte, replacement);
            }
            html = texte + "\n\n" + resultatEgo;
            htmlContent += "<pre>" + texte + "</pre></body></html>";

            webBrowser.NavigateToString(htmlContent);

            return somme;
        }


        /// <summary>
        /// Cette fonction qui prend en paramètre un texte et un mot, 
        /// et compte le nombre d'ocurences de ce mot dans le texte.
        /// </summary>
        /// <param name="maPhrase"></param>
        /// <param name="mot"></param>
        /// <returns></returns>
        private int NbsMotsDansPhrase(string maPhrase, string mot)
        {
            Regex regex = new Regex(@"\b" + mot + @"\b", RegexOptions.IgnoreCase);

            return regex.Matches(maPhrase).Count;
        }

        private void importer_Click(object sender, RoutedEventArgs e)
        {
            // Options de la boîte de dialogue
            string[] options = { "TXT", "WORD", "PDF" };

            // Créer une boîte de dialogue personnalisée
            var dialog = new CustomDialog("Choisissez une option", options);

            // Afficher la boîte de dialogue et attendre la réponse
            var result = dialog.ShowDialog();

            // Vérifier la réponse de l'utilisateur
            if (result == true)
            {
                string selectedOption = dialog.SelectedOption;

                OpenFileDialog openFileDialog = new OpenFileDialog();

                // Traiter la sélection de l'utilisateur
                switch (selectedOption)
                {
                    case "TXT":
                        MessageBox.Show("importer au format TXT");
                        openFileDialog.Filter = "Fichiers texte (*.txt)|*.txt";

                        if (openFileDialog.ShowDialog() == true)
                        {
                            string selectedFilePath = openFileDialog.FileName;

                            // Utilisez le fichier sélectionné (selectedFilePath) comme vous le souhaitez
                            // par exemple, lisez son contenu
                            string content = File.ReadAllText(selectedFilePath);
                            textEntre.Text = content;
                        }
                        break;
                    case "WORD":
                        MessageBox.Show("importer au format WORD");
                        openFileDialog.Filter = "Fichiers Word (*.docx)|*.docx";

                        if (openFileDialog.ShowDialog() == true)
                        {
                            string selectedFilePath = openFileDialog.FileName;

                            // Ouvrir le fichier Word en utilisant Open XML SDK
                            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(selectedFilePath, false))
                            {
                                // Extraire le contenu texte du document Word
                                string documentText = string.Empty;
                                Body body = wordDocument.MainDocumentPart.Document.Body;

                                if (body != null)
                                {
                                    documentText = body.InnerText;
                                }

                                // Faites quelque chose avec le contenu texte extrait du fichier Word
                                textEntre.Text = documentText;
                            }
                        }
                        break;
                    case "PDF":
                        MessageBox.Show("importer au format PDF");
                        openFileDialog.Filter = "Fichiers PDF (*.pdf)|*.pdf";

                        if (openFileDialog.ShowDialog() == true)
                        {
                            string selectedFilePath = openFileDialog.FileName;

                            // Ouvrir le fichier PDF en utilisant iTextSharp
                            using (PdfReader pdfReader = new PdfReader(selectedFilePath))
                            {
                                // Extraire le contenu texte du document PDF
                                string documentText = string.Empty;

                                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                                {
                                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                                    string pageText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);
                                    documentText += pageText;
                                }

                                // Faites quelque chose avec le contenu texte extrait du fichier PDF
                                textEntre.Text = documentText;
                            }
                        }
                        break;
                }
                // Appeler directement la méthode textEntre_TextChanged pour exécuter le code spécifique
                textEntre_TextChanged(textEntre, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
            }
        }

        private void telecharger_Click(object sender, RoutedEventArgs e)
        {
            SaveToWord(html);
        }

        // Méthode pour enregistrer au format Word
        public void SaveToWord(string formattedText)
        {
            // Show a SaveFileDialog to let the user choose the file location.
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Word Document (*.docx)|*.docx";
            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                // On crée un nouveau WordprocessingDocument.
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(saveFileDialog.FileName, WordprocessingDocumentType.Document))
                {
                    // Add a new main document part.
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                    // Create the document structure.
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    // Split the formatted text into parts.
                    string[] parts = formattedText.Split(new string[] { "<span style='background-color: red;'>", "</span>" }, StringSplitOptions.None);

                    // Add the parts to the document.
                    Paragraph paragraph = body.AppendChild(new Paragraph());

                    // Add the parts to the document.
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i % 2 == 0)
                        {
                            // Add regular text.
                            Run run = paragraph.AppendChild(new Run());
                            AddTextWithLineBreaks(run, parts[i]);
                        }
                        else
                        {
                            // Add text with red background.
                            Run run = paragraph.AppendChild(new Run());
                            run.AppendChild(new Text(parts[i]));

                            // Set run properties for red background.
                            run.RunProperties = new RunProperties();
                            run.RunProperties.AppendChild(new Highlight() { Val = HighlightColorValues.Red });
                        }
                    }

                    // Set paragraph properties to display on the same line.
                    paragraph.ParagraphProperties = new ParagraphProperties();
                    paragraph.ParagraphProperties.AppendChild(new ParagraphStyleId() { Val = "NoSpacing" });

                    // on parcourt le paragraphe pour garder tous les espacements intactes
                    foreach (Run run in paragraph.Elements<Run>())
                    {
                        foreach (Text text in run.Elements<Text>())
                        {
                            text.Space = SpaceProcessingModeValues.Preserve;
                        }
                    }

                    // On ferme le document
                    wordDocument.Close();

                    MessageBox.Show("Le document a été enregistré avec succès", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("Le document n'a pas été enregistré car il est déjà ouvert." +
                    "\nVeuillez le fermer et recommencer !", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Ajouter un texte en prenant en compte les retours à la ligne
        private void AddTextWithLineBreaks(Run run, string text)
        {
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                run.AppendChild(new Text(lines[i]));
                if (i < lines.Length - 1)
                {
                    run.AppendChild(new Break());
                }
            }
        }
    }


    // Classe pour la boîte de dialogue personnalisée
    public class CustomDialog : Window
    {
        public string SelectedOption { get; private set; }

        public CustomDialog(string title, string[] options)
        {
            Title = title;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 300;
            Height = 200;

            var stackPanel = new StackPanel();

            foreach (var option in options)
            {
                var button = new Button
                {
                    Content = option,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(10),
                };

                button.Click += (sender, e) =>
                {
                    SelectedOption = option;
                    DialogResult = true;
                    Close();
                };

                stackPanel.Children.Add(button);
            }

            Content = stackPanel;
        }
    }
}
