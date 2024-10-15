using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiEpubTTSReader.Models;
using System.Diagnostics;
using HtmlAgilityPack;
using VersOne.Epub;
using System.Text;

namespace MauiEpubTTSReader
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isTextSearchVisible;
        [ObservableProperty]
        private bool _isMultipleTextSelectionVisible;
        [ObservableProperty]
        private bool _isTextReadingVisible;

        [ObservableProperty]
        private string _epubStateDisplayMessage = "No file selected";

        [ObservableProperty]
        private string _textToFind = string.Empty;

        private List<TextLocation> _locationsOfFoundText = [];
        public List<TextLocation> LocationsOfFoundText
        {
            get => _locationsOfFoundText;
            set
            {
                SetProperty(ref _locationsOfFoundText, value);
                //if (value.Count > 1)
                //    //IsMultipleTextSelectionVisible = true;
                //else
                //{
                //    //IsMultipleTextSelectionVisible = false;
                //    SelectedFoundTextLocation = value.First();
                //}
            }
        }

        [ObservableProperty]
        private TextLocation? _selectedFoundTextLocation;

        private string _completeBookText = string.Empty;

        public MainPageViewModel()
        {
            // TODO Hook it up with real found locations of the text the user entered
            LocationsOfFoundText =
            [
                new TextLocation { Index = 10, Text = "This is the first text" },
                new TextLocation { Index = 50, Text = "This is the second text" },
                new TextLocation { Index = 150, Text = "This is the third text" },
            ];
        }

        [RelayCommand]
        private async Task BrowseEpubFile()
        {
            try
            {
                // Define the allowed file types for .epub files
                var epubFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "org.idpf.epub-container" } },
                    { DevicePlatform.Android, new[] { "application/epub+zip" } },
                    { DevicePlatform.WinUI, new[] { ".epub" } }
                });

                // Define PickOptions with the .epub file type
                var options = new PickOptions
                {
                    PickerTitle = "Please select an EPUB file",
                    FileTypes = epubFileType
                };

                // Open the file picker
                var loadedFile = await FilePicker.Default.PickAsync(options);

                if (loadedFile != null)
                {
                    // File selected
                    EpubStateDisplayMessage = $"Selected file: {loadedFile.FileName}";

                    // Read the loaded file content
                    var book = await EpubReader.ReadBookAsync(await loadedFile.OpenReadAsync());

                    // Get all the text from the book
                    StringBuilder outerSB = new();
                    foreach (var textContent in book.ReadingOrder)
                    {
                        HtmlDocument htmlDocument = new();
                        htmlDocument.LoadHtml(textContent.Content);

                        StringBuilder innerSB = new();
                        foreach (var node in htmlDocument.DocumentNode.SelectNodes("//text()"))
                            innerSB.AppendLine(node.InnerText.Trim());

                        // TODO - Need to clear other possible tags from raw text, like @page etc.

                        outerSB.AppendLine(innerSB.ToString());
                    }
                    _completeBookText = outerSB.ToString();

                    IsTextSearchVisible = true;
                    IsMultipleTextSelectionVisible = false;
                    IsTextReadingVisible = false;
                    // TODO - Reset found text lists, variables, stop the reading etc.
                }
                else
                {
                    EpubStateDisplayMessage = "No file selected.";
                    IsTextSearchVisible = false;
                    IsMultipleTextSelectionVisible = false;
                    IsTextReadingVisible = false;
                    // TODO - Reset found text lists, variables, stop the reading etc.
                }
            }
            catch (Exception)
            {
                EpubStateDisplayMessage = "Something went wrong :(";
                IsTextSearchVisible = false;
                IsMultipleTextSelectionVisible = false;
                IsTextReadingVisible = false;
                // TODO - Reset found text lists, variables, stop the reading etc.
                // TODO - Log or set up notifications
            }
        }

        [RelayCommand]
        private void FindText()
        {
            // TODO Find the text the user entered, or set to beginning of the book if the text was empty
            IsMultipleTextSelectionVisible = true;
        }

        [RelayCommand]
        private void StartTextToSpeech()
        {
            // TODO Start text to speech from the found text
        }
    }
}
