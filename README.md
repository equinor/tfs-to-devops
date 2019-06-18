# tfs-to-devops
Migration tool that accomplishes two tasks relevant for moving from TFS to Azure DevOps

## Export workitems, backlogs, iterations etc
```bash
tfs-to-devops export [TfsUrl] [TfsProject] [AzureUrl] [AzureProject]  
```

## Export changesets with file changes
```bash
tfs-to-devops history [TfsUrl] [TfsBranchPath] [DateFrom] [DateTo] [Path]
```
