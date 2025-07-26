using static PartyMod.CustomChatboxUtility;
using static PartyMod.CustomChatBoxManager;

namespace PartyMod
{
    public class CustomChatBoxManager : MonoBehaviour
    {
        public static CustomChatBoxManager Instance;

        public static Text inputField, messagesDisplay;
        public static InputField chatInput, messagesInput;
        public bool inputActive, isCustomChatBoxCreated, chatCreated;

        public static List<string> messages = new();

        void Update()
        {
            if (ChatBox.Instance != null && !isCustomChatBoxCreated)
            {
                isCustomChatBoxCreated = true;  
                InputFieldBuilder.CreateInputField();
            }

            if (ChatBox.Instance != null && !chatCreated)
            {
                chatCreated = true;
                ChatBoxBuilder.CreateChatBox();
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
                    AppendMessage(text);
                }

                chatInput.text = "";
                chatInput.DeactivateInputField();
                chatInput.gameObject.SetActive(false);
                inputActive = false;

            }

        }
    }

    public class CustomChatboxUtility
    {
      
        public static void AppendMessage(string text)
        {
            if (text.StartsWith("/msg"))
            {
                string message = Regex.Replace(text.Substring(5), "<.*?>", string.Empty);
                SendSecuredMessage(message);
            }
            else if (text.StartsWith("/party")) HandlePartyCommand(text, clientId);
            else if (text.StartsWith("/chat")) HandleChatCommand(text);
            else if (text.StartsWith("/net")) HandleNetCommand(text);
            else if (text.StartsWith("/help")) HandleHelpCommand();
            else ChatBox.Instance.SendMessage(text);
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

        public static void ChatEnable(bool visible)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[CustomChatBox] Instance not ready yet.");
                return;
            }
            Instance.gameObject.SetActive(visible);
        }



        public static void AppendCustomMessage(string text)
        {
            messages.Add(text);
            if (messages.Count > 4) messages.RemoveAt(0);
            if (messagesDisplay != null) messagesDisplay.text = string.Join("\n", messages);         
            if (messagesInput != null) messagesInput.text = "";   
        }

        public static void HandleGMFChatPacket(string[] args)
        {
            string msg = string.Join(" ", args);
            AppendCustomMessage(msg);
        }

        public static void SendSecuredMessage(string message)
        {
            string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(clientId));
            pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);
            string taggedMsg = $"[GMF] chat {pseudo}: {message}";

            var packets = CreateGMFPacket(taggedMsg);

            foreach (var user in connectedModUsers)
            {
                if (user.Key == clientId) continue;
                SendGMFPacket(user.Key, packets);
            }

            AppendCustomMessage("(You) " + $"{pseudo}: {message}");
        }
    }
}
