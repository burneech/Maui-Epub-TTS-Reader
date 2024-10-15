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
        private bool _isMultipleSubstringSelectionVisible;
        [ObservableProperty]
        private bool _isTextReadingVisible;

        [ObservableProperty]
        private string _epubStateDisplayMessage = "No file selected";

        [ObservableProperty]
        private string _textToFind = string.Empty;

        private List<SubstringLocation> _locatedSubstringOccurrences = [];
        public List<SubstringLocation> LocatedSubstringOccurrences
        {
            get => _locatedSubstringOccurrences;
            set
            {
                SetProperty(ref _locatedSubstringOccurrences, value);
                //if (value.Count > 1)
                //    //IsMultipleSubstringSelectionVisible = true;
                //else
                //{
                //    //IsMultipleSubstringSelectionVisible = false;
                //    SelectedFoundTextLocation = value.First();
                //}
            }
        }

        [ObservableProperty]
        private SubstringLocation? _selectedSubstringOccurrence;

        private string _completeBookText = string.Empty;

        public MainPageViewModel()
        {
            // TODO Hook it up with real found locations of the text the user entered
            //LocatedSubstringOccurrences =
            //[
            //    new SubstringLocation { Index = 10, SubstringPreview = "This is the first text" },
            //    new SubstringLocation { Index = 50, SubstringPreview = "This is the second text" },
            //    new SubstringLocation { Index = 150, SubstringPreview = "This is the third text" },
            //];
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

                    // TODO - Could replace code above with method that returns true/false depending on the resulting string length > 0, to reduce nesting IFs

                    IsTextSearchVisible = true;
                    IsMultipleSubstringSelectionVisible = false;
                    IsTextReadingVisible = false;
                    // TODO - Reset found text lists, variables, stop the reading etc.
                }
                else
                {
                    EpubStateDisplayMessage = "No file selected.";
                    IsTextSearchVisible = false;
                    IsMultipleSubstringSelectionVisible = false;
                    IsTextReadingVisible = false;
                    // TODO - Reset found text lists, variables, stop the reading etc.
                }
            }
            catch (Exception)
            {
                EpubStateDisplayMessage = "Something went wrong :(";
                IsTextSearchVisible = false;
                IsMultipleSubstringSelectionVisible = false;
                IsTextReadingVisible = false;
                // TODO - Reset found text lists, variables, stop the reading etc.
                // TODO - Log or set up notifications
            }
        }

        [RelayCommand]
        private async Task FindText()
        {
            // TODO Find the text the user entered, or set to beginning of the book if the text was empty

            if (string.IsNullOrEmpty(TextToFind))
            {
                // Search text was left empty, offer reading from the beginning of the book
                LocatedSubstringOccurrences.Clear();
                SelectedSubstringOccurrence = new SubstringLocation { Index = 0, SubstringPreview = string.Empty };
                IsMultipleSubstringSelectionVisible = false;
                IsTextReadingVisible = true;
            }
            else
            {
                // Search text was entered, try to find textOccurrences in the book
                IsMultipleSubstringSelectionVisible = true;
                LocatedSubstringOccurrences = await FindTextOccurrancesAsync(TextToFind, _completeBookText, 40);
            }
        }

        [RelayCommand]
        private void StartTextToSpeech()
        {
            // TODO Start text to speech from the found text
        }

        public async Task<List<SubstringLocation>> FindTextOccurrancesAsync(string textToFind, string textToScan, int extraSubstringPreviewCharacters)
        {
            return await Task.Run(() =>
            {
                var occurrences = new List<SubstringLocation>();

                if (string.IsNullOrEmpty(textToFind) || string.IsNullOrEmpty(textToScan) || extraSubstringPreviewCharacters < 0)
                    return occurrences;

                int index = textToScan.IndexOf(textToFind, StringComparison.OrdinalIgnoreCase);

                while (index != -1)
                {
                    // Calculate the end index for the substring preview (preview chars after the found text or until the end)
                    int endIndex = Math.Min(index + textToFind.Length + extraSubstringPreviewCharacters, textToScan.Length);

                    // Extract the substring: found text + preview characters
                    string foundSubstringText = textToScan.Substring(index, endIndex - index);

                    // TODO - Replace last three substring preview characters with dots if they can be sacrificed or if there is extra text after them

                    // Add the occurrence to the list
                    occurrences.Add(new SubstringLocation
                    {
                        Index = index,
                        SubstringPreview = foundSubstringText
                    });

                    // Continue searching after this occurrence
                    index = textToScan.IndexOf(textToFind, index + 1, StringComparison.OrdinalIgnoreCase);
                }

                return occurrences;
            });
        }
    }
}
