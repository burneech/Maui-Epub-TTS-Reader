using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiEpubTTSReader.Services;
using MauiEpubTTSReader.Models;
using MauiEpubTTSReader.Enums;

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
        private string _epubStateDisplayMessage = string.Empty;

        private string _epubFileName = string.Empty;

        [ObservableProperty]
        private string _searchStateDisplayMessage = string.Empty;

        [ObservableProperty]
        private string _textToFind = string.Empty;

        [ObservableProperty]
        private List<SubstringLocation> _locatedSubstringOccurrences = [];

        [ObservableProperty]
        private SubstringLocation? _selectedSubstringOccurrence;

        private string _completeBookText = string.Empty;

        [RelayCommand]
        private async Task BrowseEpubFile()
        {
            var epubFile = await EpubFileHandler.LoadEpubFile();
            if (epubFile != null)
            {
                _completeBookText = await EpubFileHandler.ReadEpubFile(epubFile);

                if (string.IsNullOrEmpty(_completeBookText))
                {
                    CurrentUIState = UIStateEnum.EpubLoadedTextEmpty;
                    return;
                }

                _epubFileName = epubFile.FileName;
                CurrentUIState = UIStateEnum.EpubLoadedOk;
            }
            else
            {
                CurrentUIState = UIStateEnum.EpubLoadError;
            }
        }

        [RelayCommand]
        private async Task FindText()
        {
            if (string.IsNullOrEmpty(TextToFind))
            {
                // Search text was left empty, reading will start from the beginning of the book
                LocatedSubstringOccurrences.Clear();
                SelectedSubstringOccurrence = new SubstringLocation { Index = 0, SubstringPreview = string.Empty };
                CurrentUIState = UIStateEnum.FindTextEmpty;
            }
            else
            {
                // Search text was entered, try to find textOccurrences in the book
                LocatedSubstringOccurrences = await TextScanner.FindTextOccurrancesAsync(TextToFind, _completeBookText, 40);
                if (LocatedSubstringOccurrences.Count == 1)
                {
                    SelectedSubstringOccurrence = LocatedSubstringOccurrences[0];
                    CurrentUIState = UIStateEnum.FindTextSingleOccurranceFound;
                }
                else if (LocatedSubstringOccurrences.Count > 1)
                {
                    // TODO - Limit searching to first 100 occurrences, ask user to write a more unique text query (the return list could be too large for common words)
                    // TODO - Limit TextToFind length that can be entered in the UI
                    SelectedSubstringOccurrence = LocatedSubstringOccurrences[0];
                    CurrentUIState = UIStateEnum.FindTextMultipleOccurrancesFound;
                }
                else
                {
                    LocatedSubstringOccurrences.Clear();
                    SelectedSubstringOccurrence = null;
                    CurrentUIState = UIStateEnum.FindTextNotFound;
                }
            }
        }

        [RelayCommand]
        private void StartTextToSpeech()
        {
            // TODO Start text to speech from the found text
        }

        #region UI state handling
        [ObservableProperty]
        private UIStateEnum _currentUIState;
        partial void OnCurrentUIStateChanged(UIStateEnum value) => UpdateUIState(value);
        private void UpdateUIState(UIStateEnum appState)
        {
            switch (appState)
            {
                case UIStateEnum.EpubLoadedOk:
                    EpubStateDisplayMessage = !string.IsNullOrEmpty(_epubFileName) ? $"Selected file: {_epubFileName}" : "Selected file.";
                    IsTextSearchVisible = true;
                    IsMultipleSubstringSelectionVisible = IsTextReadingVisible = false;
                    break;
                case UIStateEnum.EpubLoadedTextEmpty:
                    EpubStateDisplayMessage = !string.IsNullOrEmpty(_epubFileName) ? $"Epub text returned empty: {_epubFileName}" : "Epub text returned empty.";
                    IsTextSearchVisible = IsMultipleSubstringSelectionVisible = IsTextReadingVisible = false;
                    break;
                case UIStateEnum.EpubLoadError:
                    EpubStateDisplayMessage = "Loading the Epub file has failed or was cancelled.";
                    IsTextSearchVisible = IsMultipleSubstringSelectionVisible = IsTextReadingVisible = false;
                    break;
                case UIStateEnum.FindTextEmpty:
                    SearchStateDisplayMessage = "Reading will start from the beginning.";
                    IsMultipleSubstringSelectionVisible = false;
                    IsTextReadingVisible = true;
                    break;
                case UIStateEnum.FindTextSingleOccurranceFound:
                    SearchStateDisplayMessage = "Matching text found. You can now start listening.";
                    IsMultipleSubstringSelectionVisible = false;
                    break;
                case UIStateEnum.FindTextMultipleOccurrancesFound:
                    SearchStateDisplayMessage = "Multiple matching text occurrances found. Select one and start listening.";
                    IsMultipleSubstringSelectionVisible = IsTextReadingVisible = true;
                    break;
                case UIStateEnum.FindTextNotFound:
                    SearchStateDisplayMessage = "No matching text found.";
                    IsMultipleSubstringSelectionVisible = IsTextReadingVisible = false;
                    break;
                default:
                    break;
            }
        }
        #endregion UI state handling
    }
}
