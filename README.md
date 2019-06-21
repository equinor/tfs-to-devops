# tfs-to-devops
Migration tool to ease the pain of moving from TFS to Azure DevOps

## Export workitems, backlogs, iterations etc
Exports workitems and bugs to Azure DevOps. Doesn't include Test Plans / Test Suits / Test Cases since there is no real API to support it.
```bash
tfs-to-devops export [TfsUrl] [TfsProject] [AzureUrl] [AzureProject]
```
```
[TfsUrl]       The URL used in Visual Studio, excluding project root
[TfsProject]   The root (project) that holds backlogs and workitems
[AzureUrl]     The Azure organization URL (https://dev.azure.com/<Organization>)
[AzureProject] Project found under organization in Azure server URL (https://dev.azure.com/<Organization>/<Project>)
```
## Export changesets with file changes
Creates a dump of changesets from a given branch and date period. One folder for each changeset, includes diffable versions of all changed files. Useful for maintaining history of TFS branches when moving to GitHub.

```bash
tfs-to-devops history [TfsUrl] [TfsBranchPath] [DateFrom] [DateTo] [Path]
```
```
[TfsUrl]              The URL used in Visual Studio, excluding project root
[TfsProject]          The root (project) that holds branches for history export
[TfsBranchPath]       Path to branch, excluding project
[DateFrom DD.MM.YYYY] The Azure organization URL (https://dev.azure.com/<Organization>)
[DateTo DD.MM.YYYY]   Project found under organization in Azure server URL
[Path]                Path to store changesets
```
