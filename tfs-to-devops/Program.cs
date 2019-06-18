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
            try
            {
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
                        throw new ArgumentOutOfRangeException(ops.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.Error($"{e.Message}{Environment.NewLine}{e.StackTrace}");
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

                Logger.Info($"TFS Server URL:     {tfsServerUrl}");
                Logger.Info($"TFS Project:        {tfsProject}");
                Logger.Info($"Azure Server URL:   {azureUrl}");
                Logger.Info($"Azure Organization: TODO");
                Logger.Info($"Azure Project:      TODO");

                azureUrl = @"https://dev.azure.com/DispatchMigrationTest";
                azureProject = "DispatchGit";

                var azureClient = new AzureDevopsClient.Client(azureUrl, azureProject);
                azureClient.Connect();
                azureClient.Initialize();

                var tfs2015Client = new Tfs2015Client.Client(tfsServerUrl, tfsProject);
                tfs2015Client.Connect();
                tfs2015Client.Initialize();
                
                azureClient.CreateAreas(tfs2015Client.GetAreas());
                azureClient.CreateIterations(tfs2015Client.GetIterations());
                azureClient.CreateWorkitems(tfs2015Client.GetWorkitems());
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
            var tfsProject = args[2];
            var tfsBranch = args[3];
            var dateFrom = DateTime.Parse(args[4]);
            var dateTo = DateTime.Parse(args[5]);
            var path = args[6];

            Logger.Info($"TFS Server URL: {tfsServerUrl}");
            Logger.Info($"TFS project:    {tfsProject}");
            Logger.Info($"TFS branch:     {tfsBranch}");
            Logger.Info($"Date from-to:   {dateFrom.ToShortDateString()} - {dateTo.ToShortDateString()}");
            Logger.Info($"Output path:    {path}");

            var tfs2015Client = new Tfs2015Client.Client(tfsServerUrl, tfsProject);
            tfs2015Client.Connect();

            tfs2015Client.ExportHistory(tfsBranch, dateFrom, dateTo, path);
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
            if (args.Length == 7) return true;

            Logger.Error($"Invalid or missing paramaters{Environment.NewLine}Usage: tfs-to-devops history [TfsUrl] [TfsBranchPath] [DateFrom] [DateTo] [Path]");
            Logger.Error($"  [TfsUrl]   The URL used in Visual Studio, excluding project root");
            Logger.Error($"  [TfsProject] The root (project) that holds branches for history export");
            Logger.Error($"  [TfsBranchPath] Path to branch, excluding project");
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
