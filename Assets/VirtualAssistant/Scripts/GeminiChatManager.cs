using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Meta.WitAi;
using Meta.WitAi.Requests;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

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

        [SerializeField, TextArea] private string _prompt = "";
        [SerializeField] private VoiceService _voiceService;
        [SerializeField] private TTSSpeaker _ttsSpeaker;
        [SerializeField] private GameObject _avatar;
        [SerializeField] private PromptMenu _promptMenu;
        [SerializeField] private TextMeshProUGUI _versionText;

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
            _promptMenu.SetPromptInputFieldText(_prompt);
            _promptMenu.gameObject.SetActive(true);
            _versionText.text = $"Version {Application.version}";
        }

        void OnEnable()
        {
            _inputField.onEndEdit.AddListener(OnEndEditInputField);
            _voiceService.VoiceEvents.OnStartListening.AddListener(OnStartListeningVoiceEvent);
            _voiceService.VoiceEvents.OnComplete.AddListener(OnCompleteVoiceEvent);
            _promptMenu.OnSetPrompt += OnSetPrompt;
        }

        void OnDisable()
        {
            _inputField.onEndEdit.RemoveListener(OnEndEditInputField);
            _voiceService.VoiceEvents.OnStartListening.RemoveListener(OnStartListeningVoiceEvent);
            _voiceService.VoiceEvents.OnComplete.RemoveListener(OnCompleteVoiceEvent);
            _promptMenu.OnSetPrompt -= OnSetPrompt;
        }

        private void OnEndEditInputField(string inputString)
        {
            if (Input.GetKeyDown(KeyCode.Return)) StartCoroutine(SendChatRequestToGemini(inputString));
        }

        private void OnStartListeningVoiceEvent()
        {
            _inputField.interactable = false;
        }

        private void OnCompleteVoiceEvent(VoiceServiceRequest request)
        {
            StartCoroutine(SendChatRequestToGemini(_inputField.text));
        }

        private void OnSetPrompt(string prompt)
        {
            _prompt = prompt;
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

        private IEnumerator SendChatRequestToGemini(string newMessage)
        {
            Debug.Log("chat: "+newMessage);
            if (string.IsNullOrWhiteSpace(newMessage))
            {
                EnableUI(true);
                yield break;
            }

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
            _inputField.text = "";
            EnableUI(false);
            _avatar.GetComponent<Animator>().SetBool("isListening", true);

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
                    EnableUI(true);
                }
                else
                {
                    Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                    if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        var message = response.candidates[0].content.parts[0].text;
                        var tTSSpeakerClipEvents = new TTSSpeakerClipEvents();
                        void OnLoadAbortTTS(TTSSpeaker tTSSpeaker, TTSClipData tTSClipData)
                        {
                            AddBotMessage(message);
                            EnableUI(true);
                        }
                        void OnLoadFailedTTS(TTSSpeaker tTSSpeaker, TTSClipData tTSClipData, string error)
                        {
                            AddBotMessage(message);
                            EnableUI(true);
                        }
                        void OnLoadSuccessTTS(TTSSpeaker tTSSpeaker, TTSClipData tTSClipData)
                        {
                            AddBotMessage(message);
                            _avatar.GetComponent<AudioSource>().clip = tTSClipData.clip;
                            _avatar.GetComponent<AudioSource>().Play();
                            _avatar.GetComponent<Animator>().SetBool("isListening", false);
                            _avatar.GetComponent<Animator>().SetBool("isTalking", true);
                        }
                        void OnPlaybackCompleteTTS(TTSSpeaker tTSSpeaker, TTSClipData tTSClipData)
                        {
                            EnableUI(true);
                        }
                        tTSSpeakerClipEvents.OnLoadAbort.AddListener(OnLoadAbortTTS);
                        tTSSpeakerClipEvents.OnLoadFailed.AddListener(OnLoadFailedTTS);
                        tTSSpeakerClipEvents.OnLoadSuccess.AddListener(OnLoadSuccessTTS);
                        tTSSpeakerClipEvents.OnPlaybackComplete.AddListener(OnPlaybackCompleteTTS);
                        _ttsSpeaker.Speak(message, tTSSpeakerClipEvents);
                    }
                    else
                    {
                        Debug.Log("No text found.");
                        EnableUI(true);
                    }
                }
            }  
        }

        private void AddBotMessage(string message)
        {
            Debug.Log($"bot: {message}");
            Content botContent = new Content
            {
                role = "model",
                parts = new Part[]
                {
                    new Part { text = message }
                }
            };
            List<Content> contentsList = new List<Content>(_chatHistory);
            contentsList.Add(botContent);
            _chatHistory = contentsList.ToArray();
            AppendMessage(botContent);
        }

        private void EnableUI(bool enable)
        {
            _button.interactable = _inputField.interactable = enable;
            if (enable)
            {
                _avatar.GetComponent<Animator>().SetBool("isListening", false);
                _avatar.GetComponent<Animator>().SetBool("isTalking", false);
            }
        }
    }
}