# tfs-to-devops
Migration tool that accomplishes two tasks relevant for moving from TFS to Azure DevOps

## Export workitems, backlogs, iterations etc
In progress...
```bash
tfs-to-devops export [TfsUrl] [TfsProject] [AzureUrl] [AzureProject]
```
[TfsUrl]   The URL used in Visual Studio, excluding project root
[TfsProject] The root (project) that holds backlogs and workitems
[AzureUrl] The Azure organization URL (https://dev.azure.com/<Organization>
[AzureProject] Project found under organization in Azure server URL (https://dev.azure.com/<Organization>/<Project>

## Export changesets with file changes
```bash
tfs-to-devops history [TfsUrl] [TfsBranchPath] [DateFrom] [DateTo] [Path]

```
