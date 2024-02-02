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

            Console.Title = "MANTIKOR v." + Assembly.GetExecutingAssembly().GetName().Version;

            while (true)
            {
                Menu_Class.PrintFrontend(targetList, attack);
                Console.Write("#>");
                string input = Console.ReadLine();

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
                    default:
                        break;
                }
            }
        }
    }
}
