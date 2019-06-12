using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tfs_to_devops
{
    class Program
    {
        private static ConsoleColor foreground = Console.ForegroundColor;
        private static ConsoleColor background = Console.BackgroundColor;

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            if (args.Length != 4)
            {
                WriteError($"Invalid or missing paramaters{Environment.NewLine}Usage: tfs-to-devops [TFS server URL] [TFS project name] [Azure server URL] [Azure project name]");
                return;
            }

            try
            {
                var tfsServerUrl = args[0];
                var tfsProject = args[1];
                var azureUrl = args[2];
                var azureProject = args[3];

                WriteInfo($"TFS Server URL:   {tfsServerUrl}");
                WriteInfo($"TFS Project:      {tfsProject}");
                WriteInfo($"Azure Server URL: {azureUrl}");
                WriteInfo($"Azure Project:    {azureProject}");

                WriteInfo($"{Environment.NewLine}Reading from {Path.Combine(tfsServerUrl, tfsProject)}...");

                var azureClient = new AzureDevops.Client();
                var tfs2015Client = new Tfs2015Client.Client(tfsServerUrl, tfsProject);

                var tfsAreas = tfs2015Client.GetAreas();
                var tfsIterations = tfs2015Client.GetIterations();
                var tfsWorkitems = tfs2015Client.GetWorkitems();

                WriteInfo($"  Found {tfsIterations.Count()} iterations, {tfsAreas.Count()} areas and {tfsWorkitems.Count()} workitems / bugs");
            }
            catch (Exception e)
            {
                WriteError(e.Message);
            }
        }
        private static void WriteError(string errorText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorText);
            Console.ForegroundColor = foreground;
        }

        private static void WriteWarning(string warningText)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(warningText);
            Console.ForegroundColor = foreground;
        }
        private static void WriteInfo(string infoText)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(infoText);
            Console.ForegroundColor = foreground;
        }
    }
}
