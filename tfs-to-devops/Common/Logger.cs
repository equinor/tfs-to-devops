using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Logger
    {
        private static readonly ConsoleColor Foreground = Console.ForegroundColor;
        private static ConsoleColor background = Console.BackgroundColor;
        static Logger()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void Error(string errorText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorText);
            Console.ForegroundColor = Foreground;
        }

        public static void Warning(string warningText)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(warningText);
            Console.ForegroundColor = Foreground;
        }
        public static void Info(string infoText)
        {
            if (infoText.Contains("Tfs2015Client"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (infoText.Contains("AzureDevopsClient"))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine(infoText);
            Console.ForegroundColor = Foreground;
        }
    }
}
