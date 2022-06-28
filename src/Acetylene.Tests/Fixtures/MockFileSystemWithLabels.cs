using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Acetylene.Tests.Fixtures;

using XFS = MockUnixSupport;

[Serializable]
public class MockFileSystemWithLabels : FileSystemBase, IMockFileDataAccessor {
    private const string DEFAULT_CURRENT_DIRECTORY = @"C:\";
    private const string TEMP_DIRECTORY = @"C:\temp";

    private readonly IDictionary<string, FileSystemEntry> files;
    private readonly PathVerifier pathVerifier;

    public MockFileSystemWithLabels(IDictionary<string, MockFileData> files, string currentDirectory = "",
        Dictionary<string, string> volumeLabels = null) {
        if (string.IsNullOrEmpty(currentDirectory)) {
            currentDirectory = XFS.Path(DEFAULT_CURRENT_DIRECTORY);
        }
        else if (!System.IO.Path.IsPathRooted(currentDirectory)) {
            throw new ArgumentException("Current directory needs to be rooted.", nameof(currentDirectory));
        }

        var defaultTempDirectory = XFS.Path(TEMP_DIRECTORY);

        StringOperations = new StringOperations(XFS.IsUnixPlatform());
        pathVerifier = new PathVerifier(this);
        this.files = new Dictionary<string, FileSystemEntry>(StringOperations.Comparer);

        Path = new MockPath(this, defaultTempDirectory);
        File = new MockFile(this);
        Directory = new MockDirectory(this, currentDirectory);
        FileInfo = new MockFileInfoFactory(this);
        FileStream = new MockFileStreamFactory(this);
        DirectoryInfo = new MockDirectoryInfoFactory(this);
        DriveInfo = new MockDriveInfoFactoryWithLabels(this, volumeLabels);

        FileSystemWatcher = new MockFileSystemWatcherFactory();

        if (files != null) {
            foreach (var entry in files) {
                AddFile(entry.Key, entry.Value);
            }
        }

        if (!FileExists(currentDirectory)) {
            AddDirectory(currentDirectory);
        }

        if (!FileExists(defaultTempDirectory)) {
            AddDirectory(defaultTempDirectory);
        }
    }

    public StringOperations StringOperations { get; }
    public override IFile File { get; }
    public override IDirectory Directory { get; }
    public override IFileInfoFactory FileInfo { get; }
    public override IFileStreamFactory FileStream { get; }
    public override IPath Path { get; }
    public override IDirectoryInfoFactory DirectoryInfo { get; }
    public override IDriveInfoFactory DriveInfo { get; }
    public override IFileSystemWatcherFactory FileSystemWatcher { get; }
    public IFileSystem FileSystem => this;
    public PathVerifier PathVerifier => pathVerifier;

    private string FixPath(string path, bool checkCaps = false) {
        if (path == null) {
            throw new ArgumentNullException(nameof(path), StringResources.Manager.GetString("VALUE_CANNOT_BE_NULL"));
        }

        var pathSeparatorFixed = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(pathSeparatorFixed);

        return checkCaps ? GetPathWithCorrectDirectoryCapitalization(fullPath) : fullPath;
    }

    //If C:\foo exists, ensures that trying to save a file to "C:\FOO\file.txt" instead saves it to "C:\foo\file.txt".
    private string GetPathWithCorrectDirectoryCapitalization(string fullPath) {
        string[] splitPath = fullPath.Split(Path.DirectorySeparatorChar);
        string leftHalf = fullPath;
        string rightHalf = "";

        for (int i = splitPath.Length - 1; i > 1; i--) {
            rightHalf = i == splitPath.Length - 1
                ? splitPath[i]
                : splitPath[i] + Path.DirectorySeparatorChar + rightHalf;
            int lastSeparator = leftHalf.LastIndexOf(Path.DirectorySeparatorChar);
            leftHalf = lastSeparator > 0 ? leftHalf.Substring(0, lastSeparator) : leftHalf;

            if (DirectoryExistsWithoutFixingPath(leftHalf)) {
                string baseDirectory = files[leftHalf].Path;
                return baseDirectory + Path.DirectorySeparatorChar + rightHalf;
            }
        }

        return fullPath.TrimSlashes();
    }

    /// <inheritdoc />
    public MockFileData GetFile(string path) {
        path = FixPath(path).TrimSlashes();
        return GetFileWithoutFixingPath(path);
    }

    private void SetEntry(string path, MockFileData mockFile) {
        path = FixPath(path, true).TrimSlashes();
        files[path] = new FileSystemEntry { Path = path, Data = mockFile };
    }

    /// <inheritdoc />
    public void AddFile(string path, MockFileData mockFile) {
        var fixedPath = FixPath(path, true);
        lock (files) {
            var file = GetFile(fixedPath);

            if (file != null) {
                var isReadOnly = (file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                var isHidden = (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                if (isReadOnly || isHidden) {
                    throw CommonExceptions.AccessDenied(path);
                }

                //file.CheckFileAccess(fixedPath, FileAccess.Write);
            }

            var directoryPath = Path.GetDirectoryName(fixedPath);

            if (!DirectoryExistsWithoutFixingPath(directoryPath)) {
                AddDirectory(directoryPath);
            }

            SetEntry(fixedPath, mockFile ?? new MockFileData(string.Empty));
        }
    }

    /// <inheritdoc />
    public void AddDirectory(string path) {
        var fixedPath = FixPath(path, true);
        var separator = Path.DirectorySeparatorChar.ToString();

        lock (files) {
            if (FileExists(fixedPath) &&
                (GetFile(fixedPath).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
                throw CommonExceptions.AccessDenied(fixedPath);
            }

            var lastIndex = 0;
            var isUnc =
                StringOperations.StartsWith(fixedPath, @"\\") ||
                StringOperations.StartsWith(fixedPath, @"//");

            if (isUnc) {
                //First, confirm they aren't trying to create '\\server\'
                lastIndex = StringOperations.IndexOf(fixedPath, separator, 2);

                if (lastIndex < 0) {
                    throw CommonExceptions.InvalidUncPath(nameof(path));
                }

                /*
                 * Although CreateDirectory(@"\\server\share\") is not going to work in real code, we allow it here for the purposes of setting up test doubles.
                 * See PR https://github.com/TestableIO/System.IO.Abstractions/pull/90 for conversation
                 */
            }

            while ((lastIndex = StringOperations.IndexOf(fixedPath, separator, lastIndex + 1)) > -1) {
                var segment = fixedPath.Substring(0, lastIndex + 1);
                if (!DirectoryExistsWithoutFixingPath(segment)) {
                    SetEntry(segment, new MockDirectoryData());
                }
            }

            var s = StringOperations.EndsWith(fixedPath, separator) ? fixedPath : fixedPath + separator;
            SetEntry(s, new MockDirectoryData());
        }
    }

    /// <inheritdoc />
    public void AddFileFromEmbeddedResource(string path, Assembly resourceAssembly, string embeddedResourcePath) {
        using (var embeddedResourceStream = resourceAssembly.GetManifestResourceStream(embeddedResourcePath)) {
            if (embeddedResourceStream == null) {
                throw new ArgumentException("Resource not found in assembly", nameof(embeddedResourcePath));
            }

            using (var streamReader = new BinaryReader(embeddedResourceStream)) {
                var fileData = streamReader.ReadBytes((int)embeddedResourceStream.Length);
                AddFile(path, new MockFileData(fileData));
            }
        }
    }

    /// <inheritdoc />
    public void AddFilesFromEmbeddedNamespace(string path, Assembly resourceAssembly, string embeddedRresourcePath) {
        var matchingResources =
            resourceAssembly.GetManifestResourceNames().Where(f => f.StartsWith(embeddedRresourcePath));
        foreach (var resource in matchingResources) {
            using (var embeddedResourceStream = resourceAssembly.GetManifestResourceStream(resource))
            using (var streamReader = new BinaryReader(embeddedResourceStream)) {
                var fileName = resource.Substring(embeddedRresourcePath.Length + 1);
                var fileData = streamReader.ReadBytes((int)embeddedResourceStream.Length);
                var filePath = Path.Combine(path, fileName);
                AddFile(filePath, new MockFileData(fileData));
            }
        }
    }

    /// <inheritdoc />
    public void MoveDirectory(string sourcePath, string destPath) {
        sourcePath = FixPath(sourcePath);
        destPath = FixPath(destPath);

        var sourcePathSequence =
            sourcePath.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

        lock (files) {
            var affectedPaths = files.Keys
                .Where(p => PathStartsWith(p, sourcePathSequence))
                .ToList();

            foreach (var path in affectedPaths) {
                var newPath = Path.Combine(destPath,
                    path.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar));
                var entry = files[path];
                entry.Path = newPath;
                files[newPath] = entry;
                files.Remove(path);
            }
        }

        bool PathStartsWith(string path, string[] minMatch) {
            var pathSequence = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSequence.Length < minMatch.Length) {
                return false;
            }

            for (var i = 0; i < minMatch.Length; i++) {
                if (!StringOperations.Equals(minMatch[i], pathSequence[i])) {
                    return false;
                }
            }

            return true;
        }
    }

    /// <inheritdoc />
    public void RemoveFile(string path) {
        path = FixPath(path);

        lock (files) {
            if (FileExists(path) && (GetFile(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
                throw CommonExceptions.AccessDenied(path);
            }

            files.Remove(path);
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path) {
        if (string.IsNullOrEmpty(path)) {
            return false;
        }

        path = FixPath(path).TrimSlashes();

        lock (files) {
            return files.ContainsKey(path);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> AllPaths {
        get {
            lock (files) {
                return files.Keys.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> AllNodes {
        get {
            lock (files) {
                return AllPaths.Where(path => !IsStartOfAnotherPath(path)).ToArray();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> AllFiles {
        get {
            lock (files) {
                return files.Where(f => !f.Value.Data.IsDirectory).Select(f => f.Key).ToArray();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> AllDirectories {
        get {
            lock (files) {
                return files.Where(f => f.Value.Data.IsDirectory).Select(f => f.Key).ToArray();
            }
        }
    }

    private bool IsStartOfAnotherPath(string path) {
        return AllPaths.Any(otherPath => otherPath.StartsWith(path) && otherPath != path);
    }

    private MockFileData GetFileWithoutFixingPath(string path) {
        lock (files) {
            return files.TryGetValue(path, out var result) ? result.Data : null;
        }
    }

    private bool DirectoryExistsWithoutFixingPath(string path) {
        lock (files) {
            return files.TryGetValue(path, out var result) && result.Data.IsDirectory;
        }
    }

    [Serializable]
    private class FileSystemEntry {
        public string Path { get; set; }
        public MockFileData Data { get; set; }
    }
}

internal static class StringResources {
    public static ResourceManager Manager { get; } = new ResourceManager(
        $"{typeof(StringResources).Namespace}.Properties.Resources",
        typeof(StringResources).GetTypeInfo().Assembly);
}

internal static class StringExtensions {
    [Pure]
    public static string[] SplitLines(this string input) {
        var list = new List<string>();
        using (var reader = new StringReader(input)) {
            string str;
            while ((str = reader.ReadLine()) != null) {
                list.Add(str);
            }
        }

        return list.ToArray();
    }

    [Pure]
    public static string Replace(this string source, string oldValue, string newValue,
        StringComparison comparisonType) {
        // from http://stackoverflow.com/a/22565605 with some adaptions
        if (string.IsNullOrEmpty(oldValue)) {
            throw new ArgumentNullException(nameof(oldValue));
        }

        if (source.Length == 0) {
            return source;
        }

        if (newValue == null) {
            newValue = string.Empty;
        }

        var result = new StringBuilder();
        int startingPos = 0;
        int nextMatch;
        while ((nextMatch = source.IndexOf(oldValue, startingPos, comparisonType)) > -1) {
            result.Append(source, startingPos, nextMatch - startingPos);
            result.Append(newValue);
            startingPos = nextMatch + oldValue.Length;
        }

        result.Append(source, startingPos, source.Length - startingPos);

        return result.ToString();
    }

    [Pure]
    public static string TrimSlashes(this string path) {
        if (string.IsNullOrEmpty(path)) {
            return path;
        }

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (XFS.IsUnixPlatform()
            && (path[0] == Path.DirectorySeparatorChar || path[0] == Path.AltDirectorySeparatorChar)
            && trimmed == "") {
            return Path.DirectorySeparatorChar.ToString();
        }

        if (XFS.IsWindowsPlatform()
            && trimmed.Length == 2
            && char.IsLetter(trimmed[0])
            && trimmed[1] == ':') {
            return trimmed + Path.DirectorySeparatorChar;
        }

        return trimmed;
    }

    [Pure]
    public static string NormalizeSlashes(this string path) {
        path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var sep = Path.DirectorySeparatorChar.ToString();
        var doubleSep = sep + sep;

        var prefixSeps = new string(path.TakeWhile(c => c == Path.DirectorySeparatorChar).ToArray());
        path = path.Substring(prefixSeps.Length);

        // UNC Paths start with double slash but no reason
        // to have more than 2 slashes at the start of a path
        if (XFS.IsWindowsPlatform() && prefixSeps.Length >= 2) {
            prefixSeps = prefixSeps.Substring(0, 2);
        }
        else if (prefixSeps.Length > 1) {
            prefixSeps = prefixSeps.Substring(0, 1);
        }

        while (true) {
            var newPath = path.Replace(doubleSep, sep);

            if (path == newPath) {
                return prefixSeps + path;
            }

            path = newPath;
        }
    }
}

internal static class CommonExceptions {
    private const int _fileLockHResult = unchecked((int)0x80070020);

    public static FileNotFoundException FileNotFound(string path) =>
        new FileNotFoundException(
            string.Format(
                CultureInfo.InvariantCulture,
                StringResources.Manager.GetString("COULD_NOT_FIND_FILE_EXCEPTION"),
                path
            ),
            path
        );

    public static DirectoryNotFoundException CouldNotFindPartOfPath(string path) =>
        new DirectoryNotFoundException(
            string.Format(
                CultureInfo.InvariantCulture,
                StringResources.Manager.GetString("COULD_NOT_FIND_PART_OF_PATH_EXCEPTION"),
                path
            )
        );

    public static UnauthorizedAccessException AccessDenied(string path) =>
        new UnauthorizedAccessException(
            string.Format(
                CultureInfo.InvariantCulture,
                StringResources.Manager.GetString("ACCESS_TO_THE_PATH_IS_DENIED"),
                path
            )
        );

    public static Exception InvalidUseOfVolumeSeparator() =>
        new NotSupportedException(StringResources.Manager.GetString("THE_PATH_IS_NOT_OF_A_LEGAL_FORM"));

    public static Exception PathIsNotOfALegalForm(string paramName) =>
        new ArgumentException(
            StringResources.Manager.GetString("THE_PATH_IS_NOT_OF_A_LEGAL_FORM"),
            paramName
        );

    public static ArgumentNullException FilenameCannotBeNull(string paramName) =>
        new ArgumentNullException(
            paramName,
            StringResources.Manager.GetString("FILENAME_CANNOT_BE_NULL")
        );

    public static ArgumentException IllegalCharactersInPath(string paramName = null) =>
        paramName != null
            ? new ArgumentException(StringResources.Manager.GetString("ILLEGAL_CHARACTERS_IN_PATH_EXCEPTION"),
                paramName)
            : new ArgumentException(StringResources.Manager.GetString("ILLEGAL_CHARACTERS_IN_PATH_EXCEPTION"));

    public static Exception InvalidUncPath(string paramName) =>
        new ArgumentException(@"The UNC path should be of the form \\server\share.", paramName);

    public static IOException ProcessCannotAccessFileInUse(string paramName = null) =>
        paramName != null
            ? new IOException(
                string.Format(StringResources.Manager.GetString("PROCESS_CANNOT_ACCESS_FILE_IN_USE_WITH_FILENAME"),
                    paramName), _fileLockHResult)
            : new IOException(StringResources.Manager.GetString("PROCESS_CANNOT_ACCESS_FILE_IN_USE"), _fileLockHResult);

    public static IOException FileAlreadyExists(string paramName) =>
        new IOException(string.Format(StringResources.Manager.GetString("FILE_ALREADY_EXISTS"), paramName));
}