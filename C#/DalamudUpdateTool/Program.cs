using System.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DalamudUpdateTool {
    static class Program {
        private static FileInfo ResolveFilePath() {
            string appDataDir;

            try {
                appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            catch (PlatformNotSupportedException e) {
                Console.WriteLine(e);
                throw;
            }

            if (string.IsNullOrEmpty(appDataDir)) {
                throw new ApplicationException("The AppData directory could not be located.");
            }

            return new FileInfo($"{appDataDir}/XIVLauncher/dalamudConfig.json");
        }

        private static async Task<string?> UpdateFileContents(FileInfo file, string? betaKind, string? betaKey) {
            var contentsJson = await File.ReadAllTextAsync(file.FullName);
            var jsonContent = JsonConvert.DeserializeObject(contentsJson) as JObject;
            if (betaKind != null) {
                var betaKindObject = jsonContent?.SelectToken("betaKind");
                betaKindObject?.Replace(betaKind);
            }

            if (betaKey == null) return jsonContent?.ToString();

            var betaKeyObject = jsonContent?.SelectToken("betaKey");
            betaKeyObject?.Replace(betaKey);

            return jsonContent?.ToString();
        }

        private static async Task WriteEditedContentsAtomic(FileInfo file, string contents) {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, contents);

            File.Move(tempFile, file.FullName, true);
        }

        private static void PrintEditedContents(string contents) {
            Console.Write(contents);
        }

        private static void BackupFile(FileInfo file) {
            var now = DateTime.Now;

            var newName = file.FullName + $"{now:dd-MM-yyyy--HH-mm}";
            File.Copy(file.FullName, newName, true);
        }

        private static int Main(string[] args) {
            var betaKindOption =
                new Option<string>(aliases: ["-bkind", "--beta-kind"], description: "The beta 'kind' to use.",
                    getDefaultValue: () => "release");
            var betaKeyOption = new Option<string>(aliases: ["-bkey", "--beta-key"],
                description: "The beta key to use.",
                getDefaultValue: () => "");
            var noBackupOption = new Option<bool>(aliases: ["-nb", "--no-backup"],
                description: "Do not backup the config file before editing.", getDefaultValue: () => false);
            var dryRunOption = new Option<bool>(aliases: ["-d", "--dry-run"],
                description: "Print the new contents of the file without editing or backing up.",
                getDefaultValue: () => false);

            var rootCommand =
                new RootCommand(
                    "Application to quickly and seamlessly edit and optionally backup your Dalamud config.");
            rootCommand.AddOption(betaKindOption);
            rootCommand.AddOption(betaKeyOption);
            rootCommand.AddOption(noBackupOption);
            rootCommand.AddOption(dryRunOption);

            rootCommand.SetHandler(async (betaKind, betaKey, noBackup, dryRun) => {
                    Console.WriteLine($"betaKind: {betaKind}");
                    Console.WriteLine($"betaKey: {betaKey}");
                    Console.WriteLine($"noBackup: {noBackup}");
                    Console.WriteLine($"dryRun: {dryRun}");

                    var configFile = ResolveFilePath();

                    var newContents = await UpdateFileContents(configFile, betaKind, betaKey);
                    if (newContents == null) {
                        Console.WriteLine("No change to config was made.");
                        Environment.Exit(0);
                    }

                    if (dryRun) {
                        PrintEditedContents(newContents);
                        Environment.Exit(0);
                    }

                    if (!noBackup) {
                        BackupFile(configFile);
                    }

                    await WriteEditedContentsAtomic(configFile, newContents);
                }, betaKeyOption, betaKindOption,
                noBackupOption, dryRunOption);

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}