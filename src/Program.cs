using System;
using System.Reflection;

namespace MANTIKOR
{
    class Program
    {
        private static Menu menu = new Menu();
        private static TargetList targetList = new TargetList();
        private static Attack attack = new Attack();

        static void Main(string[] args)
        {
            Console.Title = "MANTIKOR v." + Assembly.GetExecutingAssembly().GetName().Version;

            while (true)
            {
                menu.printFrontend(targetList, attack);
                Console.Write("#>");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        menu.configureNetworkAdapter();
                        break;
                    case "2":
                        targetList.addNewTarget();
                        break;
                    case "3":
                        targetList.printTargetList();
                        break;
                    case "4":
                        attack.startAttack(menu.captureDevice, targetList);
                        break;
                    case "5":
                        attack.forceStop();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}