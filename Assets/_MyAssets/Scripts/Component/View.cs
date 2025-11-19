using MyScripts.Interface;

namespace MyScripts.Component
{
    public sealed class View : MonoBehaviour, IView
    {
        [SerializeField] private Presenter _presenter;
        public IPresenter Presenter => _presenter;

        [SerializeField] private TMP_InputField _inputField;

        [SerializeField] private TextMeshProUGUI _outputText;

        [SerializeField] private Button _sendButton;

        public event Action<IViewData> OnSend;                 // メッセージ送信を外部に知らせるイベント

        private sealed class ViewData : IViewData                 
        {
            public Speaker Speaker { get; }                       
            public string Message { get; }                        

            public ViewData(Speaker speaker, string message)      // コンストラクタで中身を設定
            {
                Speaker = speaker;                                // 引数speakerをプロパティに保存
                Message = message;                                // 引数messageをプロパティに保存
            }
        }

        public void OnSendFromPresenter(IViewData input)
        {
            // 返答を受け取った時に実行される
            if (_outputText != null)                              // 出力先のTextが設定されているか確認
            {
                _outputText.text = input.Message;                 // 受け取ったメッセージ本文を画面に表示
            }
        }

        private void Awake()                                                // 最初に一度だけUI設定を行う
        {

            if (_inputField != null)                                       // InputFieldが設定されているか確認
            {
                _inputField.lineType = TMP_InputField.LineType.MultiLineNewline; // 複数行入力を許可

                var tmp = _inputField;                                     // 参照を短い変数にコピー

                if (tmp.textComponent != null)                             // 実際に文字を表示するText部分を取得
                {
                    var t = tmp.textComponent;                             // TextMeshProの本体
                    t.textWrappingMode = TMPro.TextWrappingModes.Normal;   // 自動改行を有効化

                    t.overflowMode = TMPro.TextOverflowModes.Masking;      // 枠からはみ出した部分をマスク
                }

                if (tmp.textViewport != null)                              // 入力部分のビュー（表示枠）を取得
                {
                    var m = tmp.textViewport.GetComponent<RectMask2D>();   // マスク用コンポーネントを探す
                    if (m == null) m = tmp.textViewport.gameObject.AddComponent<RectMask2D>(); // 無ければ追加

                    m.enabled = true;                                      // マスクを有効化
                }

                _inputField.onSubmit.RemoveAllListeners();                 // 既存のSubmitイベントを全て解除

                _inputField.onValidateInput += (text, index, ch) =>        // 入力文字のバリデーション設定
                {
                    if (ch == '\n' || ch == '\r' || ch == '\t') return '\0'; // 改行・タブを無効化

                    return ch;                                             // それ以外の文字はそのまま許可
                };

                var nav = _inputField.navigation;                          // UIナビゲーション設定を取得

                nav.mode = Navigation.Mode.None;                           // Tabなどでフォーカスを移動させない

                _inputField.navigation = nav;                              // 変更した設定を反映
            }

            if (_sendButton != null)                                       // 送信ボタンが設定されているか確認
            {
                var navBtn = _sendButton.navigation;                       // ボタンのナビゲーション設定を取得
                navBtn.mode = Navigation.Mode.None;                        // 矢印キーなどでフォーカス移動させない

                _sendButton.navigation = navBtn;                           // 変更を適用

                _sendButton.onClick.RemoveAllListeners();                  // 既存のOnClickを一度クリア

                _sendButton.onClick.AddListener(OnSendToPresenter);        // クリック時に自分の送信処理を呼ぶ
            }
            else
            {
                Debug.LogWarning("[View] _sendButton が設定されていません。"); // 設定漏れの警告
            }
        }


        private IViewData GetViewData()
        {
            string text = string.Empty;                           // 入力欄のテキストを一旦入れる変数
            if (_inputField != null)                              // InputField がアサインされているか確認
            {
                text = _inputField.text;                          // プレイヤーが入力した文字列を取得
            }

            if (string.IsNullOrWhiteSpace(text))                  // 空文字やスペースだけの場合は
            {
                return null;                                      // 無効として null を返す
            }

            return new ViewData(Speaker.Player, text);            // プレイヤー名＋メッセージでViewDataを作成
        }

        public void OnSendToPresenter()                           // ボタンから呼ぶために public に変更
        {
            // プレイヤーの操作を送りたい時に実行する

            IViewData viewData = GetViewData();                   // 現在の入力内容を IViewData に変換

            if (viewData == null)                                // 入力が空などで無効なら
            {
                Debug.LogWarning("[View] 入力が空のため送信しませんでした。"); // 送信せず警告を出す
                return;                                           
            }

            if (Presenter != null)                                // Presenter が割り当てられている場合だけ呼ぶ
            {
                Presenter.OnSendFromView(viewData);               // Presenter に「プレイヤーがこう言いました」と通知
            }

            if (OnSend == null)                                  // 外部イベント購読者の有無をチェック
            {
                Debug.LogWarning("[View] OnSend に購読者がいません(GameManagerが登録されていない可能性)。"); //
            }

            OnSend?.Invoke(viewData);                             // 外部（GameManagerなど）にもイベントで通知

            if (_inputField != null)                              // 送信後、入力欄があれば
            {
                _inputField.text = string.Empty;                  // 入力欄を空にリセットする
            }
        }
    }
}
