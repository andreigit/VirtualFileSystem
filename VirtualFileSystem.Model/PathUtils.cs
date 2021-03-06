﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualFileSystem.Model
{

    /// <summary>
    /// File System Path Utils
    /// </summary>
    public static partial class PathUtils
    {

        // Path Separator Validators

        public static bool IsPathSeparator(char c) =>
            Consts.PathSeparators.Contains(c);

        // Volume Name Validators

        public static bool IsVolumeSeparator(char c) =>
            c == Consts.VolumeSeparator;

        public static bool IsValidVolumeChar(char c) =>
            Consts.ValidVolumeChars.Concat(Consts.AltValidVolumeChars).Contains(c);

        public static bool IsValidVolumeName(string name) =>
            !string.IsNullOrEmpty(name) &&
            Consts.ValidVolumeNames.Concat(Consts.AltValidVolumeNames).Contains(name);

        // Other Path Validators

        public static bool IsAbsolutePath(string path) =>
            !string.IsNullOrEmpty(path) &&
            Consts.ValidVolumeNames.Concat(Consts.AltValidVolumeNames).Any(
                volume => path.StartsWith(volume, StringComparison.Ordinal)
            );

        public static bool IsValidPathChar(char c) =>
            !Consts.InvalidPathChars.Contains(c);

        public static bool IsValidFileNameChar(char c) =>
            !Consts.InvalidFileNameChars.Contains(c);

        private static bool IsValidName(string name, int maxLength, Func<char, bool> isValidChar) =>
            !string.IsNullOrEmpty(name) && name.Length <= maxLength && name.All(isValidChar);

        public static bool IsValidFileSystemName(string name) =>
            IsValidName(name, Consts.MaxFileSystemNameLength, IsValidFileNameChar);

        public static bool IsValidDirectoryName(string name) =>
            IsValidName(name, Consts.MaxDirectoryNameLength, IsValidFileNameChar);

        public static bool IsValidFileName(string name) =>
            IsValidName(name, Consts.MaxFileNameLength, IsValidFileNameChar);

        // Path Combining/Splitting

        public static string CombinePath(string path1, string relativePath2)
        {
            if (path1 is null)
                path1 = string.Empty;

            if (relativePath2 is null)
                relativePath2 = string.Empty;

            relativePath2 = relativePath2.Trim().TrimStart(Consts.PathSeparators.ToArray());
            if (IsAbsolutePath(relativePath2))
                return relativePath2;

            path1 = path1.Trim().TrimEnd(Consts.PathSeparators.ToArray());

            if (path1.Length == 0 && relativePath2.Length == 0)
                return string.Empty;

            if (path1.Length == 0)
                return relativePath2;

            if (relativePath2.Length == 0)
                return path1;

            return path1 + new string(Consts.PathSeparator, 1) + relativePath2;
        }

        public static string[] SplitPath(string path) =>
            path?.Split(Consts.PathSeparators.ToArray(), StringSplitOptions.RemoveEmptyEntries) ?? new string[] { };

        /// <summary>
        /// Static constructor
        /// </summary>
        /// <remarks>
        /// Needs for the guaranted static fields initialization in a multithreading work
        /// </remarks>
        static PathUtils()
        {
        }

    }

}
