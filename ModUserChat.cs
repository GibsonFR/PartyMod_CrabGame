using static PartyMod.UiUtility;
using static PartyMod.ModUserChatManager;
using static PartyMod.ModUserChatUtility;

namespace PartyMod
{
    public class ModUserChatManager : MonoBehaviour
    {
        public static ModUserChatManager Instance;

        public static readonly List<string> messages = new();
        public static Text messagesDisplay;
        public static InputField messagesInput;
        public static Text inputField;
        public static InputField chatInput;

        public bool inputActive, isCustomChatBoxCreated, chatCreated;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            GibsonNetworkManager.OnPacketReceived -= HandleChatPacket;
            GibsonNetworkManager.OnPacketReceived += HandleChatPacket;
        }

        void Update()
        {
            if (ChatBox.Instance != null && !isCustomChatBoxCreated)
            {
                isCustomChatBoxCreated = true;
                CreateInputField();
            }

            if (ChatBox.Instance != null && !chatCreated)
            {
                chatCreated = true;
                CreateChatBox();
            }

            if (!inputActive && Input.GetKeyDown(KeyCode.Return))
            {
                DisableNativeChatBox();
                inputActive = true;
                chatInput.interactable = true;
                chatInput.gameObject.SetActive(true);
                chatInput.ActivateInputField();
            }
            else if (inputActive && Input.GetKeyDown(KeyCode.Return))
            {
                string text = chatInput.text.Trim();

                if (!string.IsNullOrEmpty(text))
                {
                    if (text.StartsWith("/msg"))
                    {
                        string message = text.Substring(4).Trim();
                        if (!string.IsNullOrEmpty(message)) SendChatMessage(message);
                    }
                    else if (text.StartsWith("/party")) PartyManager.HandlePartyCommand(text, clientId);
                    else if (text.StartsWith("/net")) GibsonNetworkManager.ForceTryReconnect();
                    else ChatBox.Instance?.SendMessage(text); 
                    
                }

                chatInput.text = "";
                chatInput.DeactivateInputField();
                chatInput.gameObject.SetActive(false);
                inputActive = false;
            }

        }
    }

    public class ModUserChatUtility
    {
        public static void AppendCustomMessage(string text)
        {
            messages.Add(text);
            if (messages.Count > 4) messages.RemoveAt(0);

            if (messagesDisplay != null)
                messagesDisplay.text = string.Join("\n", messages);

            if (messagesInput != null)
                messagesInput.text = "";
        }

        public static void SendChatMessage(string text)
        {
            string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(clientId));
            pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);

            string taggedMsg = $"[CHAT] {pseudo}: {text}";

            foreach (var user in connectedModUsers.Keys)
            {
                if (user == clientId) continue;
                GibsonNetworkManager.SendPacket(user, taggedMsg);
            }

            AppendCustomMessage($"(You) {pseudo}: {text}");
        }

        public static void HandleChatPacket(ulong senderId, string message)
        {
            if (!message.StartsWith("[CHAT]")) return;

            string payload = message.Substring(6); 
            AppendCustomMessage(payload);
        }
        public static void DisableNativeChatBox()
        {
            var realChat = GameObject.FindObjectOfType<ChatBox>();
            if (realChat != null && realChat.inputField != null)
            {
                realChat.enabled = false;
                realChat.inputField.enabled = false;
            }
        }
        public static void CreateChatBox()
        {
            Canvas parentCanvas = GameObject.FindObjectsOfType<Canvas>()
                .FirstOrDefault(c => c.renderMode != RenderMode.WorldSpace);
            if (!parentCanvas) return;

            var panel = CreateUIObject("SecretChatPanel", parentCanvas.transform,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector3(20, -20, 0), new Vector2(400, 150));
            AddImage(panel, new Color(0.12f, 0.18f, 0.12f, 0.93f));

            var displayObj = CreateUIObject("MessagesDisplay", panel.transform,
                new Vector2(0, 0.3f), new Vector2(1, 1), new Vector2(0, 1),
                Vector3.zero, Vector2.zero);

            var contentObj = CreateUIObject("Content", displayObj.transform,
                Vector2.zero, Vector2.one, new Vector2(0, 1), Vector3.zero, Vector2.zero);

            messagesDisplay = AddText(contentObj, 18, new Color(0.8f, 1f, 0.8f), TextAnchor.UpperLeft);
            AddScrollRect(panel, displayObj.GetComponent<RectTransform>(), contentObj.GetComponent<RectTransform>());

            var inputObj = CreateUIObject("SecretInputField", panel.transform,
                new Vector2(0, 0), new Vector2(1, 0.3f), Vector2.zero,
                Vector3.zero, Vector2.zero);

            var inputTextObj = CreateUIObject("InputText", inputObj.transform,
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector3.zero, Vector2.zero);

            var inputText = AddText(inputTextObj, 18, Color.white, TextAnchor.UpperLeft);
            messagesInput = AddInputField(inputObj, inputText, InputField.LineType.MultiLineNewline);
        }

        public static void CreateInputField()
        {
            Canvas parentCanvas = GameObject.FindObjectsOfType<Canvas>()
                .FirstOrDefault(c => c.renderMode != RenderMode.WorldSpace);
            if (!parentCanvas) return;

            var panel = CreateUIObject("CustomChatBox", parentCanvas.transform,
                Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector3(35, 22, 0), new Vector2(400, 150));
            AddImage(panel, new Color(0.12f, 0.18f, 0.12f, 0f));

            var displayObj = CreateUIObject("ChatDisplay", panel.transform,
                new Vector2(0, 0.3f), new Vector2(1, 1), new Vector2(0, 1),
                Vector3.zero, Vector2.zero);

            inputField = AddText(displayObj, 18, Color.white, TextAnchor.UpperLeft);

            var inputObj = CreateUIObject("ChatInput", panel.transform,
                new Vector2(0, 0), new Vector2(1, 0.3f), Vector2.zero,
                Vector3.zero, Vector2.zero);

            var inputTextObj = CreateUIObject("InputText", inputObj.transform,
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector3.zero, Vector2.zero);

            var inputText = AddText(inputTextObj, 18, Color.white, TextAnchor.MiddleLeft);
            chatInput = AddInputField(inputObj, inputText, InputField.LineType.SingleLine);
            chatInput.interactable = false;
            chatInput.gameObject.SetActive(false);
        }
    }
}
