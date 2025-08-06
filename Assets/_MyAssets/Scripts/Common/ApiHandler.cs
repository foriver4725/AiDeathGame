using Firebase;
using Firebase.AI;

namespace MyScripts.Common;

public static class ApiHandler
{
    private static readonly string ModelName = "gemini-2.0-flash";
    private static readonly string UserRoleName = "user";
    private static readonly string ModelRoleName = "model";

    private static GenerativeModel model = null;
    private static readonly List<ModelContent> sessionHistory = new(256);

    private static ReadOnlyCollection<ModelContent> SessionHistoryCached = null;
    public static ReadOnlyCollection<ModelContent> SessionHistory => SessionHistoryCached ??= new(sessionHistory);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Geminiモデルの呼び出し
        var ai = FirebaseAI.GetInstance(FirebaseAI.Backend.GoogleAI());
        model = ai.GetGenerativeModel(modelName: ModelName);
    }

    /// <summary>
    /// セッション履歴をリセットして、新しく会話を始める.
    /// </summary>
    public static void StartNewSession()
    {
        sessionHistory.Clear();
    }

    /// <summary>
    /// セッション内の履歴を踏まえて、会話を続ける.
    /// </summary>
    public static async UniTask<(bool, string)> AskAsync(string prompt, Ct ct)
    {
        // 生成AIにリクエストを投げ、レスポンスを受け取る
        try
        {
            if (model == null)
                throw new InvalidOperationException("APIモデルが初期化されていません。");

            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("プロンプトが空または無効です。", nameof(prompt));

            // セッション履歴にプロンプトを追加
            sessionHistory.Add(new ModelContent(
                UserRoleName,
                new ModelContent.Part[] { new ModelContent.TextPart(prompt) }
            ));

            // プロンプトを送信、返答を受け取る
            var response = await model.GenerateContentAsync(sessionHistory, ct).AsUniTask();
            string responseText = response.Text;

            // レスポンスをセッション履歴に追加
            sessionHistory.Add(new ModelContent(
                ModelRoleName,
                new ModelContent.Part[] { new ModelContent.TextPart(responseText) }
            ));

            if (string.IsNullOrWhiteSpace(responseText))
                throw new Exception("APIからのレスポンスが空です。");

            return (true, responseText);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"API呼び出し中にエラーが発生しました: {e.Message}");
            return (false, string.Empty);
        }
    }
}