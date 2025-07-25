namespace PartyMod
{
    public class PartyChatManager : MonoBehaviour
    {
        public static bool showSecretChatBox = true;
        public static PartyChatManager Instance;

        public static InputField messagesInput;
        public static Text messagesDisplay;
        public static List<string> messages = new();

        void Update()
        {
            if (ChatBox.Instance != null) CreateIfNeeded();
            if (Instance != null)
                Instance.gameObject.SetActive(showSecretChatBox);
        }

        public static void CreateIfNeeded()
        {
            if (Instance != null) return;

            Canvas parentCanvas = GameObject.FindObjectsOfType<Canvas>()
                .FirstOrDefault(c => c.renderMode != RenderMode.WorldSpace);
            if (parentCanvas == null) return;

            var go = new GameObject("SecretChatPanel");
            go.transform.SetParent(parentCanvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = new Vector3(20, -20, 0);
            rt.sizeDelta = new Vector2(400, 150);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.18f, 0.12f, 0.93f);

            // Zone d'affichage texte (Text)
            var displayObj = new GameObject("MessagesDisplay");
            displayObj.transform.SetParent(go.transform, false);

            var displayRect = displayObj.AddComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0, 0.3f);
            displayRect.anchorMax = new Vector2(1, 1);
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.viewport = displayRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(displayObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            messagesDisplay = contentObj.AddComponent<Text>();
            messagesDisplay.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            messagesDisplay.fontSize = 18;
            messagesDisplay.color = new Color(0.8f, 1f, 0.8f);
            messagesDisplay.alignment = TextAnchor.UpperLeft;
            messagesDisplay.horizontalOverflow = HorizontalWrapMode.Wrap;
            messagesDisplay.verticalOverflow = VerticalWrapMode.Overflow;
            messagesDisplay.text = "";
            messagesDisplay.supportRichText = false;

            scrollRect.content = contentRect;

            // Zone d'entrée (InputField)
            var inputObj = new GameObject("SecretInputField");
            inputObj.transform.SetParent(go.transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.3f);
            inputRect.offsetMin = new Vector2(12, 12);
            inputRect.offsetMax = new Vector2(-12, -12);

            messagesInput = inputObj.AddComponent<InputField>();
            messagesInput.interactable = true;
            messagesInput.lineType = InputField.LineType.MultiLineNewline;
            messagesInput.readOnly = false;
            messagesInput.text = "";

            // Text inside InputField
            var inputTextObj = new GameObject("InputText");
            inputTextObj.transform.SetParent(inputObj.transform, false);
            var inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            var inputText = inputTextObj.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = 18;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.UpperLeft;
            inputText.supportRichText = false;

            messagesInput.textComponent = inputText;

            Instance = go.AddComponent<PartyChatManager>();
        }

        public void AppendSecret(string text)
        {
            messages.Add(text);
            if (messages.Count > 4) messages.RemoveAt(0);
            if (messagesDisplay != null)
            {
                messagesDisplay.text = string.Join("\n", messages);
            }
            if (messagesInput != null)
            {
                messagesInput.text = "";
            }
        }

        public static void HandleGMFChatPacket(string[] args)
        {
            string msg = string.Join(" ", args);
            CreateIfNeeded();
            Instance.AppendSecret(msg);
        }

        public static void SendSecuredMessage(string message)
        {
            string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(clientId));
            pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);
            string taggedMsg = $"[GMF] chat {pseudo}: {message}";

            var packets = CreateGMFPacket(taggedMsg);

            foreach (var user in modUsers)
            {
                if (user.Key == clientId) continue;
                SendGMFPacket(user.Key, packets);
            }

            CreateIfNeeded();
            Instance.AppendSecret("(You) " + $"{pseudo}: {message}");
        }
    }

    public class ChatPatches
    {
        [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.SendMessage), new[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool OnChatBoxSendMessagePrefix(ChatBox __instance, ref string __0)
        {
            if (__0.StartsWith("/msg"))
            {
                string message = Regex.Replace(__0.Substring(5), "<.*?>", string.Empty);
                PartyChatManager.SendSecuredMessage(message);
                __instance.inputField.text = "";
                return false;
            }
            if (__0.StartsWith("/party"))
            {
                HandlePartyCommand(__0, clientId);
                __instance.inputField.text = "";
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientSend), nameof(ClientSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool OnClientSendSendChatMessagePrefix(string __0)
        {
            if (!__0.StartsWith("/party") && !__0.StartsWith("/msg")) return true;
            return false;
        }

        [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.SendMessage))]
        [HarmonyPostfix]
        public static void OnChatBoxUpdate(ChatBox __instance)
        {
            var textComp = __instance.messages;
            if (textComp != null && !string.IsNullOrEmpty(textComp.text))
            {
                var cleanLines = textComp.text
                    .Split('\n')
                    .Where(line => !line.Contains("/party") && !line.Contains("/msg"))
                    .ToArray();

                textComp.text = string.Join("\n", cleanLines);
            }
        }
    }
}
