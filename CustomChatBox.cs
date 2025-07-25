namespace PartyMod
{
    public class CustomChatBox : MonoBehaviour
    {
        public static CustomChatBox Instance;

        public static Text chatDisplay;
        public static InputField chatInput;
        private bool inputActive, isCustomChatBoxCreated;

        void Update()
        {
            if (ChatBox.Instance != null && !isCustomChatBoxCreated)
            {
                isCustomChatBoxCreated = true;  
                CreateIfNeeded();
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

        public static void CreateIfNeeded()
        {
            if (Instance != null) return;

            Canvas parentCanvas = GameObject.FindObjectsOfType<Canvas>()
                .FirstOrDefault(c => c.renderMode != RenderMode.WorldSpace);
            if (parentCanvas == null)
            {
                Debug.LogError("[CustomChatBox] Canvas non trouvé !");
                return;
            }

            var go = new GameObject("CustomChatBox");
            go.transform.SetParent(parentCanvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = new Vector3(35, 22, 0);
            rt.sizeDelta = new Vector2(400, 150);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.18f, 0.12f, 0f);

            var displayObj = new GameObject("ChatDisplay");
            displayObj.transform.SetParent(go.transform, false);

            var displayRect = displayObj.AddComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0, 0.3f);
            displayRect.anchorMax = new Vector2(1, 1);
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;

            chatDisplay = displayObj.AddComponent<Text>();
            chatDisplay.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            chatDisplay.fontSize = 18;
            chatDisplay.color = Color.white;
            chatDisplay.alignment = TextAnchor.UpperLeft;
            chatDisplay.horizontalOverflow = HorizontalWrapMode.Wrap;
            chatDisplay.verticalOverflow = VerticalWrapMode.Overflow;
            chatDisplay.text = "";

            var inputObj = new GameObject("ChatInput");
            inputObj.transform.SetParent(go.transform, false);

            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.3f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            chatInput = inputObj.AddComponent<InputField>();

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
            inputText.alignment = TextAnchor.MiddleLeft;

            chatInput.textComponent = inputText;
            chatInput.lineType = InputField.LineType.SingleLine;
            chatInput.interactable = false;  
            chatInput.gameObject.SetActive(false);

            Instance = go.AddComponent<CustomChatBox>();
        }

        public void AppendMessage(string text)
        {
            ChatBox.Instance.SendMessage(text);
        }

        private void DisableNativeChatBox()
        {
            var realChat = GameObject.FindObjectOfType<ChatBox>();
            if (realChat != null && realChat.inputField != null)
            {
                realChat.enabled = false;
                realChat.inputField.enabled = false;
            }
        }

    }
}
