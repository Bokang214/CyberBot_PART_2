# CyberBot_PART_2

# 🛡️ CyberBot — Cybersecurity Awareness Bot (Part 2)

A WPF desktop chatbot that educates users on cybersecurity topics through
natural language conversation, sentiment-aware responses, a quiz feature,
session memory, and a full activity log.

---


## Overview

CyberBot Part 2 builds on the original console-based Part 1 bot by migrating
to a full WPF GUI while preserving all original logic from `CyberBot.cs`.
The bot accepts free-text input, detects the user's emotional tone, remembers
what topics have been discussed, and guides the user toward safer online behaviour.

---

## Features

| Feature | Description |
|---|---|
| **Natural language input** | Type freely — no menu numbers required |
| **Sentiment detection** | Detects worried, frustrated, or neutral tone and adjusts responses |
| **Session memory** | Tracks topics visited and how many times each was asked |
| **Multiple responses per topic** | Randomly selects from a pool of responses per topic so repeat questions feel fresh |
| **Follow-up detection** | Typing "more" or "tell me more" returns additional content on the last topic |
| **Cybersecurity quiz** | 10-question bank, 5 drawn at random per round with explanations |
| **Chat history viewer** | Type `history` to review the last 15 messages |
| **Interests & stats** | Type `interests` to see topics explored and quiz performance |
| **Activity log** | Every conversation turn is written to `cyberbot_activity.log` |
| **Typing effect** | Bot responses stream character-by-character |
| **Voice greeting** | Plays `welcome.wav` automatically on launch |
| **Colour-coded output** | Green for safe actions, red for warnings, cyan for bullet points |

---

## Project Structure

```
CyberBot_PART_2/
│
├── App.xaml                  # Application entry point and global resources
├── App.xaml.cs               # App code-behind
│
├── CyberBot.cs               # Original Part 1 class — preserved exactly
│                             # Contains: PrintBanner, PlayVoiceGreeting,
│                             #           GetUserName, WelcomeUser, TypeEffect
│
├── EnhancedBotHandler.cs     # All Part 2 logic
│                             # Contains: NLP keyword dispatch, sentiment analysis,
│                             #           session memory, conversation history,
│                             #           quiz engine, activity logger
│
├── MainWindow.xaml           # WPF UI layout — header, chat area, quiz panel, input bar
├── MainWindow.xaml.cs        # UI code-behind — bubble rendering, typing animation,
│                             #                  quiz panel, event handlers
│
└── CyberBot_PART_2.csproj    # Project file — targets net8.0-windows, UseWPF=true
```

---

## Getting Started

### Prerequisites

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with the **.NET desktop development** workload)

### Running the project

1. Clone or download the repository.
2. Open `CyberBot_PART_2.csproj` in Visual Studio 2022.
3. Press **F5** to build and run.

### Voice greeting (optional)

Place a file named `welcome.wav` in the same folder as the built `.exe`
(typically `bin\Debug\net8.0-windows\`). The bot will play it automatically
on startup. If the file is missing, the app starts silently — no error.

### GitHub Actions (CI)

If building via GitHub Actions, ensure your workflow uses a Windows runner
and the project file includes `<EnableWindowsTargeting>true</EnableWindowsTargeting>`:

```yaml
runs-on: windows-latest
```

```xml
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

---

## How to Use

1. **Enter your name** when prompted — the bot greets you and shows available topics.
2. **Type naturally** — ask about any cybersecurity topic in plain English.
3. **Use keywords** to jump straight to a topic (see table below).
4. **Special commands:**

| Command | Action |
|---|---|
| `quiz` | Start a 5-question cybersecurity quiz |
| `history` | View the last 15 messages |
| `interests` | See topics you've explored and your quiz score |
| `help` | Show all available topics and commands |
| `clear` | Clear the chat window |

---

## Topics Covered

| Keyword | Topic |
|---|---|
| `phishing` | Phishing, spear phishing, vishing, smishing |
| `password` | Password strength, passphrases, password managers, 2FA |
| `browsing` | HTTPS, safe browsing habits, VPNs, public Wi-Fi |
| `privacy` | Data privacy, POPIA/GDPR, social media exposure |
| `malware` | Ransomware, trojans, keyloggers, spyware, rootkits |
| `social` | Social engineering, pretexting, baiting, tailgating |
| `hacked` | Incident response — step-by-step recovery guide |

You can also ask naturally — for example:
- *"What is phishing?"*
- *"How do I make a strong password?"*
- *"I think my account was hacked"*

---

## Quiz

- 10 questions in the bank covering all major topics.
- 5 questions are drawn at random each round.
- After each answer, the correct answer and an explanation are shown.
- Final score is graded and added to your session total.
- Type `quiz` or click **Take Quiz** to begin.

---

## Requirements

- **Framework:** .NET 8, Windows only (`net8.0-windows`)
- **UI:** WPF (Windows Presentation Foundation)
- **No external NuGet packages** — all dependencies are part of the .NET 8 SDK
