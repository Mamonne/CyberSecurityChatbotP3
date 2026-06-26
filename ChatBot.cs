using System;
using System.Collections.Generic;
using System.Media;
using System.IO;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  ChatBot.cs  —  Core logic for the CyberBot assistant
    //  Handles: keyword recognition, sentiment detection,
    //           memory/recall, conversation flow, personalisation
    // ============================================================
    public class ChatBot
    {
        // ── User memory ─────────────────────────────────────────
        public string UserName { get; set; } = "";
        public string UserInterest { get; set; } = "";

        // Stores ALL topics the user has shown interest in over the session
        public List<string> AllInterests { get; set; } = new List<string>();

        // Tracks the last topic discussed so follow-ups work correctly
        public string LastTopic { get; set; } = "";

        // Tracks the last emotion the user expressed
        public string LastEmotion { get; set; } = "";

        // Full conversation history — each entry is "Speaker: message"
        public List<string> ConversationHistory { get; set; } = new List<string>();

        // Counts how many times the user has asked about each topic
        private Dictionary<string, int> topicFrequency = new Dictionary<string, int>();

        // Used to pick random responses
        private Random random = new Random();

        // ── Single keyword responses ─────────────────────────────
        // Topics that have one definitive response each
        private Dictionary<string, string> keywordResponses = new Dictionary<string, string>()
        {
            { "about",              "I'm CyberBot — your friendly cybersecurity sidekick, here to keep you safe online!" },
            { "purpose",            "My job is to teach you how to avoid phishing, create strong passwords, browse safely, and protect your privacy." },
            { "vpn",                "A VPN (Virtual Private Network) encrypts your internet connection and hides your IP address — great for public WiFi and protecting your privacy." },
            { "backup",             "Always back up your data using the 3-2-1 rule: 3 copies, 2 different storage types, 1 stored offsite or in the cloud." },
            { "social engineering", "Social engineering tricks people into giving away sensitive info by exploiting trust. Always verify who you're communicating with." },
            { "identity theft",     "Identity theft happens when criminals steal your personal details to impersonate you. Guard your ID number, banking info, and passwords carefully." },
            { "cyberbullying",      "Cyberbullying is harmful online behaviour. Block and report the person, save evidence, and speak to a trusted adult or authority." },
            { "dark web",           "The dark web is a hidden part of the internet often used for illegal activity. Avoid it — your personal data can be bought and sold there." },
            { "cookie",             "Cookies track your browsing habits. Clear them regularly and be careful when accepting 'all cookies' on unfamiliar websites." },
            { "antivirus",          "Antivirus software scans for and removes threats. Keep it updated — new malware appears every single day." },
            { "firewall",           "A firewall acts like a security guard at your network door — blocking suspicious traffic before it can reach your device." },
            { "hacker",             "Hackers try to break into systems. Ethical hackers (white hats) help find vulnerabilities; criminal hackers (black hats) exploit them." },
            { "virus",              "A virus is malicious code that attaches to files and spreads — it can delete data, slow your system, or open backdoors for attackers." },
            { "malware",            "Malware is any software designed to harm your device or steal your data. It includes viruses, spyware, ransomware, and trojans." },
            { "ransomware",         "Ransomware encrypts your files and demands payment. Prevention: keep backups, avoid suspicious links, and keep all software updated." },
            { "encryption",         "Encryption converts data into unreadable code — only someone with the correct key can decrypt it. Look for 'https://' and the padlock icon." },
            { "2fa",                "Two-factor authentication (2FA) adds a second verification step — even if your password is stolen, attackers can't access your account without it." },
            { "wifi",               "Public WiFi is a hotspot for hackers. Avoid logging into banking or email on open networks — use a VPN if you must connect." },
            { "update",             "Software updates patch security vulnerabilities. Outdated software is one of the top ways hackers gain access — always update promptly." },
            { "scam",               "Scammers use fake calls, emails, and websites to steal money or data. If something feels urgent or too good to be true — stop and verify." },
            { "safe browsing",      "Check for 'https://' before entering personal info. Use a reputable browser with built-in phishing protection like Chrome or Firefox." },
            { "cybersecurity",      "Cybersecurity protects computers, networks, and data from digital attacks — from personal password safety to national infrastructure defence." }
        };

        // ── Multi-response topics (randomly selected each time) ──
        // These topics have enough depth to warrant varied responses
        private Dictionary<string, List<string>> randomResponses = new Dictionary<string, List<string>>()
        {
            {
                "phishing", new List<string>()
                {
                    "Be cautious of emails asking for personal info — legitimate organisations rarely request passwords by email.",
                    "Check the sender's email carefully. Phishing emails use addresses like 'support@paypa1.com' — notice the '1' instead of 'l'.",
                    "Never click links in unexpected emails. Go directly to the official website by typing the address yourself.",
                    "Look out for urgent language like 'Your account will be suspended!' — scammers use fear to make you act without thinking.",
                    "Hover over links before clicking to see the real URL — if it doesn't match the sender, it's a phishing attempt.",
                    "Spear phishing targets specific people using personal info scraped from social media. Be careful what you share publicly.",
                    "When in doubt, call the company directly using a number from their official website — never one given in a suspicious email."
                }
            },
            {
                "password", new List<string>()
                {
                    "Use a passphrase like 'Lion#Runs@Sunset2026!' — long and memorable, but incredibly hard to crack.",
                    "Never reuse passwords. If one site is breached, attackers will try the same password on your email and banking accounts.",
                    "Use a password manager like Bitwarden or 1Password — they generate and store strong, unique passwords for every site.",
                    "Change your passwords immediately if you suspect a breach, and enable 2FA on all important accounts.",
                    "A strong password is at least 12 characters long with uppercase, lowercase, numbers, and symbols.",
                    "Avoid obvious passwords like your name, birthday, or 'password123' — these are the first things attackers try.",
                    "Check if your email has been in a data breach at haveibeenpwned.com — then change any affected passwords immediately."
                }
            },
            {
                "privacy", new List<string>()
                {
                    "Review your social media privacy settings — limit who can see your posts, location, and personal details.",
                    "Be careful what you share online. Your birthday and school name can help attackers crack your security questions.",
                    "Use a VPN on public WiFi to encrypt your traffic and prevent eavesdroppers from intercepting your data.",
                    "Check which apps have access to your camera, microphone, and location — revoke permissions you don't actively need.",
                    "Read privacy policies before signing up — look for what data is collected and whether it's sold to third parties.",
                    "Use a privacy-focused search engine like DuckDuckGo to reduce how much data is collected about your searches.",
                    "Regularly audit your connected accounts — apps that use 'Login with Google' share your data with Google."
                }
            },
            {
                "malware", new List<string>()
                {
                    "Never download software from unofficial websites — always use the developer's official site or a trusted app store.",
                    "Malware can hide inside email attachments, even in Word or PDF files. Scan attachments before opening them.",
                    "Keep your antivirus and operating system updated — new malware signatures are released every day.",
                    "If your computer suddenly slows down, shows unexpected pop-ups, or crashes often, it may be infected — run a full scan.",
                    "Spyware secretly monitors your activity and sends data to attackers. Use reputable security software to detect and remove it."
                }
            },
            {
                "ransomware", new List<string>()
                {
                    "Back up your data regularly using the 3-2-1 rule — 3 copies, 2 different media, 1 offsite. This is your best defence.",
                    "Never pay the ransom — there is no guarantee attackers will restore your files, and it encourages future attacks.",
                    "Ransomware often spreads through phishing emails and malicious downloads. Think before you click on anything.",
                    "Keep all software and your operating system updated — ransomware frequently exploits known vulnerabilities in outdated software.",
                    "Disconnect from the internet immediately if you suspect a ransomware infection — this can stop it spreading to other devices."
                }
            },
            {
                "wifi", new List<string>()
                {
                    "Avoid using public WiFi for sensitive tasks like banking or shopping — use mobile data instead.",
                    "Use a VPN on public WiFi to encrypt your traffic and protect your data from eavesdroppers.",
                    "Make sure your home WiFi uses WPA3 or WPA2 encryption — check your router's settings to confirm.",
                    "Change your router's default admin password immediately — factory passwords are publicly known and easy to exploit.",
                    "Turn off WiFi on your phone when not in use — devices constantly probe for known networks, which attackers can spoof."
                }
            },
            {
                "2fa", new List<string>()
                {
                    "Enable 2FA on every account that supports it — especially email, banking, and social media.",
                    "Use an authenticator app like Google Authenticator rather than SMS — SIM swapping attacks can intercept SMS codes.",
                    "Even if an attacker has your password, 2FA blocks them from logging in without your second factor.",
                    "Keep backup codes in a safe place — if you lose your phone, they let you regain access to your accounts.",
                    "Hardware keys like YubiKey offer the strongest form of 2FA and are virtually impossible to phish."
                }
            },
            {
                "encryption", new List<string>()
                {
                    "End-to-end encryption means only you and the recipient can read messages — even the service provider can't. Use Signal.",
                    "Always check for 'https://' and the padlock icon before entering personal information on any website.",
                    "Full-disk encryption protects your data if your device is stolen — enable it on your laptop and phone today.",
                    "Encrypted email services like ProtonMail keep your messages private, unlike standard email providers.",
                    "Passwords should never be stored in plain text — services should use hashing algorithms like bcrypt to protect them."
                }
            }
        };

        // ── Sentiment keyword map ────────────────────────────────
        // Maps emotion keywords in user input to a sentiment category
        private Dictionary<string, string> sentimentMap = new Dictionary<string, string>()
        {
            { "worried",          "anxious"     },
            { "scared",           "anxious"     },
            { "anxious",          "anxious"     },
            { "nervous",          "anxious"     },
            { "afraid",           "anxious"     },
            { "frustrated",       "frustrated"  },
            { "angry",            "frustrated"  },
            { "annoyed",          "frustrated"  },
            { "fed up",           "frustrated"  },
            { "confused",         "confused"    },
            { "lost",             "confused"    },
            { "unsure",           "confused"    },
            { "don't understand", "confused"    },
            { "sad",              "sad"         },
            { "unhappy",          "sad"         },
            { "down",             "sad"         },
            { "depressed",        "sad"         },
            { "happy",            "positive"    },
            { "excited",          "positive"    },
            { "great",            "positive"    },
            { "awesome",          "positive"    },
            { "curious",          "curious"     },
            { "interested",       "curious"     },
            { "want to learn",    "curious"     },
            { "overwhelmed",      "overwhelmed" },
            { "stressed",         "overwhelmed" },
            { "too much",         "overwhelmed" }
        };

        // ── Multiple empathy responses per sentiment ─────────────
        private Dictionary<string, List<string>> sentimentResponses = new Dictionary<string, List<string>>()
        {
            {
                "anxious", new List<string>()
                {
                    "It's completely understandable to feel that way — cyber threats are real, but knowledge is your best defence.",
                    "Feeling worried about online security is actually a great first step — it means you're taking it seriously.",
                    "Don't panic! Most attacks target uninformed people. The fact that you're asking means you're already ahead."
                }
            },
            {
                "frustrated", new List<string>()
                {
                    "I hear you — cybersecurity can feel overwhelming at first. Let's slow down and take it one topic at a time.",
                    "It's frustrating when things feel complicated. Let's focus on one practical step you can take right now.",
                    "Even cybersecurity professionals find this stuff complex sometimes. You're doing great just by being here."
                }
            },
            {
                "confused", new List<string>()
                {
                    "No problem at all — let me break that down more simply for you.",
                    "Confusion is part of learning! Ask me to explain anything in simpler terms and I'll do my best.",
                    "Great question to be confused about — it means you're thinking critically. Let me clarify."
                }
            },
            {
                "sad", new List<string>()
                {
                    "I'm sorry you're feeling down. Remember, every step you take to learn about cybersecurity is a real win!",
                    "It's okay to feel that way. Learning something new is tough — but you're making progress just by being here.",
                    "Take it easy on yourself. Cybersecurity is a journey, not a destination — small steps add up to big protection."
                }
            },
            {
                "positive", new List<string>()
                {
                    "Love the energy! Let's channel that positivity into some great cybersecurity habits.",
                    "Fantastic mindset! You're in the perfect mood to learn something valuable today.",
                    "That's the spirit! Staying positive while learning about security is the best approach."
                }
            },
            {
                "curious", new List<string>()
                {
                    "Curiosity is the best tool in cybersecurity — let's explore together!",
                    "I love the enthusiasm! Curious people make the best learners — let's dive in.",
                    "That's exactly the right attitude. Let me share something interesting with you."
                }
            },
            {
                "overwhelmed", new List<string>()
                {
                    "Take a breath — cybersecurity doesn't have to be learned all at once. Let's focus on one thing.",
                    "I understand — there's a lot to take in. Let's start with what matters most for you right now.",
                    "Feeling overwhelmed is completely normal. Let's break this down into simple, actionable steps."
                }
            }
        };

        // ── Which topic to suggest after each sentiment ──────────
        private Dictionary<string, string> sentimentTopicMap = new Dictionary<string, string>()
        {
            { "anxious",     "phishing"      },
            { "frustrated",  "password"      },
            { "confused",    "safe browsing" },
            { "sad",         "privacy"       },
            { "positive",    "2fa"           },
            { "curious",     "encryption"    },
            { "overwhelmed", "password"      }
        };

        // ============================================================
        //  PlayVoiceGreeting — plays the WAV greeting on startup
        // ============================================================
        public void PlayVoiceGreeting()
        {
            try
            {
                string wavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Greeting.wav");
                if (File.Exists(wavPath))
                {
                    SoundPlayer player = new SoundPlayer(wavPath);
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                // Audio is non-critical — log the issue and continue
                Console.WriteLine("Audio warning: " + ex.Message);
            }
        }

        // ============================================================
        //  GetResponse — public entry point; logs history and routes
        // ============================================================
        public string GetResponse(string input)
        {
            // Guard: empty input
            if (string.IsNullOrWhiteSpace(input))
                return "You didn't type anything. Please ask a question or type 'help'.";

            string lower = input.ToLower().Trim();

            // Add to conversation history before routing
            ConversationHistory.Add("You: " + input);

            string response = RouteInput(lower, input);

            // Log the bot's response too
            ConversationHistory.Add("CyberBot: " + response);

            return response;
        }

        // ============================================================
        //  RouteInput — central dispatcher; checks each handler type
        // ============================================================
        private string RouteInput(string lower, string original)
        {
            // Exit commands
            if (lower == "exit" || lower == "quit" || lower == "bye")
                return "Goodbye, " + UserName + "! Stay cyber-safe — think before you click!";

            // Help request
            if (lower.Contains("help") || lower.Contains("what can i ask"))
                return BuildHelpMessage();

            // Greeting
            if (lower.Contains("how are you"))
                return "I'm running perfectly! More importantly, how are YOU doing, " + UserName + "?";

            // Name update mid-chat
            if (lower.Contains("my name is"))
                return HandleNameUpdate(lower, original);

            // Interest update
            if (lower.Contains("i'm interested in") || lower.Contains("i am interested in"))
                return HandleInterestUpdate(lower, original);

            // Memory recall request
            if (lower.Contains("what do you remember") || lower.Contains("what do you know about me"))
                return BuildMemoryRecallMessage();

            // Follow-up on last topic
            if (lower.Contains("tell me more") || lower.Contains("more details") || lower == "more")
                return HandleFollowUp();

            // Sentiment detection — check before keywords so emotion is prioritised
            string detectedSentiment = DetectSentiment(lower);
            if (detectedSentiment != "")
                return HandleSentiment(detectedSentiment);

            // Multi-response keyword topics
            foreach (string topic in randomResponses.Keys)
            {
                if (lower.Contains(topic))
                {
                    LastTopic = topic;
                    IncrementTopicFrequency(topic);
                    return AddPersonalisation(GetRandomTip(topic), topic);
                }
            }

            // Single keyword responses
            foreach (string keyword in keywordResponses.Keys)
            {
                if (lower.Contains(keyword))
                {
                    LastTopic = keyword;
                    IncrementTopicFrequency(keyword);
                    return AddPersonalisation(keywordResponses[keyword], keyword);
                }
            }

            // Fallback for unrecognised input
            return "I'm not sure I understand that, " + UserName +
                   ". Try rephrasing, or type 'help' to see what I can help with.";
        }

        // ============================================================
        //  HandleNameUpdate — updates username mid-conversation
        // ============================================================
        private string HandleNameUpdate(string lower, string original)
        {
            int idx = lower.IndexOf("my name is") + "my name is".Length;
            string newName = original.Substring(idx).Trim();

            if (string.IsNullOrWhiteSpace(newName))
                return "I didn't catch your name — could you try again?";

            string oldName = UserName;
            UserName = newName;

            return string.IsNullOrEmpty(oldName)
                ? "Nice to meet you, " + UserName + "! I'll remember your name."
                : "Got it! I'll call you " + UserName + " from now on instead of " + oldName + ".";
        }

        // ============================================================
        //  HandleInterestUpdate — tracks and recalls user interests
        // ============================================================
        private string HandleInterestUpdate(string lower, string original)
        {
            int idx = lower.Contains("i'm interested in")
                ? lower.IndexOf("i'm interested in") + "i'm interested in".Length
                : lower.IndexOf("i am interested in") + "i am interested in".Length;

            string topic = original.Substring(idx).Trim();

            if (string.IsNullOrWhiteSpace(topic))
                return "What topic are you interested in? Let me know!";

            bool alreadyMentioned = AllInterests.Contains(topic.ToLower());
            if (!alreadyMentioned)
                AllInterests.Add(topic.ToLower());

            string previousInterest = UserInterest;
            UserInterest = topic;

            // User changed their stated interest
            if (!string.IsNullOrEmpty(previousInterest) && previousInterest.ToLower() != topic.ToLower())
            {
                return "Interesting! You previously told me you were interested in " + previousInterest +
                       ", and now you're exploring " + topic + " as well. I love that you're expanding your knowledge, " +
                       UserName + "! I'll keep both topics in mind as we chat.";
            }

            // User repeated an interest they've mentioned before
            if (alreadyMentioned)
            {
                return "You've mentioned " + topic + " before — clearly it's important to you, " + UserName +
                       "! Let me know if you'd like to go deeper on it.";
            }

            // First time mentioning this interest
            return "Got it, " + UserName + "! I'll remember that you're interested in " + topic + ". " +
                   "That's a great area to focus on — I'll bring it up whenever it's relevant to our conversation!";
        }

        // ============================================================
        //  HandleFollowUp — gives more info on the last discussed topic
        // ============================================================
        private string HandleFollowUp()
        {
            if (string.IsNullOrEmpty(LastTopic))
                return "What topic would you like more info on? Try asking about password, phishing, or privacy.";

            string tip = GetFollowUpTip(LastTopic);
            int visitCount = topicFrequency.ContainsKey(LastTopic) ? topicFrequency[LastTopic] : 1;

            // Vary the intro based on how many times this topic has been visited
            string prefix = visitCount > 2
                ? "You seem really interested in " + LastTopic + " — here's another angle: "
                : "Sure! Here's more about " + LastTopic + ": ";

            return prefix + tip;
        }

        // ============================================================
        //  DetectSentiment — scans input for emotion keywords
        // ============================================================
        private string DetectSentiment(string lower)
        {
            foreach (string keyword in sentimentMap.Keys)
            {
                if (lower.Contains(keyword))
                    return sentimentMap[keyword];
            }
            return "";
        }

        // ============================================================
        //  HandleSentiment — empathetic reply + relevant tip
        // ============================================================
        private string HandleSentiment(string sentiment)
        {
            LastEmotion = sentiment;

            // Pick a random empathy line for this emotion
            string empathy = GetRandomFromList(sentimentResponses[sentiment]);

            // Get the suggested cybersecurity topic for this emotion
            string suggestedTopic = sentimentTopicMap.ContainsKey(sentiment)
                ? sentimentTopicMap[sentiment]
                : "password";

            LastTopic = suggestedTopic;
            IncrementTopicFrequency(suggestedTopic);

            // Get a relevant tip for that topic
            string tip = randomResponses.ContainsKey(suggestedTopic)
                ? GetRandomTip(suggestedTopic)
                : keywordResponses.ContainsKey(suggestedTopic)
                    ? keywordResponses[suggestedTopic]
                    : "";

            string followUpPrompt = "\n\nWould you like to know more about " + suggestedTopic +
                                    "? Just say 'tell me more'!";

            return empathy +
                   (tip != "" ? " Here's something useful to know: " + tip : "") +
                   followUpPrompt;
        }

        // ============================================================
        //  BuildMemoryRecallMessage — summarises what the bot remembers
        // ============================================================
        private string BuildMemoryRecallMessage()
        {
            var facts = new List<string>();

            if (!string.IsNullOrEmpty(UserName))
                facts.Add("Your name is " + UserName);

            if (!string.IsNullOrEmpty(UserInterest))
                facts.Add("You're currently interested in " + UserInterest);

            if (AllInterests.Count > 1)
                facts.Add("All interests you've shared: " + string.Join(", ", AllInterests));

            if (!string.IsNullOrEmpty(LastTopic))
                facts.Add("The last topic we discussed was " + LastTopic);

            if (!string.IsNullOrEmpty(LastEmotion))
                facts.Add("The last emotion you expressed was feeling " + LastEmotion);

            if (topicFrequency.Count > 0)
                facts.Add("Your most-asked topic is " + GetMostAskedTopic());

            if (facts.Count == 0)
                return "I don't know much about you yet! Tell me your name and what you're interested in.";

            return "Here's what I remember about you, " + UserName + ":\n• " +
                   string.Join("\n• ", facts);
        }

        // ============================================================
        //  BuildHelpMessage — returns the full list of topics
        // ============================================================
        private string BuildHelpMessage()
        {
            return "Here's what I can help you with, " + UserName + ":\n\n" +
                   "Security Topics: password, phishing, privacy, malware, virus, ransomware, " +
                   "encryption, firewall, 2fa, wifi, update, scam, safe browsing, vpn, backup, " +
                   "antivirus, social engineering, identity theft, dark web, hacker, cybersecurity\n\n" +
                   "Commands:\n" +
                   "  'tell me more'          — follow-up on the last topic\n" +
                   "  'what do you remember'  — see what I've remembered about you\n" +
                   "  'help'                  — show this list\n" +
                   "  'exit'                  — quit the app";
        }

        // ============================================================
        //  GetRandomTip — returns a random tip from a topic's list
        // ============================================================
        private string GetRandomTip(string topic)
        {
            return GetRandomFromList(randomResponses[topic]);
        }

        // ============================================================
        //  GetRandomFromList — generic helper for random selection
        // ============================================================
        private string GetRandomFromList(List<string> list)
        {
            return list[random.Next(list.Count)];
        }

        // ============================================================
        //  GetFollowUpTip — gets a tip for the last discussed topic
        // ============================================================
        private string GetFollowUpTip(string topic)
        {
            if (randomResponses.ContainsKey(topic))
                return GetRandomTip(topic);

            if (keywordResponses.ContainsKey(topic))
                return keywordResponses[topic];

            return "I don't have more on that right now. Try asking about password, phishing, or privacy!";
        }

        // ============================================================
        //  AddPersonalisation — appends name + interest to any response
        // ============================================================
        private string AddPersonalisation(string response, string topic)
        {
            // Always greet the user by name
            string result = UserName + ", " + response;

            // Naturally remind the user their interest is relevant — vary phrasing by visit count
            if (!string.IsNullOrEmpty(UserInterest))
            {
                int visitCount = topicFrequency.ContainsKey(topic) ? topicFrequency[topic] : 1;

                if (visitCount == 1)
                    result += " Since you're interested in " + UserInterest + ", this is definitely relevant to you!";
                else if (visitCount == 2)
                    result += " This connects closely to your interest in " + UserInterest + ".";
                else
                    result += " You clearly care about " + UserInterest + " — " + topic + " is a big part of that picture.";
            }

            // Always prompt the user to continue exploring
            result += "\n\nWould you like to know more? Say 'tell me more' or ask about another topic.";

            return result;
        }

        // ============================================================
        //  IncrementTopicFrequency — tracks how often topics are asked
        // ============================================================
        private void IncrementTopicFrequency(string topic)
        {
            if (topicFrequency.ContainsKey(topic))
                topicFrequency[topic]++;
            else
                topicFrequency[topic] = 1;
        }

        // ============================================================
        //  GetMostAskedTopic — returns the topic with the highest count
        // ============================================================
        private string GetMostAskedTopic()
        {
            string mostAsked = "";
            int max = 0;
            foreach (var pair in topicFrequency)
            {
                if (pair.Value > max)
                {
                    max = pair.Value;
                    mostAsked = pair.Key;
                }
            }
            return mostAsked;
        }
    }
}