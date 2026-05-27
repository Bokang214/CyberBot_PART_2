using System;
using System.Media;
using System.Threading;

namespace CyberBot_PART_2
{
    
    public class CyberBot
    {
        private string userName = string.Empty;

        // Public to access userName from GUI
        public string UserName
        {
            get => userName;
            set => userName = value;
        }

        // Public event for GUI to subscribe to responses (instead of Console)
        public event Action<string> OnResponse;

        public void Start()
        {
            // Original console code preserved but GUI will use methods directly
            // This maintains backward compatibility
            Console.Title = "Cybersecurity Awareness Bot";
            ShowHeader();
            PlayVoiceGreeting();
            GetUserName();
            WelcomeUser();
            // MenuLoop removed for GUI - handled by MainWindow
        }

        // All your original methods preserved exactly
        public void ShowHeader()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("=================================================");
            Console.WriteLine("========CYBERSECURITY AWARENESS BOT==============");
            Console.WriteLine("=================================================");
            Console.WriteLine(@" 
      ____      _               ____        _   
     / ___|   _| |__   ___ _ __| __ )  ___ | |_ 
    | |  | | | | '_ \ / _ \ '__|  _ \ / _ \| __|
    | |__| |_| | |_) |  __/ |  | |_) | (_) | |_ 
     \____\__, |_.__/ \___|_|  |____/ \___/ \__|
          |___/                                 
           Stay Safe Online!
");
            Console.ResetColor();
        }

        public void PlayVoiceGreeting()
        {
            try
            {
                string path = "welcome.wav";
                SoundPlayer player = new SoundPlayer(path);
                player.Load();
                player.PlaySync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🔊 Voice greeting could not play.");
                Console.WriteLine("Error: " + ex.Message);
                Console.ResetColor();
            }
        }

        public void GetUserName()
        {
            Console.Write("\nEnter your name: ");
            userName = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(userName))
            {
                Console.Write("Name cannot be empty. Try again: ");
                userName = Console.ReadLine();
            }
        }

        public void WelcomeUser()
        {
            TypeEffect($"\nHello, {userName}! Welcome to the Cybersecurity Awareness Bot.");
            TypeEffect("I'm here to help you stay safe online\n");
        }

        // Original menu - kept for console version compatibility
        private void MenuLoop()
        {
            while (true)
            {
                ShowMenu();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\n{userName}> ");
                Console.ResetColor();
                string choice = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(choice)) continue;

                switch (choice)
                {
                    case "1":
                        TypeEffect("My purpose is to educate you about cybersecurity and help you stay safe online.");
                        break;
                    case "2":
                        TypeEffect("Phishing is a scam where attackers trick you into giving away personal information through fake emails or websites");
                        break;
                    case "3":
                        TypeEffect("Use strong passwords with letters, numbers, and symbols. Avoid using personal information and never reuse passwords.");
                        break;
                    case "4":
                        TypeEffect("Safe browsing means visiting secure websites (https), avoiding suspicious links, and keeping your software updated.");
                        break;
                    case "5":
                        HandleGeneralQuestions();
                        break;
                    case "6":
                    case "exit":
                        TypeEffect($"Goodbye {userName}, stay safe online and remember, think before you click!");
                        return;
                    default:
                        ShowError("Please choose a valid option (1-6).");
                        break;
                }
            }
        }

        private void ShowMenu()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n================ MENU ================");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("1. What is my purpose?");
            Console.WriteLine("2. Phishing");
            Console.WriteLine("3. Passwords");
            Console.WriteLine("4. Safe Browsing");
            Console.WriteLine("5. General Questions");
            Console.WriteLine("6. Exit");
            Console.ResetColor();
        }

        private void HandleGeneralQuestions()
        {
            Console.Write("\nAsk (e.g. how are you, what can I ask): ");
            string input = Console.ReadLine()?.ToLower();
            if (string.IsNullOrWhiteSpace(input))
            {
                ShowError("Invalid question.");
                return;
            }

            if (input.Contains("how are you"))
                TypeEffect("I am doing great, thank you for asking!");
            else if (input.Contains("what can i ask"))
                TypeEffect("You can ask about phishing, passwords, safe browsing, or my purpose.");
            else if (input.Contains("purpose"))
                TypeEffect("My purpose is to help you stay safe online.");
            else
                ShowError("I didn't understand that question.");
        }

        public void TypeEffect(string message)
        {
            foreach (char c in message)
            {
                Console.Write(c);
                Thread.Sleep(15);
            }
            Console.WriteLine();
        }

        private void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
