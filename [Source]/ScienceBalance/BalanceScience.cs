using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace SigmaScienceBalancePlugin
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class BalanceScience : MonoBehaviour
    {
        public static Dictionary<CelestialBody, float> scienceCap = new Dictionary<CelestialBody, float>();
        public static string messageText = "<b><color=#FFAA45>Wernher von Kerman:</color>\n<color=#FFFFFF>We are getting too much data from <cb>.\nIt won't help us any longer.\n \n \n \n \n \n \n \n \n \n </color></b>";
        public static float messageDuration = 15f;
        public static bool debug = false;

        void Awake()
        {
            // Get settings node
            ConfigNode settings = GameDatabase.Instance.GetConfigNodes("SigmaScienceBalance").FirstOrDefault();
            if (settings == null) return;

            // Load debug setting
            if (settings.HasValue("debug") && settings.GetValue("debug").Equals("true", StringComparison.OrdinalIgnoreCase))
                debug = true;
            Debug.Log("Found settings node.");

            // Load settings from config
            if (settings.HasValue("messageText"))
                messageText = settings.GetValue("messageText"); Debug.Log("messageText = " + messageText);
            if (settings.HasValue("messageDuration"))
                messageDuration = GetDuration(settings.GetValue("messageDuration")); Debug.Log("messageDuration = " + messageDuration);

            // Load science cap from config
            foreach (ConfigNode Body in settings.GetNodes("Body").Where(n => n.HasValues(new[] { "name", "cap" })))
            {
                Debug.Log("Found Body node.");
                CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(b => b.name == Body.GetValue("name"));
                if (body == null) continue;
                Debug.Log("body = " + body);

                float cap = 0;
                if (float.TryParse(Body.GetValue("cap"), out cap) && cap > 0 && cap < float.PositiveInfinity)
                {
                    scienceCap.Add(body, cap); Debug.Log("added [" + body.name + ", " + cap + "] to scienceCap.");
                }
            }
            Debug.Log("scienceCap.Count = " + scienceCap.Count);

            // Use the mod only if required
            if (scienceCap.Count > 0)
            {
                Debug.Log("Mod is required and will be used.");
                GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(FixScience));
                DontDestroyOnLoad(this);
            }
        }

        void FixScience(float value, ScienceSubject subject, ProtoVessel vessel, bool dataBool)
        {
            Debug.Log(value + " science received for " + subject.id + ".");
            CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(cb => subject.IsFromBody(cb));

            if (body != null && scienceCap.ContainsKey(body))
            {
                Debug.Log("body = " + body);

                // Count past science
                float pastScience = 0; Debug.Log("counting past science.");
                foreach (ScienceSubject subj in ResearchAndDevelopment.GetSubjects().Where(s => s.IsFromBody(body)))
                {
                    pastScience += subj.science;
                    Debug.Log("adding " + subj.science + " science from " + subj.id + ".");
                }
                Debug.Log("pastScience = " + pastScience);

                if (pastScience > scienceCap[body])
                {
                    // Calculate excess science
                    float excessScience = pastScience - scienceCap[body]; Debug.Log("excessScience = " + excessScience);

                    // Remove excess science, do not remove more than what has been just added
                    ResearchAndDevelopment.Instance.CheatAddScience(excessScience < value ? -excessScience : -value); Debug.Log("offset science by " + (excessScience < value ? -excessScience : -value));

                    // Clear all messages
                    foreach (ScreenMessagesText message in ScreenMessages.Instance.gameObject.GetComponentsInChildren<ScreenMessagesText>())
                    {
                        Destroy(message.gameObject);
                    }

                    // Add new message
                    ScreenMessages.PostScreenMessage(FixMessage(messageText, body), messageDuration);
                    UnityEngine.Debug.Log("<b>[SigmaScienceBalance]<color=#EE0000>[WARNING]</color><color=#FFFFFF>: Reached Science Cap for " + body.name + ". (" + scienceCap[body] + ")</color></b>");
                }
            }
        }

        // Replace text in the message
        string FixMessage(string message, CelestialBody body)
        {
            return message
                .Replace("<cb>", body.name)
                .Replace("<<", "<")
                .Replace(">>", ">")
                .Replace("< ", " ")
                .Replace("> ", " ");
        }

        // Parse the messageDuration
        float GetDuration(string input)
        {
            float output = messageDuration;

            if (!float.TryParse(input, out output))
                output = messageDuration;

            if (output > 0 && output < float.PositiveInfinity)
                return output;
            else
                return messageDuration;
        }
    }

    public static class Debug
    {
        public static void Log(string s)
        {
            if (BalanceScience.debug) UnityEngine.Debug.Log("SigmaLog: ScienceBalance: " + s);
        }
    }
}
