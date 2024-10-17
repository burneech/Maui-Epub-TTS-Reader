using HtmlAgilityPack;
using VersOne.Epub;
using System.Text;

namespace MauiEpubTTSReader.Services
{
    public class EpubFileHandler
    {
        public static async Task<FileResult?> LoadEpubFile()
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

            return loadedFile ?? null;
        }

        public static async Task<string> ReadEpubFile(FileResult epubFileToRead)
        {
            if (epubFileToRead != null)
            {
                // Read the loaded file content
                var book = await EpubReader.ReadBookAsync(await epubFileToRead.OpenReadAsync());

                // Get all the text from the book
                StringBuilder outerSB = new();
                foreach (var textContent in book.ReadingOrder)
                {
                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(textContent.Content);

                    StringBuilder innerSB = new();
                    foreach (var node in htmlDocument.DocumentNode.SelectNodes("//text()"))
                        innerSB.AppendLine(node.InnerText.Trim());

                    // TODO - Should clear other possible tags from raw text, like @page etc.

                    outerSB.AppendLine(innerSB.ToString());
                }
                return outerSB.ToString();
            }
            return string.Empty;
        }
    }
}
