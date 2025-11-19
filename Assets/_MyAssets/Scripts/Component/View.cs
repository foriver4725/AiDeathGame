using MyScripts.Interface;

namespace MyScripts.Component
{
    public sealed class View : MonoBehaviour, IView
    {
        [SerializeField] private Presenter _presenter;
        public IPresenter Presenter => _presenter;

        [SerializeField] private TMP_InputField _inputField;

        [SerializeField] private TextMeshProUGUI _outputText;

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
