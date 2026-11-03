using System.IO;
using System.Runtime.CompilerServices;

namespace HCore.Test
{
    internal static class FixtureFile
    {
        // the fixtures live in the source tree, so a capture lands where git sees it
        public static string GetPath(string fileName, [CallerFilePath] string callerFilePath = "")
        {
            var testDirectory = Path.GetDirectoryName(callerFilePath);

            return Path.Combine(testDirectory, "Fixtures", fileName);
        }
    }
}
