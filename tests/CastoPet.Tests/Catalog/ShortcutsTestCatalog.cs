namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> ShortcutTestCases { get; } =
    [
        new("Shortcut service loads empty state and round trips", ShortcutServiceLoadsEmptyStateAndRoundTrips),
        new("Shortcut service normalizes duplicate identities", ShortcutServiceNormalizesDuplicateIdentities),
        new("Shortcut service appends candidates with contiguous ordering", ShortcutServiceAppendsCandidatesWithContiguousOrdering),
        new("Shortcut service mutates ordered entries", ShortcutServiceMutatesOrderedEntries),
        new("Shortcut service updates program launch options safely", ShortcutServiceUpdatesProgramLaunchOptionsSafely),
        new("Shortcut service enforces its entry limit", ShortcutServiceEnforcesEntryLimit),
        new("Shortcut service recovers malformed storage", ShortcutServiceRecoversMalformedStorage),
        new("Shortcut service isolates malformed entries", ShortcutServiceIsolatesMalformedEntries),
        new("Shortcut service notifies only after persisted mutations", ShortcutServiceNotifiesOnlyAfterPersistedMutations),
        new("Shortcut drop handler classifies existing file system items", ShortcutDropHandlerClassifiesExistingFileSystemItems),
        new("Shortcut drop handler rejects executable scripts", ShortcutDropHandlerRejectsExecutableScripts),
        new("Shortcut drop handler accepts safe web targets", ShortcutDropHandlerAcceptsSafeWebTargets),
        new("Shortcut drop handler accepts Steam game URIs", ShortcutDropHandlerAcceptsSteamGameUris),
        new("Shortcut drop handler rejects missing and unsafe inputs", ShortcutDropHandlerRejectsMissingAndUnsafeInputs),
        new("Shortcut drop handler aggregates mixed batch duplicates", ShortcutDropHandlerAggregatesMixedBatchDuplicates),
        new("Shortcut drop handler reports shortcut limit failures", ShortcutDropHandlerReportsShortcutLimitFailures),
        new("Shortcut launcher creates structured shell start info", ShortcutLauncherCreatesStructuredShellStartInfo),
        new("Shortcut launcher accepts every supported target type", ShortcutLauncherAcceptsEverySupportedTargetType),
        new("Shortcut launcher rejects missing and malformed definitions", ShortcutLauncherRejectsMissingAndMalformedDefinitions),
        new("Shortcut launcher rejects tampered executable file definitions", ShortcutLauncherRejectsTamperedExecutableFileDefinitions),
        new("Shortcut launcher contains and logs start failures", ShortcutLauncherContainsAndLogsStartFailures),
    ];
}
