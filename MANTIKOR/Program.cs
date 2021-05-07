using System;
using System.Reflection;

namespace MANTIKOR
{
    class Program
    {
        private static Menu menu = new Menu();

        static void Main(string[] args)
        {
            Console.Title = "MANTIKOR v." + Assembly.GetExecutingAssembly().GetName().Version;

            while (true)
            {
                menu.printFrontend();
                Console.Write("#>");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        menu.configureNetworkAdapter();
                        break;
                    case "2":
                        menu.addNewTarget();
                        break;
                    case "3":
                        menu.printTargetList();
                        break;
                    case "4":
                        menu.startAttack();
                        break;
                    case "5":
                        menu.forceStop();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}