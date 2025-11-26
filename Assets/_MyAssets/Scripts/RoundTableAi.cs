using MyScripts;                                                             // GameManager と同じ名前空間を使うための using

namespace MyScripts                                                          // プロジェクト既存と同じ名前空間の開始
{
    public sealed class RoundTableAiButton : MonoBehaviour                   // 円卓上の AI ボタン用コンポーネント定義
    {
        [SerializeField] private TMP_Dropdown convFirst;                     // 会話の1人目を選ぶ Dropdown への参照

        [SerializeField] private TMP_Dropdown convSecond;                    // 会話の2人目を選ぶ Dropdown への参照

        [SerializeField] private Button convSubmit;                          // 会話開始ボタン(convSubmit)への参照

        public enum AiId                                                    // このボタンがどの AI かを表す種類
        {
            A,                                                               // A キャラクター

            B,                                                               // B キャラクター

            C                                                                // C キャラクター
        }

        [SerializeField] private AiId aiId;                                  // このボタンが担当する AI の種類

        private Button _selfButton;                                          // 自分自身の Button コンポーネントを保持する変数

        private void Awake()                                                 // 起動時に一度だけ呼ばれるメソッド
        {
            _selfButton = GetComponent<Button>();                            // 自分のオブジェクトについている Button を取得

            if (_selfButton != null)                                         // Button が見つかったかチェック
            {
                _selfButton.onClick.AddListener(OnClickSeat);                // クリックされたときに OnClickSeat を呼ぶよう登録
            }
            else                                                              // Button が無かった場合
            {
                Debug.LogWarning("[RoundTableAiButton] Button が見つかりません。"); // Button 未設定の警告表示
            }
        }

        private void OnClickSeat()                                           // 円卓のこの席がクリックされたときの処理
        {
            if (convFirst == null || convSecond == null || convSubmit == null) // 必要な参照が全部設定されているか確認
            {
                Debug.LogWarning("[RoundTableAiButton] Dropdown や convSubmit が設定されていません。"); // 足りない場合は警告
                return;                                                      // これ以上の処理を行わず終了
            }

            convFirst.value = 0;                                             // 1人目の話者を「プレイヤー」に固定（インデックス0）

            convSecond.value = aiId switch                                   // 2人目の話者を A/B/C にセット
            {
                AiId.A => 1,                                                 // A のときは Dropdown インデックス 1

                AiId.B => 2,                                                 // B のときは Dropdown インデックス 2

                AiId.C => 3,                                                 // C のときは Dropdown インデックス 3

                _ => 1                                                       // 念のためのフォールバック（デフォルトで A）
            };

            convSubmit.onClick.Invoke();                                     // 既存の会話開始ボタンをコードから「押す」
        }

        private void OnDestroy()                                             // オブジェクト破棄時に呼ばれるメソッド
        {
            if (_selfButton != null)                                         // Button が存在するか確認
            {
                _selfButton.onClick.RemoveListener(OnClickSeat);            // クリックイベントの登録を解除
            }
        }
    }
}
