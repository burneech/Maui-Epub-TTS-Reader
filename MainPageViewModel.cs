using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiEpubTTSReader.Models;

namespace MauiEpubTTSReader
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isTextFindingVisible;
        [ObservableProperty]
        private bool _isMultipleTextSelectionVisible;
        [ObservableProperty]
        private bool _isTextReadingVisible;

        [ObservableProperty]
        private string _epubStateDisplayMessage = "No file selected";

        [ObservableProperty]
        private string _textToFind = string.Empty;

        private List<TextLocation> _locationsOfFoundText = new List<TextLocation>();
        public List<TextLocation> LocationsOfFoundText
        {
            get => _locationsOfFoundText;
            set
            {
                SetProperty(ref _locationsOfFoundText, value);
                if (value.Count > 1)
                    IsMultipleTextSelectionVisible = true;
                else
                {
                    IsMultipleTextSelectionVisible = false;
                    SelectedFoundTextLocation = value.First();
                }
            }
        }

        [ObservableProperty]
        private TextLocation? _selectedFoundTextLocation;

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
        private void BrowseEpubFile()
        {
            // TODO Load book and read its text
            IsTextFindingVisible = true;
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
