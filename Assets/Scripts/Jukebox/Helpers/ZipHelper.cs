#region License
/*
    Copyright (c) 2015, Paweł Hofman (CodeTitans)
    All Rights Reserved.

    Licensed under MIT License
    For more information please visit:

    https://github.com/phofman/zip/blob/master/LICENSE
        or
    http://opensource.org/licenses/MIT


    For latest source code, documentation, samples
    and more information please visit:

    https://github.com/phofman/zip

    Modifications by RHeavenStudio for Jukebox
    # Jukebox 0.2.2
        + Added support for filtering files (https://stackoverflow.com/a/35416368)
        - Remove unused parameters and methods
*/
#endregion

using System.Text;

namespace System.IO.Compression
{
    /// <summary>
    /// Helper class to simplify operations over ZIP archive.
    /// </summary>
    public static class ZipHelper
    {
        /// <summary>
        /// Creates a zip archive that contains the files and directories from the specified directory.
        /// </summary>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, CompressionLevel.Optimal, false);
        }

        /// <summary>
        /// Creates a zip archive that contains the files and directories from the specified directory.
        /// </summary>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory)
        {
            if (string.IsNullOrEmpty(sourceDirectoryName))
                throw new ArgumentNullException("sourceDirectoryName");
            if (string.IsNullOrEmpty(destinationArchiveFileName))
                throw new ArgumentNullException("destinationArchiveFileName");

            var filesToAdd = Directory.GetFiles(sourceDirectoryName, "*", SearchOption.AllDirectories);
            var entryNames = GetEntryNames(filesToAdd, sourceDirectoryName, includeBaseDirectory);

            using (var zipFileStream = new FileStream(destinationArchiveFileName, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                {
                    for (int i = 0; i < filesToAdd.Length; i++)
                    {
                        archive.CreateEntryFromFile(filesToAdd[i], entryNames[i], compressionLevel);
                    }
                }
            }
        }

        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory, Predicate<string> filter)
        {
            if (string.IsNullOrEmpty(sourceDirectoryName))
            {
                throw new ArgumentNullException("sourceDirectoryName");
            }
            if (string.IsNullOrEmpty(destinationArchiveFileName))
            {
                throw new ArgumentNullException("destinationArchiveFileName");
            }
            var filesToAdd = Directory.GetFiles(sourceDirectoryName, "*", SearchOption.AllDirectories);
            var entryNames = GetEntryNames(filesToAdd, sourceDirectoryName, includeBaseDirectory);
            using (var zipFileStream = new FileStream(destinationArchiveFileName, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                {
                    for (int i = 0; i < filesToAdd.Length; i++)
                    {
                        // Add the following condition to do filtering:
                        if (!filter(filesToAdd[i]))
                        {
                            continue;
                        }
                        archive.CreateEntryFromFile(filesToAdd[i], entryNames[i], compressionLevel);
                    }
                }
            }
        }

        private static string[] GetEntryNames(string[] names, string sourceFolder, bool includeBaseName)
        {
            if (names == null || names.Length == 0)
                return new string[0];

            if (includeBaseName)
                sourceFolder = Path.GetDirectoryName(sourceFolder);

            int length = string.IsNullOrEmpty(sourceFolder) ? 0 : sourceFolder.Length;
            if (length > 0 && sourceFolder != null && sourceFolder[length - 1] != Path.DirectorySeparatorChar && sourceFolder[length - 1] != Path.AltDirectorySeparatorChar)
                length++;

            var result = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                result[i] = names[i].Substring(length);
            }

            return result;
        }

        /// <summary>
        /// Extracts all the files in the specified zip archive to a directory on the file system.
        /// </summary>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, true);
        }

        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwrite)
        {
            if (string.IsNullOrEmpty(sourceArchiveFileName))
                throw new ArgumentNullException("sourceArchiveFileName");
            if (string.IsNullOrEmpty(destinationDirectoryName))
                throw new ArgumentNullException("destinationDirectoryName");

            using (var zipFileStream = new FileStream(sourceArchiveFileName, FileMode.Open))
            {
                using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(destinationDirectoryName, overwrite);
                }
            }
        }

        /// <summary>
        /// Opens a zip archive at the specified path and in the specified mode. 
        /// </summary>
        public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode)
        {
            if (string.IsNullOrEmpty(archiveFileName))
                throw new ArgumentNullException("archiveFileName");

            switch (mode)
            {
                case ZipArchiveMode.Create:
                    return new ZipArchive(new FileStream(archiveFileName, FileMode.Create), ZipArchiveMode.Create);
                case ZipArchiveMode.Update:
                    return new ZipArchive(new FileStream(archiveFileName, FileMode.OpenOrCreate), ZipArchiveMode.Update);
                case ZipArchiveMode.Read:
                    return new ZipArchive(new FileStream(archiveFileName, FileMode.Open), ZipArchiveMode.Read);
                default:
                    throw new IOException("Unsupported archive mode");
            }
        }

        /// <summary>
        /// Opens a zip archive for reading at the specified path.
        /// </summary>
        public static ZipArchive OpenRead(string archiveFileName)
        {
            return Open(archiveFileName, ZipArchiveMode.Read);
        }
    }
}