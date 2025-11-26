using System.Collections.ObjectModel;                                   // コレクション系の型を使うためのusing

using Cysharp.Threading.Tasks;                                          // UniTaskを使うためのusing

using UnityEngine;                                                      // Debug.Logなどを使うためのusing

namespace MyScripts.Common                                              // プロジェクト用の名前空間
{
    public static class ApiHandler                                     // Firebaseなし環境用の簡易ApiHandlerクラス
    {
        private static readonly System.Collections.Generic.List<string> sessionHistory
            = new System.Collections.Generic.List<string>(256);        // 会話履歴を入れるための簡易List

        private static ReadOnlyCollection<string> _sessionHistoryCached = null; // キャッシュ用の変数

        public static ReadOnlyCollection<string> SessionHistory        // 履歴を外部に公開するプロパティ
            => _sessionHistoryCached ??= new ReadOnlyCollection<string>(sessionHistory);

        /// <summary>
        /// セッション履歴をリセットして、新しく会話を始める.
        /// </summary>
        public static void StartNewSession()                            // 会話履歴をリセットするメソッド
        {
            sessionHistory.Clear();                                     // これまでの履歴を全部消す
        }

        /// <summary>
        /// セッション内の履歴を踏まえて、会話を続ける(ダミー版).
        /// </summary>
        public static async UniTask<(bool, string)> AskAsync(string prompt, System.Threading.CancellationToken ct)
        // AIに質問するためのダミーメソッド(本物のAPI呼び出しはしない)
        {
            if (string.IsNullOrWhiteSpace(prompt))                      // 空文字かどうかをチェック
            {
                Debug.LogWarning("[ApiHandler] プロンプトが空なので、空の結果を返します。"); // 注意ログ

                return (false, string.Empty);                           // 失敗フラグと空文字を返す
            }

            sessionHistory.Add(prompt);                                 // とりあえず履歴リストにプロンプトを保存

            await UniTask.Yield();                                      // 非同期っぽさを保つために1フレーム待つ

            Debug.LogWarning("[ApiHandler] Firebase AI が導入されていないため、ダミー応答を返します。");
            // 本物のAIと通信していないことをコンソールに表示

            string dummyResponse = string.Empty;                        // ダミーの返事(今回は空文字)

            return (false, dummyResponse);                              // 「失敗」として空文字を返す
        }
    }
}
