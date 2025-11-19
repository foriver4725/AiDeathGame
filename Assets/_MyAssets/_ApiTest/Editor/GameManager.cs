#if !UNITY_EDITOR
#error "This script is intended to be used in the Unity Editor only."
#endif

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Ct = System.Threading.CancellationToken;

using MyScripts.Common;
using MyScripts.Component;

namespace MyScripts._ApiTest
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TextMeshProUGUI convText;

        [SerializeField] private View view;

        private void Start() => ImplAsync(destroyCancellationToken).Forget();

        private async UniTaskVoid ImplAsync(Ct ct)
        {
            while (!ct.IsCancellationRequested)
            {
                int buttonIndex = await UniTask.WhenAny(
                    sendButton.OnClickAsync(ct),
                    resetButton.OnClickAsync(ct)
                );

                if (buttonIndex == 0)
                {
                    // 送信ボタンが押された
                    string prompt = inputField.text.Trim();
                    if (string.IsNullOrEmpty(prompt))
                    {
                        Debug.LogError("プロンプトが空です。");
                        continue;
                    }

                    // APIに問い合わせ
                    var (success, response) = await ApiHandler.AskAsync(prompt, ct);
                    if (success)
                    {
                        convText.text = response;
                        inputField.text = string.Empty; // 入力フィールドをクリア
                    }
                    else
                    {
                        Debug.LogError("APIからのレスポンスが失敗しました。");
                    }
                }
                else if (buttonIndex == 1)
                {
                    // リセットボタンが押された
                    ApiHandler.StartNewSession();
                    convText.text = string.Empty; // 会話履歴をクリア
                }
            }
        }
    }
}
