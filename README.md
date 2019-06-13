# tfs-to-devops
Migration tool that accomplishes two tasks relevant for moving from TFS to Azure DevOps

## Export workitems, backlogs, iterations etc
tfs-to-devops export [TfsUrl] [TfsProject] [AzureUrl] [AzureProject]
  TfsUrl        The URL used in Visual Studio, excluding project root
  TfsProject    The root (project) that holds backlogs and workitems
  AzureUrl      The Azure organization URL (https://dev.azure.com/<Organization>)
  AzureProject  Project found under organization in Azure server URL

## Export changesets with file changes
tfs-to-devops history [TfsUrl] [TfsBranchPath] [DateFrom] [DateTo] [Path]
  TfsUrl                The URL used in Visual Studio, excluding project root
  TfsProject            The root (project) that holds branches for history export
  TfsBranchPath         Path to branch, excluding project
  DateFrom (DD.MM.YYYY) The Azure organization URL (https://dev.azure.com/<Organization>)
  DateTo (DD.MM.YYYY)   Project found under organization in Azure server URL
  Path                  Path to store changesets

