using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

namespace VirtualAssistant
{
    public class GeminiChatManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _button;
        [SerializeField] private ScrollRect _scroll;
        
        [SerializeField] private RectTransform _sent;
        [SerializeField] private RectTransform _received;

        private float _height;

        [SerializeField] private string _prompt = "You are Budi, a young man from Indonesia. Don't break character. Don't ever mention that you are an AI model.";

        [Header("JSON API Configuration")]
        [SerializeField] private TextAsset _jsonApi;
        private string _apiKey = ""; 
        private string _apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent"; // Edit it and choose your prefer model
        private Content[] _chatHistory;

        private void Start()
        {
            UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(_jsonApi.text);
            _apiKey = jsonApiKey.key;   
            _chatHistory = new Content[] { };
            _button.onClick.AddListener(SendChat);
        }

        private void AppendMessage(Content content)
        {
            _scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            var item = Instantiate(content.role == "user" ? _sent : _received, _scroll.content);
            item.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = content.parts[0].text;
            item.anchoredPosition = new Vector2(0, -_height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            _height += item.sizeDelta.y;
            _scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height);
            _scroll.verticalNormalizedPosition = 0;
        }

        public void SendChat()
        {
            string userMessage = _inputField.text;
            StartCoroutine( SendChatRequestToGemini(_inputField.text));
        }

        private IEnumerator SendChatRequestToGemini(string newMessage)
        {

            string url = $"{_apiEndpoint}?key={_apiKey}";
        
            Content userContent = new Content
            {
                role = "user",
                parts = new Part[]
                {
                    new Part { text = newMessage }
                }
            };

            Content systemInstructionContent = new Content
            {
                role = "user",
                parts = new Part[]
                {
                    new Part { text = _prompt }
                }
            };

            List<Content> contentsList = new List<Content>(_chatHistory);
            contentsList.Add(userContent);
            _chatHistory = contentsList.ToArray();

            AppendMessage(userContent);
            _button.enabled = false;
            _inputField.text = "";
            _inputField.enabled = false;

            ChatRequest chatRequest = new ChatRequest { contents = _chatHistory, systemInstruction = systemInstructionContent};

            string jsonData = JsonUtility.ToJson(chatRequest);

            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

            // Create a UnityWebRequest with the JSON data
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(jsonToSend);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error);
                }
                else
                {
                    Debug.Log("Request complete!");
                    Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                    if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                            //This is the response to your request
                            string reply = response.candidates[0].content.parts[0].text;
                            Content botContent = new Content
                            {
                                role = "model",
                                parts = new Part[]
                                {
                                    new Part { text = reply }
                                }
                            };
                            //This part adds the response to the chat history, for your next message
                            contentsList.Add(botContent);
                            _chatHistory = contentsList.ToArray();

                            AppendMessage(botContent);
                    }
                    else
                    {
                        Debug.Log("No text found.");
                    }
                }
                _button.enabled = true;
                _inputField.enabled = true;
            }  
        }
    }
}