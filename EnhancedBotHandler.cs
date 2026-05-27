using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CyberBot_PART_2
{
    // ══════════════════════════════════════════════════════════════════════
    //  Conversation history entry
    // ══════════════════════════════════════════════════════════════════════
    public class ConversationEntry
    {
        public string Role { get; set; }   // "User" or "Bot"
        public string Message { get; set; }
        public DateTime Time { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Quiz question model
    // ══════════════════════════════════════════════════════════════════════
    public class QuizQuestion
    {
        public string Question { get; set; }
        public string[] Options { get; set; }
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Sentiment enum
    // ══════════════════════════════════════════════════════════════════════
    public enum Sentiment { Neutral, Worried, Frustrated, Curious }

    // ══════════════════════════════════════════════════════════════════════
    // Adapter to make CyberBot work with GUI
    // with all Part 2 features
    // ══════════════════════════════════════════════════════════════════════
    public static class EnhancedBotHandler
    {


        private static string userName;
        private static string lastTopic;
        private static Random rand = new Random();

        //  Conversation memory / history
        public static List<ConversationEntry> ConversationHistory { get; } = new List<ConversationEntry>();

        // Track topics the user has visited
        private static HashSet<string> topicsVisited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Track how many times each topic was asked (for follow-up detection)
        private static Dictionary<string, int> topicAskCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Activity log path 
        private static readonly string LogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "cyberbot_activity.log");

        // ── Quiz runtime state ─────────────────────────────────────────────
        public static bool QuizActive { get; private set; } = false;
        private static List<QuizQuestion> quizPool;
        private static int quizIndex = 0;
        private static int quizScore = 0;
        public static int QuizTotal { get; private set; } = 0;
        public static int QuizCorrect { get; private set; } = 0;



        // Multiple varied responses per topic (random selection)
        private static List<string> phishingResponses = new List<string>
        {
            "🎣  PHISHING\n" +
            "───────────────────────────────────────────────────\n" +
            "Phishing is when attackers impersonate a trusted source to steal\n" +
            "your credentials or install malware.\n\n" +
            "Red flags to spot:\n" +
            "  ✗  Urgency — \"Act NOW or your account will be closed!\"\n" +
            "  ✗  Mismatched sender (support@paypa1.com vs paypal.com)\n" +
            "  ✗  Generic greeting — \"Dear Customer\" instead of your name\n" +
            "  ✗  Suspicious links — hover before you click!\n\n" +
            "What to do: Don't click. Report it. Delete it.\n" +
            "Navigate directly to the site by typing the URL yourself.",

            "🎣  SPEAR PHISHING\n" +
            "───────────────────────────────────────────────────\n" +
            "Spear phishing is a targeted phishing attack that uses personal\n" +
            "information about you to seem more convincing.\n\n" +
            "  • Attackers research you on social media first.\n" +
            "  • Emails may reference your name, employer, or recent events.\n" +
            "  • Even tech-savvy people fall for it.\n\n" +
            "Defence: Always verify unexpected requests through official channels.\n" +
            "Call the sender directly on a known number — never the one in the email.",

            "🎣  SMISHING & VISHING\n" +
            "───────────────────────────────────────────────────\n" +
            "Phishing isn't only email:\n\n" +
            "  • Smishing  — phishing via SMS (\"Your parcel is held, click here\")\n" +
            "  • Vishing   — phishing over a phone call (fake bank/SARS calls)\n\n" +
            "Golden rules:\n" +
            "  ✗  Never give OTPs or passwords over the phone.\n" +
            "  ✓  Hang up and call back on the official number."
        };

        private static List<string> passwordResponses = new List<string>
        {
            "🔑  PASSWORDS\n" +
            "───────────────────────────────────────────────────\n" +
            "A strong password is your first line of defence.\n\n" +
            "Creating strong passwords:\n" +
            "  • Minimum 12 characters — longer is always better.\n" +
            "  • Mix uppercase, lowercase, numbers, and symbols.\n" +
            "  • Use passphrases: \"CorrectHorseBatteryStaple!\" is strong AND memorable.\n\n" +
            "Critical rules:\n" +
            "  ✗  NEVER reuse passwords — one breach exposes everything.\n" +
            "  ✗  NEVER share passwords, not even with IT support.\n" +
            "  ✓  Use a password manager (Bitwarden, 1Password, KeePass).",

            "🔑  TWO-FACTOR AUTHENTICATION (2FA)\n" +
            "───────────────────────────────────────────────────\n" +
            "2FA adds a second verification step so a stolen password alone\n" +
            "is not enough to break into your account.\n\n" +
            "Types (safest first):\n" +
            "  1.  Hardware key (YubiKey)   — near-impossible to phish.\n" +
            "  2.  Authenticator app        — Google Auth, Microsoft Auth.\n" +
            "  3.  SMS OTP                  — better than nothing; SIM-swap risk.\n\n" +
            "Enable 2FA on email first — it unlocks every other account reset.",

            "🔑  PASSWORD MANAGERS\n" +
            "───────────────────────────────────────────────────\n" +
            "A password manager generates and stores a unique, complex\n" +
            "password for every site — you only remember one master passphrase.\n\n" +
            "Top options (all free tiers available):\n" +
            "  • Bitwarden  — open-source, audited, excellent free tier.\n" +
            "  • 1Password  — polished UI, family sharing.\n" +
            "  • KeePass    — fully offline, no cloud.\n\n" +
            "  ✓  Enable breach alerts on HaveIBeenPwned.com."
        };

        private static List<string> browsingResponses = new List<string>
        {
            "🌐  SAFE BROWSING\n" +
            "───────────────────────────────────────────────────\n" +
            "Before you click:\n" +
            "  ✓  Check for HTTPS (padlock icon) — HTTP exposes your data.\n" +
            "  ✓  Hover over links to preview the real destination URL.\n" +
            "  ✓  Spot typos in domain names (g00gle.com, paypa1.com).\n\n" +
            "While browsing:\n" +
            "  ✓  Keep your browser and all extensions updated.\n" +
            "  ✓  Use an ad-blocker — malvertising is real.\n" +
            "  ✗  Never ignore certificate warnings.\n\n" +
            "Downloads: only from official, verified sources.",

            "🌐  VPN & PUBLIC WI-FI\n" +
            "───────────────────────────────────────────────────\n" +
            "A VPN encrypts all traffic between your device and a remote server,\n" +
            "hiding it from anyone monitoring your network.\n\n" +
            "When to use a VPN:\n" +
            "  ✓  On public Wi-Fi (cafés, airports, hotels) — always.\n" +
            "  ✓  Accessing sensitive work systems remotely.\n\n" +
            "What a VPN does NOT do:\n" +
            "  ✗  Make you anonymous.\n" +
            "  ✗  Protect against malware already installed."
        };

        private static List<string> privacyResponses = new List<string>
        {
            "🔏  DATA PRIVACY\n" +
            "───────────────────────────────────────────────────\n" +
            "Your personal data is valuable — companies harvest it, criminals steal it.\n\n" +
            "Minimise your footprint:\n" +
            "  ✓  Share only what is strictly necessary on any form.\n" +
            "  ✓  Use a separate email for newsletters and low-trust signups.\n" +
            "  ✓  Audit which apps have access to your location, mic, and camera.\n\n" +
            "Know your rights:\n" +
            "  • POPIA (South Africa) gives you the right to access and delete your data.\n" +
            "  • You can request erasure from any company that holds your information.",

            "🔏  SOCIAL MEDIA PRIVACY\n" +
            "───────────────────────────────────────────────────\n" +
            "Social media oversharing fuels social engineering attacks.\n\n" +
            "  ✓  Set profiles to private — limit who sees your posts.\n" +
            "  ✓  Never post your location in real-time.\n" +
            "  ✓  Avoid sharing birthdays, ID numbers, or travel plans publicly.\n" +
            "  ✗  Beware of \"fun\" quizzes — they harvest your personal data."
        };

        private static List<string> malwareResponses = new List<string>
        {
            "🦠  MALWARE\n" +
            "───────────────────────────────────────────────────\n" +
            "Malware is any software designed to damage or gain unauthorised access.\n\n" +
            "Types to know:\n" +
            "  • Ransomware  — encrypts your files, demands payment.\n" +
            "  • Trojan      — disguised as legitimate software.\n" +
            "  • Keylogger   — records everything you type (including passwords).\n" +
            "  • Spyware     — silently monitors your activity.\n\n" +
            "Prevention:\n" +
            "  ✓  Keep OS and all software patched.\n" +
            "  ✓  Use reputable antivirus / EDR software.\n" +
            "  ✓  Follow the 3-2-1 backup rule.\n\n" +
            "If infected: disconnect from the internet immediately."
        };

        private static List<string> socialEngResponses = new List<string>
        {
            "🎭  SOCIAL ENGINEERING\n" +
            "───────────────────────────────────────────────────\n" +
            "Social engineering exploits human psychology — trust, urgency, fear —\n" +
            "rather than technical vulnerabilities.\n\n" +
            "Common tactics:\n" +
            "  • Pretexting  — attacker invents a scenario to gain your trust.\n" +
            "  • Baiting     — leaving infected USB drives in car parks.\n" +
            "  • Tailgating  — following authorised staff through secured doors.\n\n" +
            "Defence:\n" +
            "  ✓  Verify identities independently before acting on any request.\n" +
            "  ✓  Slow down — urgency is always a manipulation tactic.\n" +
            "  ✓  Call back on a known number, not one they gave you."
        };
    }
}


