using System.Reflection;

namespace Mantikor
{
    internal class Program
    {
        //--Variables
        private static readonly Menu_Class menu = new();
        private static readonly TargetList_Class targetList = new();
        private static readonly Attack_Class attack = new();

        static void Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            TrySetConsoleTitle("MANTIKOR v." + Assembly.GetExecutingAssembly().GetName().Version);

            bool running = true;
            while (running)
            {
                Menu_Class.PrintFrontend(targetList, attack);
                Console.Write("#>");
                string? input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        menu.ConfigureNetworkAdapter();
                        break;
                    case "2":
                        targetList.AddNewTarget(menu.captureDevice);
                        break;
                    case "3":
                        targetList.PrintTargetList();
                        break;
                    case "4":
                        attack.StartAttack(menu.captureDevice, targetList);
                        break;
                    case "5":
                        attack.ForceStop();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        break;
                }
            }

            // Stop every worker thread so the process can exit cleanly.
            attack.ForceStop();
        }

        /// <summary>
        /// Setting the console title is safe on Windows and Linux terminals, but
        /// can throw in redirected/headless environments - never fatal here.
        /// </summary>
        private static void TrySetConsoleTitle(string pTitle)
        {
            try
            {
                Console.Title = pTitle;
            }
            catch (Exception)
            {
                // Ignored - the title is cosmetic.
            }
        }
    }
}
