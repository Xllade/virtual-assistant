using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VirtualAssistant
{
    public class PromptMenu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _button;
        public UnityAction<string> OnSetPrompt;

        void Start()
        {
            _button.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                OnSetPrompt?.Invoke(_inputField.text);
            });
        }

        public void SetPromptInputFieldText(string text)
        {
            _inputField.text = text;
        }
    }
}