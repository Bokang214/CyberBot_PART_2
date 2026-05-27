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
        //  Quiz question bank (10 questions, 5 drawn per round)
        private static readonly List<QuizQuestion> QuizBank = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                Question     = "What does HTTPS stand for?",
                Options      = new[] { "HyperText Transfer Protocol Secure", "High Traffic Transmission Protocol Standard", "HyperText Transport Protocol System", "Hyper Transfer Technology Protocol Secure" },
                CorrectIndex = 0,
                Explanation  = "HTTPS = HTTP + TLS encryption. The 'S' means data between your browser and the server is encrypted in transit."
            },
            new QuizQuestion
            {
                Question     = "Which form of 2FA is considered the SAFEST?",
                Options      = new[] { "SMS one-time password", "Email verification code", "Hardware security key (e.g. YubiKey)", "Security question" },
                CorrectIndex = 2,
                Explanation  = "Hardware keys are phishing-resistant — they verify the actual domain, so even a perfect-looking fake site can't capture the factor."
            },
            new QuizQuestion
            {
                Question     = "You receive an urgent email from your bank asking you to click a link. What should you do?",
                Options      = new[] { "Click the link quickly", "Forward it to all contacts to warn them", "Open your browser and go directly to the bank's website", "Reply asking for more details" },
                CorrectIndex = 2,
                Explanation  = "Never follow links in emails for sensitive actions. Always navigate directly to the site yourself."
            },
            new QuizQuestion
            {
                Question     = "What is a passphrase and why is it stronger?",
                Options      = new[] { "A shorter password that's easy to remember", "A sequence of random words — long yet memorable", "A password using only lowercase letters", "A password stored in plain text" },
                CorrectIndex = 1,
                Explanation  = "Length beats complexity. 'CorrectHorseBatteryStaple!' has 28 characters and takes centuries to brute-force."
            },
            new QuizQuestion
            {
                Question     = "What does a VPN primarily protect on a public Wi-Fi network?",
                Options      = new[] { "Viruses on your device", "Websites knowing your real identity", "Eavesdroppers monitoring unencrypted local network traffic", "All of the above equally" },
                CorrectIndex = 2,
                Explanation  = "A VPN encrypts the tunnel to the VPN server, preventing anyone on the local network from reading your traffic."
            },
            new QuizQuestion
            {
                Question     = "You find a USB drive in the car park. What should you do?",
                Options      = new[] { "Plug it in to find the owner", "Hand it to IT/security without plugging it in", "Plug it into an old computer", "Format and reuse it" },
                CorrectIndex = 1,
                Explanation  = "USB baiting is a real social engineering tactic. An infected drive can execute malware the moment it's inserted."
            },
            new QuizQuestion
            {
                Question     = "Which of the following is a classic sign of a phishing email?",
                Options      = new[] { "It comes from your manager's real email address", "It uses your full legal name in the greeting", "It creates a sense of urgency and asks you to click immediately", "It was sent during business hours" },
                CorrectIndex = 2,
                Explanation  = "Urgency is the #1 psychological trigger in phishing. Legitimate organisations rarely demand instant action under threat of consequences."
            },
            new QuizQuestion
            {
                Question     = "What is the 3-2-1 backup rule?",
                Options      = new[] { "Back up every 3 days, keep 2 copies, 1 offsite", "3 copies of data, on 2 different media types, with 1 copy offsite", "3 users approve, 2 verify, 1 stores", "Back up 3 GB every 2 hours to 1 cloud provider" },
                CorrectIndex = 1,
                Explanation  = "3 copies on 2 different storage types with 1 offsite — protects against hardware failure, theft, fire, and ransomware."
            },
            new QuizQuestion
            {
                Question     = "Your work computer may be infected with malware. What is your FIRST action?",
                Options      = new[] { "Run antivirus and continue working", "Email IT from the same machine", "Disconnect from the network immediately", "Restart the computer" },
                CorrectIndex = 2,
                Explanation  = "Disconnecting cuts off the malware from its command-and-control server, preventing data exfiltration or network spread."
            },
            new QuizQuestion
            {
                Question     = "What does 'social engineering' mean in cybersecurity?",
                Options      = new[] { "Using social media to share security tips", "Manipulating people psychologically to reveal information or take unsafe actions", "Building a social network of security professionals", "Engineering software for social platforms" },
                CorrectIndex = 1,
                Explanation  = "Social engineering targets the human element — trust, authority, fear, and urgency — rather than software vulnerabilities."
            }
        };

        // ══════════════════════════════════════════════════════════════════
        // Sentiment analysis
        // ══════════════════════════════════════════════════════════════════
        public static Sentiment DetectSentiment(string input)
        {
            string lower = input.ToLowerInvariant();

            string[] worriedKeywords = { "scared", "afraid", "worried", "terrified", "anxious", "nervous", "panic", "hacked", "breached", "stolen", "compromised", "attacked", "danger", "unsafe", "emergency", "urgent help" };
            string[] frustratedKeywords = { "annoying", "useless", "stupid", "boring", "confused", "confusing", "complicated", "don't understand", "makes no sense", "ridiculous", "waste of time", "terrible", "awful", "hate", "frustrated", "annoyed" };
            string[] curiousKeywords = { "curious", "interesting", "wonder", "tell me", "explain", "how does", "why does", "what is", "how does", "learn more" };

            if (worriedKeywords.Any(k => lower.Contains(k))) return Sentiment.Worried;
            if (frustratedKeywords.Any(k => lower.Contains(k))) return Sentiment.Frustrated;
            if (curiousKeywords.Any(k => lower.Contains(k))) return Sentiment.Curious;
            return Sentiment.Neutral;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Activity logger
        // ══════════════════════════════════════════════════════════════════
        public static void LogActivity(string role, string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string name = string.IsNullOrEmpty(userName) ? "Unknown" : userName;
                string flat = message.Replace("\n", " ").Replace("\r", "");
                File.AppendAllText(LogPath,
                    $"[{timestamp}] [{name}] {role}: {flat}{Environment.NewLine}");
            }
            catch { /* logging must never crash the app */ }
        }

        public static void LogSessionBoundary(bool isStart)
        {
            try
            {
                string label = isStart ? "SESSION START" : "SESSION END";
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===== {label} ====={Environment.NewLine}");
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        // Record conversation history
        // ══════════════════════════════════════════════════════════════════
        public static void RecordHistory(string role, string message)
        {
            ConversationHistory.Add(new ConversationEntry
            {
                Role = role,
                Message = message,
                Time = DateTime.Now
            });

            // Cap at 200 entries
            if (ConversationHistory.Count > 200)
                ConversationHistory.RemoveAt(0);

            LogActivity(role, message);
        }

        // ══════════════════════════════════════════════════════════════════
        // Track topic interest count
        // ══════════════════════════════════════════════════════════════════
        private static void TrackTopic(string topic)
        {
            topicsVisited.Add(topic);
            if (topicAskCount.ContainsKey(topic))
                topicAskCount[topic]++;
            else
                topicAskCount[topic] = 1;
            lastTopic = topic;
        }

        // ══════════════════════════════════════════════════════════════════
        // QUIZ — public entry points
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Begins a new quiz round; returns the first question text.</summary>
        public static string StartQuiz()
        {
            // Shuffle and take 5 questions
            quizPool = QuizBank.OrderBy(_ => rand.Next()).Take(5).ToList();
            quizIndex = 0;
            quizScore = 0;
            QuizActive = true;
            return BuildQuizQuestion();
        }

        /// <summary>Submits an answer (1-4) and returns result + next question or final score.</summary>
        public static string SubmitQuizAnswer(int answerNumber)
        {
            if (!QuizActive || quizPool == null || quizIndex >= quizPool.Count)
                return "No quiz is active. Type 'quiz' to start one!";

            QuizQuestion current = quizPool[quizIndex];
            bool correct = (answerNumber - 1) == current.CorrectIndex;
            if (correct) quizScore++;

            string result = correct
                ? $"✅  Correct!\n💡  {current.Explanation}"
                : $"❌  Not quite. The correct answer was:\n    {current.Options[current.CorrectIndex]}\n💡  {current.Explanation}";

            quizIndex++;

            if (quizIndex >= quizPool.Count)
            {
                // Quiz finished
                QuizActive = false;
                QuizCorrect += quizScore;
                QuizTotal += quizPool.Count;

                double pct = (double)quizScore / quizPool.Count;
                string grade = pct == 1.0 ? "🏆  Perfect score! Outstanding!" :
                               pct >= 0.8 ? "🥇  Excellent — you really know your stuff!" :
                               pct >= 0.6 ? "👍  Good — a little more practice and you'll ace it!" :
                               pct >= 0.4 ? "📚  Fair — keep studying, you're getting there!" :
                                             "🔁  Keep learning — every expert started somewhere!";

                return $"{result}\n\n" +
                       $"══ QUIZ COMPLETE ══\n" +
                       $"Score: {quizScore} / {quizPool.Count}\n" +
                       $"{grade}\n\n" +
                       $"Session total: {QuizCorrect} / {QuizTotal} correct.\n\n" +
                       $"Type 'quiz' to try another round!";
            }

            return result + "\n\n" + BuildQuizQuestion();
        }

        private static string BuildQuizQuestion()
        {
            QuizQuestion q = quizPool[quizIndex];
            return
                $"📝  Question {quizIndex + 1} of {quizPool.Count}\n" +
                $"───────────────────────────────────────────────────\n" +
                $"{q.Question}\n\n" +
                $"  1.  {q.Options[0]}\n" +
                $"  2.  {q.Options[1]}\n" +
                $"  3.  {q.Options[2]}\n" +
                $"  4.  {q.Options[3]}\n\n" +
                $"Type 1, 2, 3, or 4 to answer.";
        }

        // ══════════════════════════════════════════════════════════════════
        //  Conversation history view
        // ══════════════════════════════════════════════════════════════════
        public static string GetHistorySummary(int maxEntries = 15)
        {
            if (ConversationHistory.Count == 0)
                return "📜  No history yet — start chatting first!";

            var recent = ConversationHistory.TakeLast(maxEntries).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📜  CHAT HISTORY (last 15 messages)");
            sb.AppendLine("───────────────────────────────────────────────────");
            foreach (var entry in recent)
            {
                string label = entry.Role == "User" ? "You" : "Bot";
                string preview = entry.Message.Replace("\n", " ");
                if (preview.Length > 90) preview = preview[..90] + "…";
                sb.AppendLine($"  [{entry.Time:HH:mm}]  {label}: {preview}");
            }
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════
        // Interests / stats summary
        // ══════════════════════════════════════════════════════════════════
        public static string GetInterestsSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"⭐  {(string.IsNullOrEmpty(userName) ? "Your" : userName + "'s")} INTERESTS & STATS");
            sb.AppendLine("───────────────────────────────────────────────────");

            if (topicsVisited.Count == 0)
            {
                sb.AppendLine("  You haven't explored any topics yet.");
            }
            else
            {
                sb.AppendLine("  Topics explored:");
                foreach (string t in topicsVisited)
                {
                    int count = topicAskCount.TryGetValue(t, out int c) ? c : 0;
                    sb.AppendLine($"    ★  {t}  (asked {count}×)");
                }
            }

            sb.AppendLine();
            if (QuizTotal > 0)
            {
                double pct = Math.Round((double)QuizCorrect / QuizTotal * 100, 1);
                sb.AppendLine($"  Quiz:  {QuizCorrect} / {QuizTotal} correct  ({pct}%)");
            }
            else
            {
                sb.AppendLine("  Quiz:  Not taken yet — type 'quiz' to try one!");
            }

            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════
        // MAIN ENTRY POINT — called by MainWindow for every user message
        // ══════════════════════════════════════════════════════════════════
        public static string ProcessInput(string input, CyberBot bot)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string lower = input.ToLower().Trim();

            // ── 1. QUIZ MODE ─────────────────────────────────────────────
            if (QuizActive)
            {
                if (int.TryParse(lower, out int n) && n >= 1 && n <= 4)
                    return SubmitQuizAnswer(n);
                return "⚠️  Please type 1, 2, 3, or 4 to answer the current question.\n\n" +
                       BuildQuizQuestion();
            }

            // ── 2. NAME COLLECTION ───────────────────────────────────────
            if (string.IsNullOrEmpty(userName))
            {
                userName = input.Trim();
                bot.UserName = userName;
                LogSessionBoundary(isStart: true);
                RecordHistory("User", input);

                string welcome =
                    $"Nice to meet you, {userName}! 😊\n\n" +
                    "I can help you with:\n\n" +
                    "    Phishing              —  type 'phishing'\n" +
                    "    Passwords & 2FA       —  type 'password'\n" +
                    "    Safe Browsing / VPN   —  type 'browsing'\n" +
                    "    Privacy               —  type 'privacy'\n" +
                    "    Malware               —  type 'malware'\n" +
                    "    Social Engineering    —  type 'social'\n" +
                    "    Quiz                  —  type 'quiz'\n" +
                    "    Chat History          —  type 'history'\n" +
                    "    My Interests          —  type 'interests'\n\n" +
                    "Or just ask me anything in plain English!";

                RecordHistory("Bot", welcome);
                return welcome;
            }

            // Record user turn
            RecordHistory("User", input);

            // ── 3. SPECIAL COMMANDS ──────────────────────────────────────

            if (lower is "quiz" or "start quiz" or "take quiz")
                return RecordAndReturn(StartQuiz());

            if (lower is "history" or "chat history" or "h")
                return RecordAndReturn(GetHistorySummary());

            if (lower is "interests" or "stats" or "my interests" or "i")
                return RecordAndReturn(GetInterestsSummary());

            if (lower is "help" or "menu" or "?" or "options")
                return RecordAndReturn(GetHelpMenu());

            // ── 4. SENTIMENT DETECTION ───────────────────────────────────
            //  respond to emotional state first
            Sentiment sentiment = DetectSentiment(lower);

            string sentimentPrefix = sentiment switch
            {
                Sentiment.Worried =>
                    $"😟  I can hear that you're concerned, {userName} — that's completely understandable.\n" +
                    "Let me give you clear, actionable advice to help you stay protected.\n\n",
                Sentiment.Frustrated =>
                    $"😤  I hear you, {userName} — let me explain this as simply as I can.\n\n",
                Sentiment.Curious =>
                    $"🤔  Great question! Here's everything you need to know.\n\n",
                _ => string.Empty
            };

            // ── 5. KEYWORD / NLP TOPIC MATCHING ─────────────────────────
            // keyword recognition with multiple responses

            // Repeated-question detection: hint toward quiz
            string repeatNote = string.Empty;
            if (!string.IsNullOrEmpty(lastTopic) &&
                topicAskCount.TryGetValue(lastTopic, out int askCount) && askCount >= 2)
            {
                repeatNote = $"\n\n💡  You've asked about {lastTopic} {askCount} times — " +
                             "consider taking the quiz to test what you know! (type 'quiz')";
            }

            // Follow-up / memory recall
            if (lower.Contains("more") || lower.Contains("another tip") ||
                lower.Contains("tell me more") || lower.Contains("explain more"))
            {
                if (!string.IsNullOrEmpty(lastTopic))
                    return RecordAndReturn(sentimentPrefix + GetMoreOnTopic(lastTopic) + repeatNote);
            }

            // General questions (preserved from original Part 1)
            if (lower.Contains("how are you"))
                return RecordAndReturn($"🤖  I'm running perfectly — all systems secure! Thanks for asking, {userName}. 😊\nHow can I help you stay safe online today?");

            if (lower.Contains("what can i ask") || lower.Contains("what can you do"))
                return RecordAndReturn(GetHelpMenu());

            if (lower.Contains("purpose") || lower.Contains("what are you") || lower.Contains("who are you"))
                return RecordAndReturn($"🛡️  My purpose is to educate you about cybersecurity and help you — {userName} — stay safe online.\n\nI cover phishing, passwords, safe browsing, malware, social engineering, privacy, and more. I can also quiz you!");

            // Phishing
            if (lower.Contains("phish") || lower.Contains("scam") || lower.Contains("spam") ||
                lower.Contains("fake email") || lower.Contains("vishing") || lower.Contains("smishing"))
            {
                TrackTopic("phishing");
                return RecordAndReturn(sentimentPrefix + phishingResponses[rand.Next(phishingResponses.Count)] + repeatNote);
            }

            // Passwords / 2FA
            if (lower.Contains("password") || lower.Contains("pass") || lower.Contains("2fa") ||
                lower.Contains("two factor") || lower.Contains("credential") || lower.Contains("login"))
            {
                TrackTopic("passwords");
                return RecordAndReturn(sentimentPrefix + passwordResponses[rand.Next(passwordResponses.Count)] + repeatNote);
            }

            // Safe browsing / VPN
            if (lower.Contains("brows") || lower.Contains("https") || lower.Contains("vpn") ||
                lower.Contains("wifi") || lower.Contains("internet") || lower.Contains("website") ||
                lower.Contains("link") || lower.Contains("url"))
            {
                TrackTopic("safe browsing");
                return RecordAndReturn(sentimentPrefix + browsingResponses[rand.Next(browsingResponses.Count)] + repeatNote);
            }

            // Privacy
            if (lower.Contains("privacy") || lower.Contains("data") || lower.Contains("personal") ||
                lower.Contains("tracking") || lower.Contains("gdpr") || lower.Contains("popia") ||
                lower.Contains("social media") || lower.Contains("information"))
            {
                TrackTopic("privacy");
                return RecordAndReturn(sentimentPrefix + privacyResponses[rand.Next(privacyResponses.Count)] + repeatNote);
            }

            // Malware / viruses
            if (lower.Contains("malware") || lower.Contains("virus") || lower.Contains("ransomware") ||
                lower.Contains("trojan") || lower.Contains("spyware") || lower.Contains("infected") ||
                lower.Contains("antivirus"))
            {
                TrackTopic("malware");
                return RecordAndReturn(sentimentPrefix + malwareResponses[rand.Next(malwareResponses.Count)] + repeatNote);
            }

            // Social engineering
            if (lower.Contains("social engineer") || lower.Contains("pretex") || lower.Contains("baiting") ||
                lower.Contains("tailgat") || lower.Contains("manipulat") || lower.Contains("usb"))
            {
                TrackTopic("social engineering");
                return RecordAndReturn(sentimentPrefix + socialEngResponses[rand.Next(socialEngResponses.Count)] + repeatNote);
            }

            // Incident response
            if (lower.Contains("hacked") || lower.Contains("breach") || lower.Contains("compromised") ||
                lower.Contains("stolen account") || lower.Contains("what do i do"))
            {
                TrackTopic("incident response");
                return RecordAndReturn(sentimentPrefix + GetIncidentResponse() + repeatNote);
            }

            // ── 6. FALLBACK ──────────────────────────────────────────────
            //  graceful unknown input handling
            string topicHint = topicsVisited.Count > 0
                ? $"\n\nYou've been interested in: {string.Join(", ", topicsVisited)}.\nFeel free to dive deeper on any of those!"
                : string.Empty;

            string[] fallbacks = {
                $"🤔  I didn't quite catch that, {userName}. Could you rephrase?\n\nTry: 'Tell me about phishing', 'What is 2FA?', or 'quiz'.{topicHint}",
                $"❓  Not sure what you mean, {userName}. I'm best at cybersecurity topics.\n\nType 'help' to see what I can do.{topicHint}",
                $"🔍  I couldn't find a match for that. Try asking about:\n  phishing, passwords, browsing, malware, privacy, or social engineering.{topicHint}"
            };
            return RecordAndReturn(fallbacks[rand.Next(fallbacks.Length)]);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static string RecordAndReturn(string response)
        {
            RecordHistory("Bot", response);
            return response;
        }

        private static string GetMoreOnTopic(string topic) => topic switch
        {
            "phishing" => phishingResponses[rand.Next(phishingResponses.Count)],
            "passwords" => passwordResponses[rand.Next(passwordResponses.Count)],
            "safe browsing" => browsingResponses[rand.Next(browsingResponses.Count)],
            "privacy" => privacyResponses[rand.Next(privacyResponses.Count)],
            "malware" => malwareResponses[rand.Next(malwareResponses.Count)],
            "social engineering" => socialEngResponses[rand.Next(socialEngResponses.Count)],
            _ => "What specific topic would you like me to expand on?"
        };

        private static string GetIncidentResponse() =>
            "🚨  INCIDENT RESPONSE — STAY CALM, ACT FAST\n" +
            "───────────────────────────────────────────────────\n" +
            "If you think you've been hacked:\n\n" +
            "  1.  Change your passwords — start with email, then financial accounts.\n" +
            "  2.  Enable 2FA everywhere it isn't already on.\n" +
            "  3.  Check for and log out all unknown active sessions.\n" +
            "  4.  Notify your bank if financial data was involved.\n" +
            "  5.  Scan your device with up-to-date antivirus.\n" +
            "  6.  Report to your national cybercrime unit.\n" +
            "  7.  Check HaveIBeenPwned.com for known breaches on your email.\n\n" +
            "Speed matters — attackers move fast once inside. Don't delay.";

        private static string GetHelpMenu() =>
            $"📋  What I can help you with, {userName}:\n\n" +
            "  🎣  Phishing & scams      —  type 'phishing'\n" +
            "  🔑  Passwords & 2FA       —  type 'password'\n" +
            "  🌐  Safe Browsing / VPN   —  type 'browsing'\n" +
            "  🔏  Data Privacy          —  type 'privacy'\n" +
            "  🦠  Malware & viruses     —  type 'malware'\n" +
            "  🎭  Social Engineering    —  type 'social'\n" +
            "  🚨  Incident Response     —  type 'hacked'\n" +
            "  📝  Take the Quiz         —  type 'quiz'\n" +
            "  📜  Chat History          —  type 'history'\n" +
            "  ⭐  My Interests & Stats  —  type 'interests'\n\n" +
            "Or just ask me anything naturally — I understand plain English!";
    }
}

