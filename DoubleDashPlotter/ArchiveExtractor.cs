using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoubleDashPlotter
{
    internal class ArchiveExtractor
    {
        public static async Task Extract7zAsync(string archivePath, string destinationPath, IProgress<int> progress)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException("The specified .7z archive file does not exist.", archivePath);
            }

            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            using (var archive = SevenZipArchive.Open(archivePath))
            {
                // Find folders that contain both files and subdirectories
                var groupedEntries = archive.Entries
                    .Where(entry => !entry.IsDirectory)
                    .GroupBy(entry => Path.GetDirectoryName(entry.Key)) // Group by folder name
                    .Where(group => group.Key != null) // Ensure the folder key is not null
                    .ToList();

                var validParentFolders = groupedEntries
                    .Where(group => archive.Entries.Any(e => e.IsDirectory && e.Key.StartsWith(group.Key)))
                    .Select(group => group.Key)
                    .Distinct()
                    .ToList();

                if (validParentFolders.Count == 0)
                {
                    throw new InvalidOperationException("No valid folder found that contains both files and subdirectories.");
                }
                if (validParentFolders.Count > 1)
                {
                    throw new InvalidOperationException("Multiple valid folders contain both files and subdirectories. Extraction cannot proceed.");
                }

                string mainFolder = validParentFolders.First();
                Console.WriteLine($"Extracting contents from: {mainFolder}");

                var validEntries = archive.Entries
                    .Where(e => e.Key.StartsWith(mainFolder) && !e.IsDirectory && e.IsComplete) // Ensure it's a file and has valid data
                    .ToList();

                int totalEntries = validEntries.Count;
                int extractedCount = 0;

                foreach (var entry in validEntries)
                {
                    string relativePath = entry.Key.Substring(mainFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string fullPath = Path.Combine(destinationPath, relativePath);

                    // Ensure the directory structure exists
                    string directory = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    Console.WriteLine($"Extracting: {entry.Key}");

                    try
                    {
                        using (var entryStream = entry.OpenEntryStream())
                        {
                            if (entryStream == null)
                            {
                                Console.WriteLine($"[WARNING] Skipping {entry.Key} (no valid data stream).");
                                continue;
                            }

                            using (var fileStream = File.Create(fullPath))
                            {
                                await entryStream.CopyToAsync(fileStream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to extract {entry.Key}: {ex.Message}");
                        continue;
                    }

                    extractedCount++;
                    int progressPercent = (int)((double)extractedCount / totalEntries * 100);
                    progress?.Report(progressPercent);
                }
            }
        }
    }
}
