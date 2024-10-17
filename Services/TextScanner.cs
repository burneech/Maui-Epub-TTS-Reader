using MauiEpubTTSReader.Models;

namespace MauiEpubTTSReader.Services
{
    public class TextScanner
    {
        public static async Task<(List<SubstringLocation> results, bool wasLimitHit)> FindTextOccurrancesAsync(string textToFind, string textToScan, short extraSubstringPreviewCharacters, short occurrancesLimit)
        {
            return await Task.Run(() =>
            {
                var occurrences = new List<SubstringLocation>(occurrancesLimit);

                if (string.IsNullOrEmpty(textToFind) || string.IsNullOrEmpty(textToScan) || extraSubstringPreviewCharacters < 0)
                    return (occurrences, false);

                int index = textToScan.IndexOf(textToFind, StringComparison.OrdinalIgnoreCase);

                while (index != -1)
                {
                    // Calculate the end index for the substring preview (preview chars after the found text or until the end)
                    int endIndex = Math.Min(index + textToFind.Length + extraSubstringPreviewCharacters, textToScan.Length);

                    // Extract the substring: found text + preview characters
                    string foundSubstringText = textToScan[index..endIndex].Replace(Environment.NewLine, " ");

                    // TODO - Replace last three substring preview characters with dots if they can be sacrificed or if there is extra text after them

                    // Add the occurrence to the list
                    occurrences.Add(new SubstringLocation
                    {
                        Index = index,
                        SubstringPreview = foundSubstringText
                    });

                    if (occurrences.Count == occurrancesLimit)
                        return (occurrences, true);

                    // Continue searching after this occurrence
                    index = textToScan.IndexOf(textToFind, index + 1, StringComparison.OrdinalIgnoreCase);
                }

                return (occurrences, false);
            });
        }
    }
}
