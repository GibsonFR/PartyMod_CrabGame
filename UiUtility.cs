using static PartyMod.UiUtility;

namespace PartyMod
{
    public class UiUtility
    {
        private static readonly Font DefaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        public static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = position;
            rt.sizeDelta = size;

            return go;
        }

        public static Image AddImage(GameObject obj, Color color)
        {
            var img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text AddText(GameObject obj, int fontSize, Color color, TextAnchor alignment)
        {
            var txt = obj.AddComponent<Text>();
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.supportRichText = false;
            return txt;
        }

        public static InputField AddInputField(GameObject obj, Text textComponent, InputField.LineType lineType = InputField.LineType.SingleLine)
        {
            var input = obj.AddComponent<InputField>();
            input.textComponent = textComponent;
            input.lineType = lineType;
            input.interactable = true;
            return input;
        }

        public static ScrollRect AddScrollRect(GameObject obj, RectTransform viewport, RectTransform content)
        {
            var scroll = obj.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            return scroll;
        }
    }

    public static class ChatBoxBuilder
    {
        private static CustomChatBoxManager Instance;
        private static Text messagesDisplay;
        private static InputField messagesInput;

        public static void CreateChatBox()
        {
            if (Instance != null) return;

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

            CustomChatBoxManager.messagesDisplay = messagesDisplay;
            CustomChatBoxManager.messagesInput = messagesInput;

            Instance = panel.AddComponent<CustomChatBoxManager>();
            CustomChatBoxManager.Instance = Instance; 
            Instance.gameObject.SetActive(true);

        }
    }

    public static class InputFieldBuilder
    {
        private static CustomChatBoxManager Instance;
        private static Text inputField;
        private static InputField chatInput;

        public static void CreateInputField()
        {
            if (Instance != null) return;

            Canvas parentCanvas = GameObject.FindObjectsOfType<Canvas>()
                .FirstOrDefault(c => c.renderMode != RenderMode.WorldSpace);
            if (!parentCanvas)
            {
                Debug.LogError("[CustomChatBox] Canvas non trouvé !");
                return;
            }

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

            CustomChatBoxManager.inputField = inputField;
            CustomChatBoxManager.chatInput = chatInput;

            Instance = panel.AddComponent<CustomChatBoxManager>();

        }
    }
}
