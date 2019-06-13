using Common;
using System;
using System.IO;
using System.Linq;

namespace tfs_to_devops
{
    class Program
    {
        private enum ProgramOperation
        {
            History,
            Export,
            Unknown
        }

        static void Main(string[] args)
        {
            var ops = validateParameters(args);
            
            switch (ops)
            {
                case ProgramOperation.History:
                    performHistory(args);
                    break;
                case ProgramOperation.Export:
                    performExport(args);
                    break;
                case ProgramOperation.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void performExport(string[] args)
        {
            try
            {
                var tfsServerUrl = args[1];
                var tfsProject = args[2];
                var azureUrl = args[3];
                var azureProject = args[4];

                Logger.Info($"Export backlogs and workitems{Environment.NewLine}--------------------------------------------------------------------------------");

                Logger.Info($"TFS Server URL:   {tfsServerUrl}");
                Logger.Info($"TFS Project:      {tfsProject}");
                Logger.Info($"Azure Server URL: {azureUrl}");
                Logger.Info($"Azure Project:    {azureProject}");

                var azureClient = new AzureDevopsClient.Client(azureUrl, azureProject);
                if (!azureClient.Connect())
                {
                    Logger.Error($"Unable to connect to {azureProject} on {azureUrl}");
                    return;
                }

                var tfs2015Client = new Tfs2015Client.Client(tfsServerUrl, tfsProject);
                if (!tfs2015Client.Connect())
                {
                    Logger.Error($"Unable to connect to {tfsProject} on {tfsServerUrl}");
                    return;
                }
                
                tfs2015Client.Initialize();

                //var tfsAreas = tfs2015Client.GetAreas();
                //var tfsIterations = tfs2015Client.GetIterations();
                //var tfsWorkitems = tfs2015Client.GetWorkitems();

                //Logger.Info($"  {tfsIterations.Count()} iterations");
                //Logger.Info($"  {tfsAreas.Count()} areas");
                //Logger.Info($"  Workitems:");

                //foreach (var kvp in tfsWorkitems)
                //{
                //    Logger.Info($"    {kvp.Key} - {kvp.Value.Length}");
                //}
            }
            catch (Exception e)
            {
                Logger.Error($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
        }

        private static void performHistory(string[] args)
        {
            Logger.Info($"Export changeset history{Environment.NewLine}--------------------------------------------------------------------------------");

            var tfsServerUrl = args[1];
            var tfsBranch = args[2];
            var dateFrom = DateTime.Parse(args[3]);
            var dateTo = DateTime.Parse(args[4]);
            var path = args[5];

            Logger.Info($"TFS Server URL: {tfsServerUrl}");
            Logger.Info($"TFS branch:     {tfsBranch}");
            Logger.Info($"Date from-to:   {dateFrom.Date} - {dateTo.Date}");
            Logger.Info($"Output path:    {path}");
        }

        private static ProgramOperation validateParameters(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Logger.Error($"Invalid or missing paramaters{Environment.NewLine}Usage: tfs-to-devops [history/export]");
                return ProgramOperation.Unknown;
            }

            switch (args[0].ToLower())
            {
                case "history":
                    return validateHistoryParameters(args) ? ProgramOperation.History : ProgramOperation.Unknown;
                case "export":
                    return validateExportParameters(args) ? ProgramOperation.Export : ProgramOperation.Unknown;
                default:
                    Logger.Error($"Invalid or missing paramaters{Environment.NewLine}Usage: tfs-to-devops [history/export]");
                    return ProgramOperation.Unknown;
            }
        }

        private static bool validateHistoryParameters(string[] args)
        {
            if (args.Length == 6) return true;

            Logger.Error($"Invalid or missing paramaters{Environment.NewLine}Usage: tfs-to-devops history [TfsUrl] [TfsBranchPath] [DateFrom] [DateTo] [Path]");
            Logger.Error($"  [TfsUrl]   The URL used in Visual Studio, excluding project root");
            Logger.Error($"  [TfsBranchPath] The root (project) that holds backlogs and workitems");
            Logger.Error($"  [DateFrom DD.MM.YYYY] The Azure organization URL (https://dev.azure.com/<Organization>)");
            Logger.Error($"  [DateTo DD.MM.YYYY] Project found under organization in Azure server URL");
            Logger.Error($"  [Path] Path to store changesets");
            return false;
        }

        private static bool validateExportParameters(string[] args)
        {
            if (args.Length == 5) return true;

            Logger.Error($"Invalid or missing paramaters{Environment.NewLine}Usage: tfs-to-devops export [TfsUrl] [TfsProject] [AzureUrl] [AzureProject]");
            Logger.Error($"  [TfsUrl]   The URL used in Visual Studio, excluding project root");
            Logger.Error($"  [TfsProject] The root (project) that holds backlogs and workitems");
            Logger.Error($"  [AzureUrl] The Azure organization URL (https://dev.azure.com/<Organization>)");
            Logger.Error($"  [AzureProject] Project found under organization in Azure server URL");
            return false;
        }
    }
}
