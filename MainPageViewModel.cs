using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiEpubTTSReader.Services;
using MauiEpubTTSReader.Models;
using MauiEpubTTSReader.Enums;

namespace MauiEpubTTSReader
{
    public partial class MainPageViewModel : ObservableObject
    {
        const short _matchingTextOccurrancesSearchLimit = 100;
        const short _extraSubstringPreviewCharacters = 40;

        [ObservableProperty]
        private string _epubStateDisplayMessage = string.Empty;

        [ObservableProperty]
        private string _searchStateDisplayMessage = string.Empty;

        [ObservableProperty]
        private string _textToFind = string.Empty;

        [ObservableProperty]
        private List<SubstringLocation> _locatedSubstringOccurrences = [];

        [ObservableProperty]
        private SubstringLocation? _selectedSubstringOccurrence;

        [ObservableProperty]
        private bool _isTextSearchVisible;
        [ObservableProperty]
        private bool _isMultipleSubstringSelectionVisible;
        [ObservableProperty]
        private bool _isTextListeningVisible;

        private string _epubFileName = string.Empty;
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
                // Search text was left empty, listening will start from the beginning of the book
                LocatedSubstringOccurrences.Clear();
                SelectedSubstringOccurrence = new SubstringLocation { Index = 0, SubstringPreview = string.Empty };
                CurrentUIState = UIStateEnum.FindTextEmpty;
            }
            else
            {
                bool _wasTextOccurrancesLimitHit;

                // Search text was entered, try to find textOccurrences in the book
                (LocatedSubstringOccurrences, _wasTextOccurrancesLimitHit) = 
                    await TextScanner.FindTextOccurrancesAsync(TextToFind, _completeBookText, _extraSubstringPreviewCharacters, _matchingTextOccurrancesSearchLimit);
                if (LocatedSubstringOccurrences.Count == 1)
                {
                    SelectedSubstringOccurrence = LocatedSubstringOccurrences[0];
                    CurrentUIState = UIStateEnum.FindTextSingleOccurranceFound;
                }
                else if (LocatedSubstringOccurrences.Count > 1)
                {
                    SelectedSubstringOccurrence = LocatedSubstringOccurrences[0];
                    CurrentUIState = _wasTextOccurrancesLimitHit
                        ? UIStateEnum.FindTextMultipleOccurrancesSearchLimitHit
                        : UIStateEnum.FindTextMultipleOccurrancesFound;
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
        private UIStateEnum? _currentUIState = null;
        partial void OnCurrentUIStateChanged(UIStateEnum? value) => UpdateUIState(value);
        private void UpdateUIState(UIStateEnum? appState)
        {
            switch (appState)
            {
                case UIStateEnum.EpubLoadedOk:
                    EpubStateDisplayMessage = !string.IsNullOrEmpty(_epubFileName) ? $"Selected file: {_epubFileName}" : "Selected file.";
                    IsTextSearchVisible = true;
                    IsMultipleSubstringSelectionVisible = IsTextListeningVisible = false;
                    break;
                case UIStateEnum.EpubLoadedTextEmpty:
                    EpubStateDisplayMessage = !string.IsNullOrEmpty(_epubFileName) ? $"Epub text returned empty: {_epubFileName}" : "Epub text returned empty.";
                    IsTextSearchVisible = IsMultipleSubstringSelectionVisible = IsTextListeningVisible = false;
                    break;
                case UIStateEnum.EpubLoadError:
                    EpubStateDisplayMessage = "Loading the Epub file has failed or was cancelled.";
                    IsTextSearchVisible = IsMultipleSubstringSelectionVisible = IsTextListeningVisible = false;
                    break;
                case UIStateEnum.FindTextEmpty:
                    SearchStateDisplayMessage = "Listening will start from the beginning.";
                    IsMultipleSubstringSelectionVisible = false;
                    IsTextListeningVisible = true;
                    break;
                case UIStateEnum.FindTextSingleOccurranceFound:
                    SearchStateDisplayMessage = "Matching text found. You can start listening now.";
                    IsMultipleSubstringSelectionVisible = false;
                    IsTextListeningVisible = true;
                    break;
                case UIStateEnum.FindTextMultipleOccurrancesFound:
                    SearchStateDisplayMessage = "Multiple matching text occurrances found, pick one and start listening.";
                    IsMultipleSubstringSelectionVisible = IsTextListeningVisible = true;
                    break;
                case UIStateEnum.FindTextMultipleOccurrancesSearchLimitHit:
                    SearchStateDisplayMessage = $"Maximum matching text occurrances found ({_matchingTextOccurrancesSearchLimit}).\n" +
                        $"Please refine your query or select one of the available occurrances and start listening.";
                    IsMultipleSubstringSelectionVisible = IsTextListeningVisible = true;
                    break;
                case UIStateEnum.FindTextNotFound:
                    SearchStateDisplayMessage = "No matching text found.";
                    IsMultipleSubstringSelectionVisible = IsTextListeningVisible = false;
                    break;
                default:
                    break;
            }
        }
        #endregion UI state handling
    }
}
