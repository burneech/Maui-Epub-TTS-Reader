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
        private bool _isTextToSpeechRunning;
        public string ListenButtonText => IsTextToSpeechRunning ? "Stop listening" : "Listen";
        private CancellationTokenSource _cts;

        public bool IsTextToSpeechRunning
        {
            get => _isTextToSpeechRunning;
            private set
            {
                _isTextToSpeechRunning = value;
                OnPropertyChanged(nameof(IsTextToSpeechRunning));
                OnPropertyChanged(nameof(ListenButtonText));
            }
        }
        public IRelayCommand ToggleTextToSpeechCommand { get; }

        public MainPageViewModel()
        {
            ToggleTextToSpeechCommand = new RelayCommand(async () => await ToggleTextToSpeech());
        }

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
                SelectedSubstringOccurrence = null;
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

        private async Task ToggleTextToSpeech()
        {
            if (IsTextToSpeechRunning)
            {
                _cts.Cancel();
                IsTextToSpeechRunning = false;
            }
            else
            {
                _cts = new CancellationTokenSource();
                IsTextToSpeechRunning = true;
                await StartTextToSpeech(_cts.Token);
                IsTextToSpeechRunning = false;
            }
        }

        // TODO - Enable pausing sentences, skipping sentences in both directions, or fast forward / changing speed if the TTS supports it
        // TODO - List all the sentences in a Picker element and start listening from any one selected
        // TODO - Display the sentence currently being read
        // TODO - Extract all this TTS logic into its own service
        private async Task StartTextToSpeech(CancellationToken cancellationToken)
        {
            SelectedSubstringOccurrence ??= new SubstringLocation { Index = 0, SubstringPreview = "" };

            if (!string.IsNullOrEmpty(_completeBookText))
            {
                string textToRead = _completeBookText[SelectedSubstringOccurrence.Index..];

                // Split the text by sentence-ending punctuation (period, exclamation mark, question mark)
                string[] sentences = textToRead.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sentence in sentences)
                {
                    // Check if cancellation is requested
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Re-add the punctuation at the end of each sentence
                    string sentenceToSpeak = sentence.Trim() + ".";

                    // Call SpeakAsync to read each sentence
                    await TextToSpeech.Default.SpeakAsync(sentenceToSpeak, cancelToken: cancellationToken);
                }
            }
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
