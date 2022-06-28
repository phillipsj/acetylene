using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Acetylene.Tests.Fixtures;

[Serializable]
public class MockDriveInfoFactoryWithLabels : IDriveInfoFactory {
    private readonly IMockFileDataAccessor _mockFileSystem;
    private readonly Dictionary<string, string> _volumeLabels;

    public MockDriveInfoFactoryWithLabels(IMockFileDataAccessor mockFileSystem) {
        _mockFileSystem = mockFileSystem ?? throw new ArgumentNullException(nameof(mockFileSystem));
    }

    public MockDriveInfoFactoryWithLabels(IMockFileDataAccessor mockFileSystem,
        Dictionary<string, string> volumeLabels) {
        _mockFileSystem = mockFileSystem ?? throw new ArgumentNullException(nameof(mockFileSystem));
        _volumeLabels = volumeLabels;
    }

    public IDriveInfo[] GetDrives() {
        var driveLetters = new HashSet<string>(new DriveEqualityComparer(_mockFileSystem));
        foreach (var path in _mockFileSystem.AllPaths) {
            var pathRoot = _mockFileSystem.Path.GetPathRoot(path);
            driveLetters.Add(pathRoot);
        }

        var result = new List<DriveInfoBase>();
        foreach (string driveLetter in driveLetters) {
            try {
                var mockDriveInfo = new MockDriveInfo(_mockFileSystem, driveLetter);
                if (_volumeLabels is not null) {
                    mockDriveInfo.VolumeLabel = _volumeLabels[mockDriveInfo.Name];
                }

                result.Add(mockDriveInfo);
            }
            catch (ArgumentException) {
                // invalid drives should be ignored
            }
        }

        return result.ToArray();
    }

    public IDriveInfo FromDriveName(string driveName) {
        var drive = _mockFileSystem.Path.GetPathRoot(driveName);

        return new MockDriveInfo(_mockFileSystem, drive);
    }

    private string NormalizeDriveName(string driveName) {
        if (driveName.Length == 3 && _mockFileSystem.StringOperations.EndsWith(driveName, @":\")) {
            return _mockFileSystem.StringOperations.ToUpper(driveName[0]) + @":\";
        }

        return _mockFileSystem.StringOperations.StartsWith(driveName, @"\\") ? null : driveName;
    }

    private class DriveEqualityComparer : IEqualityComparer<string> {
        private readonly IMockFileDataAccessor _mockFileSystem;

        public DriveEqualityComparer(IMockFileDataAccessor mockFileSystem) {
            _mockFileSystem = mockFileSystem ?? throw new ArgumentNullException(nameof(mockFileSystem));
        }

        public bool Equals(string x, string y) {
            return ReferenceEquals(x, y) ||
                   (HasDrivePrefix(x) && HasDrivePrefix(y) && _mockFileSystem.StringOperations.Equals(x[0], y[0]));
        }

        private static bool HasDrivePrefix(string x) {
            return x is { Length: >= 2 } && x[1] == ':';
        }

        public int GetHashCode(string obj) {
            return _mockFileSystem.StringOperations.ToUpper(obj).GetHashCode();
        }
    }
}