using MyScripts;                                                             

namespace MyScripts                                                          

{
    public sealed class RoundTableAiButton : MonoBehaviour                   
    
    {
        [SerializeField] private TMP_Dropdown convFirst;                     
        
        [SerializeField] private TMP_Dropdown convSecond;                   

        [SerializeField] private Button convSubmit;                         

        public enum AiId                                                    // このボタンがどの AI かを表す種類
        
        {
            A,                                                               // A キャラクター

            B,                                                               // B キャラクター

            C                                                                // C キャラクター
        }

        [SerializeField] private AiId aiId;                                  

        private Button _selfButton;                                          

        private void Awake()                                                 
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

        private void OnDestroy()                                             
        {
            if (_selfButton != null)                                         // Button が存在するか確認
            {
                _selfButton.onClick.RemoveListener(OnClickSeat);            // クリックイベントの登録を解除
            }
        }
    }
}
