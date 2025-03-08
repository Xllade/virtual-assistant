using Meta.WitAi;
using Meta.WitAi.Requests;
using TMPro;
using UnityEngine;

namespace VirtualAssistant
{
    public class VoiceTranscriptionInputFieldTMP : MonoBehaviour
    {
        // The label to be updated
        public TMP_InputField label
        {
            get
            {
                if (_label == null)
                {
                    _label = gameObject.GetComponent<TMP_InputField>();
                }
                return _label;
            }
        }
        private TMP_InputField _label;

        [Header("Listen Settings")]
        [Tooltip("Various voice services to be observed")]
        [SerializeField] private VoiceService[] _voiceServices;
        [Tooltip("Text color while receiving text")]
        [SerializeField] private Color _transcriptionColor = Color.black;

        [Header("Prompt Settings")]
        [Tooltip("Color to be used for prompt text")]
        [SerializeField] private Color _promptColor = new Color(0.2f, 0.2f, 0.2f);
        [Tooltip("Prompt text that displays while listening but prior to completion")]
        [SerializeField] private string _promptDefault = "Press activate to begin listening";
        [Tooltip("Prompt text that displays while listening but prior to completion")]
        [SerializeField] private string _promptListening = "Listening...";

        [Header("Error Settings")]
        [Tooltip("Color to be used for error text")]
        [SerializeField] private Color _errorColor = new Color(0.8f, 0.2f, 0.2f);

        // If none found, grab all voice services
        private void Awake()
        {
            if (_voiceServices == null || _voiceServices.Length == 0)
            {
                _voiceServices = FindObjectsOfType<VoiceService>();
            }
        }

        // Add service delegates
        private void OnEnable()
        {
            if (_voiceServices != null)
            {
                foreach (var service in _voiceServices)
                {
                    service.VoiceEvents.OnStartListening.AddListener(OnStartListening);
                    service.VoiceEvents.OnPartialTranscription.AddListener(OnTranscriptionChange);
                    service.VoiceEvents.OnFullTranscription.AddListener(OnTranscriptionChange);
                    service.VoiceEvents.OnError.AddListener(OnError);
                    service.VoiceEvents.OnComplete.AddListener(OnComplete);
                }
            }
        }
        // Remove service delegates
        private void OnDisable()
        {
            if (_voiceServices != null)
            {
                foreach (var service in _voiceServices)
                {
                    service.VoiceEvents.OnStartListening.RemoveListener(OnStartListening);
                    service.VoiceEvents.OnPartialTranscription.RemoveListener(OnTranscriptionChange);
                    service.VoiceEvents.OnFullTranscription.RemoveListener(OnTranscriptionChange);
                    service.VoiceEvents.OnError.RemoveListener(OnError);
                    service.VoiceEvents.OnComplete.RemoveListener(OnComplete);
                }
            }
        }

        #if UNITY_EDITOR
        // Refresh prompt
        private void Update()
        {
            if (!Application.isPlaying)
            {
                SetText(_promptDefault, _promptColor);
            }
        }
        #endif

        // Set listening
        private void OnStartListening()
        {
            SetText(_promptListening, _promptColor);
        }
        // Set text change
        private void OnTranscriptionChange(string text)
        {
            SetText(text, _transcriptionColor);
        }
        // Apply error
        private void OnError(string status, string error)
        {
            SetText($"[{status}] {error}", _errorColor);
        }
        // If no text came through, show prompt
        private void OnComplete(VoiceServiceRequest request)
        {
            if (label != null && string.Equals(label?.text, _promptListening))
            {
                SetText(_promptDefault, _promptColor);
            }
        }

        // Refresh text
        private void SetText(string newText, Color newColor)
        {
            // Ignore if same
            if (label == null || string.Equals(newText, label.text) && newColor == label.caretColor)
            {
                return;
            }

            // Apply text & color
            _label.text = newText;
            _label.caretColor = newColor;
        }
    }
}