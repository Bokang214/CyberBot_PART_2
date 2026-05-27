using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberBot_PART_2
{
    public partial class MainWindow : Window
    {
        private CyberBot cyberBot;

        public MainWindow()
        {
            InitializeComponent();
            cyberBot = new CyberBot();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Log session start
            EnhancedBotHandler.LogSessionBoundary(isStart: true);

            // Play voice greeting immediately when the app opens
            _ = Task.Run(() => PlayVoiceGreeting());

            // Show header ( art is already in XAML)
            await AddBotMessage("🛡️ Welcome to the Cybersecurity Awareness Bot!");
            await Task.Delay(200);
            await AddBotMessage("I'm here to help you stay safe online.");
            await Task.Delay(200);
            await AddBotMessage("What's your name?");
        }

      

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessUserInput();
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                await ProcessUserInput();
            }
        }

        private async Task ProcessUserInput()
        {
            string userInput = InputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userInput))
                return;

            // Add user message to chat
            AddUserMessage(userInput);
            InputTextBox.Clear();
            InputTextBox.Focus();

            // Show typing indicator
            ShowTypingIndicator(true);

            // Process with the full EnhancedBotHandler (NLP, sentiment, memory, quiz)
            string response = await Task.Run(() => EnhancedBotHandler.ProcessInput(userInput, cyberBot));

            ShowTypingIndicator(false);

            // Add bot response
            await AddBotMessage(response);

            // Auto-scroll
            await Task.Delay(50);
            ChatScrollViewer.ScrollToBottom();
        }

        // ── ORIGINAL AddUserMessage  ──────────────────────────
        private void AddUserMessage(string message)
        {
            Border border = new Border { Style = (Style)FindResource("UserBubble") };
            TextBlock text = new TextBlock
            {
                Text = $"👤 {message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14
            };
            border.Child = text;
            ChatPanel.Children.Add(border);
        }

        // ── ORIGINAL AddBotMessage, typing effect preserved ──
        private async Task AddBotMessage(string message)
        {
            Border border = new Border { Style = (Style)FindResource("BotBubble") };
            TextBlock text = new TextBlock
            {
                Text = "🤖 ",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
            };
            border.Child = text;
            ChatPanel.Children.Add(border);
            ChatScrollViewer.ScrollToBottom();

            // ORIGINAL typing effect 
            string fullMessage = message;
            for (int i = 0; i <= fullMessage.Length; i++)
            {
                text.Text = $"🤖 {fullMessage.Substring(0, i)}";
                await Task.Delay(6); 
                ChatScrollViewer.ScrollToBottom();
            }

            //After typing completes, apply colour formatting
            ApplyColourFormatting(text, fullMessage);
            ChatScrollViewer.ScrollToBottom();

            // Show quiz panel if a quiz was just started
            UpdateQuizPanel();
        }

        //
        private void PlayVoiceGreeting()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.wav");
                if (System.IO.File.Exists(path))
                {
                    using (SoundPlayer player = new SoundPlayer(path))
                    {
                        player.PlaySync();
                    }
                }
            }
            catch
            {
                // Silent fail - voice not critical
            }
        }

        private async void VoiceButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() => PlayVoiceGreeting());
            await AddBotMessage("🔊 Voice greeting played!");
        }

        //
        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            QuizPanelBorder.Visibility = Visibility.Collapsed;
            await AddBotMessage("✨ Chat cleared! I'm still here — ready to help with cybersecurity. Type 'help' to see what I can do.");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Quiz panel — shown inline below the chat during a quiz
        // ═══════════════════════════════════════════════════════════════════

        private void UpdateQuizPanel()
        {
            if (!EnhancedBotHandler.QuizActive)
            {
                QuizPanelBorder.Visibility = Visibility.Collapsed;
                return;
            }

            // Build the quiz panel from the current quiz state
            // The question text and options are re-rendered from the bot's last response
            // so the user can also click instead of typing
            QuizPanelBorder.Visibility = Visibility.Visible;

            // We rebuild the options each time UpdateQuizPanel is called during a quiz
            // Parse current question from the quiz pool via a fresh peek
            // (EnhancedBotHandler exposes what it needs; we call GetCurrentQuestion)
            RenderQuizButtons();
        }

        private void RenderQuizButtons()
        {
            QuizOptionsPanel.Children.Clear();

            // The 4 answer buttons
            string[] labels = { "1", "2", "3", "4" };
            foreach (string label in labels)
            {
                Button btn = new Button
                {
                    Content = $"  {label}",
                    Style = (Style)FindResource("QuizButton"),
                    Tag = label,
                };
                btn.Click += QuizOptionButton_Click;
                QuizOptionsPanel.Children.Add(btn);
            }

            QuizProgressText.Text = $"Quiz in progress — type 1, 2, 3, or 4 (or click above)";
            QuizScoreText.Text = string.Empty;
        }

        private async void QuizOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                InputTextBox.Text = tag;
                await ProcessUserInput();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Colour formatting — applies colour to key lines in bot messages — applies colour to key lines in bot messages
        // (same effect as the console colour coding from 
        // ═══════════════════════════════════════════════════════════════════

        private static void ApplyColourFormatting(TextBlock tb, string fullText)
        {
            tb.Text = string.Empty;
            tb.Inlines.Clear();

            foreach (string rawLine in fullText.Split('\n'))
            {
                System.Windows.Documents.Run run = new System.Windows.Documents.Run(rawLine)
                {
                    Foreground = PickLineColour(rawLine)
                };
                tb.Inlines.Add(run);
                tb.Inlines.Add(new System.Windows.Documents.LineBreak());
            }
        }

        private static Brush PickLineColour(string line)
        {
            // Header lines with emoji topic markers
            if (line.StartsWith("🎣") || line.StartsWith("🔑") || line.StartsWith("🌐") ||
                line.StartsWith("🛡️") || line.StartsWith("🔒") || line.StartsWith("🎭") ||
                line.StartsWith("🦠") || line.StartsWith("🔏") || line.StartsWith("🚨") ||
                line.StartsWith("📝") || line.StartsWith("📜") || line.StartsWith("⭐") ||
                line.StartsWith("🤖"))
                return new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));   // blue

            if (line.TrimStart().StartsWith("✓") || line.StartsWith("✅"))
                return new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));   // green

            if (line.TrimStart().StartsWith("✗") || line.StartsWith("❌"))
                return new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));   // red

            if (line.TrimStart().StartsWith("•"))
                return new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xCC));   // cyan

            if (line.TrimStart().StartsWith("💡"))
                return new SolidColorBrush(Color.FromRgb(0xF0, 0x88, 0x3E));   // orange

            if (line.StartsWith("─") || line.StartsWith("═"))
                return new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D));   // dim

            if (line.TrimStart().StartsWith("1.") || line.TrimStart().StartsWith("2.") ||
                line.TrimStart().StartsWith("3.") || line.TrimStart().StartsWith("4.") ||
                line.TrimStart().StartsWith("5.") || line.TrimStart().StartsWith("6.") ||
                line.TrimStart().StartsWith("7."))
                return new SolidColorBrush(Color.FromRgb(0xF0, 0x88, 0x3E));   // orange for numbered steps

            return new SolidColorBrush(Color.FromRgb(0xC9, 0xD1, 0xD9));       // default text
        }

        // ═══════════════════════════════════════════════════════════════════
        // Typing indicator (shows "Bot is typing…" during processing)
        // ═══════════════════════════════════════════════════════════════════

        private Border _typingBubble;

        private void ShowTypingIndicator(bool show)
        {
            if (show)
            {
                _typingBubble = new Border { Style = (Style)FindResource("BotBubble") };
                TextBlock tb = new TextBlock
                {
                    Text = "🤖  typing…",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                };
                _typingBubble.Child = tb;
                ChatPanel.Children.Add(_typingBubble);
                ChatScrollViewer.ScrollToBottom();
            }
            else
            {
                if (_typingBubble != null && ChatPanel.Children.Contains(_typingBubble))
                    ChatPanel.Children.Remove(_typingBubble);
                _typingBubble = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Window close — log session end
        // ═══════════════════════════════════════════════════════════════════
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            EnhancedBotHandler.LogSessionBoundary(isStart: false);
            base.OnClosing(e);
        }
    }
}
