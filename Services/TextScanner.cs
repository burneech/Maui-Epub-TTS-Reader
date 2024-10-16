using MauiEpubTTSReader.Models;

namespace MauiEpubTTSReader.Services
{
    public class TextScanner
    {
        public static async Task<List<SubstringLocation>> FindTextOccurrancesAsync(string textToFind, string textToScan, int extraSubstringPreviewCharacters)
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
