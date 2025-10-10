using MyScripts.Common;
using System.Globalization;
using System.Text;
using UnityEngine.EventSystems;

namespace MyScripts
{

    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown convFirst;
        [SerializeField] private TMP_Dropdown convSecond;
        [SerializeField] private Button convSubmit;

        [SerializeField] private Canvas talkCanvas;
        [SerializeField] private TextMeshProUGUI talkText;

        // LayoutGroupObject
        [SerializeField] private Transform talkContentRoot;

        // 吹き出しプレハブ（緑=プレイヤー、白=相手）
        [SerializeField] private GameObject playerBubblePrefab;
        [SerializeField] private GameObject aiBubblePrefab;

        //吹き出し表示位置調整用余白
        [SerializeField] private int leftEdgePadding = 8;  // 白(左寄せ)の左端余白
        [SerializeField] private int rightEdgePadding = 8;  // 緑(右寄せ)の右端余白
        [SerializeField] private int rowTopBottom = 4;  // 行の上下余白

        // 自動スクロール
        [SerializeField] private ScrollRect talkScroll;

        //文字数カウント
        [SerializeField] private int SentenceLimit = 30;

        [SerializeField] private TextMeshProUGUI quesText;
        [SerializeField] private TMP_Dropdown ansA;
        [SerializeField] private TMP_Dropdown ansB;
        [SerializeField] private TMP_Dropdown ansC;
        [SerializeField] private Button ansSubmit;

        // Player会話
        [SerializeField] private TMP_InputField playerInputField;  // プレイヤーが入力する欄
        [SerializeField] private Button playerSendButton;          // プレイヤーが送信するボタン

        // 戻るボタン
        [SerializeField] private Button backButton;
        private Cts _talkCts;                           // 会話再生専用のキャンセル用スイッチ

        // Icon画像
        [System.Serializable]
        public struct SpeakerIcon
        {
            public string speaker;   // 会話者
            public Sprite sprite;    // 会話者アイコン
        }

        [Header("Icon Settings")]
        [SerializeField] private List<SpeakerIcon> iconTable;
        [SerializeField] private Sprite defaultIcon;           // 見つからない時の代替Icon
        [SerializeField] private int iconSize = 64;            // 表示サイズ(px)
        private Dictionary<string, Sprite> iconMap;


        private readonly List<string> answerOptions = new(8);

        private void Start() => FlowAsync(destroyCancellationToken).Forget();

        private async UniTaskVoid FlowAsync(Ct ct)
        {
            // 初期化
            {
                talkCanvas.gameObject.SetActive(false);

                // 問題と選択肢を取得し、UIにセット
                var data = SData.Instance.GetData(SceneManager.Now);
                if (data == null)
                {
                    Debug.LogError("No data found for the current scene.");
                    return;
                }
                quesText.text = data.Question;
                answerOptions.Clear();
                answerOptions.AddRange(data.Answers);
                ansA.ClearOptions();
                ansB.ClearOptions();
                ansC.ClearOptions();
                ansA.AddOptions(answerOptions);
                ansB.AddOptions(answerOptions);
                ansC.AddOptions(answerOptions);

                // 前提条件を与え、会話の準備をする
                await BeginConvAsync(ct);
            }

            while (!ct.IsCancellationRequested)
            {
                int placedButtonIndex = await UniTask.WhenAny(
                    convSubmit.OnClickAsync(ct),
                    ansSubmit.OnClickAsync(ct)
                );

                if (placedButtonIndex == 0)
                {
                    // 会話ボタンが押された

                    // 話し手の名前を取得
                    string firstTalker = GetTalkersName(convFirst.value);
                    string secondTalker = GetTalkersName(convSecond.value);
                    if (string.IsNullOrEmpty(firstTalker) || string.IsNullOrEmpty(secondTalker))
                    {
                        Debug.LogError("Invalid talker selection.");
                        continue;
                    }

                    // 会話データを生成させる
                    List<string> talkList = await AskForTalkListAsync(firstTalker, secondTalker, ct);

                    ClearBubbles();

                    _talkCts?.Cancel();                             // 進行中があれば一旦停止
                    _talkCts?.Dispose();                            // リソース破棄
                    _talkCts = new Cts();

                    // 会話を再生、終わったらUIオフ
                    talkCanvas.gameObject.SetActive(true);
                    {
                        try
                        {
                            for (int i = 0; i < talkList.Count; i++) // プレイヤー発言の直後に「次の話者」を参照できるようにするため、添字付きループ
                            {
                                string talk = talkList[i];                                 // その回の台本行を取得
                                int colon = talk.IndexOf(':');                              // 「話者: 内容」を分割するコロン位置を探す
                                string speaker = colon >= 0 ? talk.Substring(0, colon).Trim() : string.Empty; // 話者名（A/B/C/プレイヤー）を取り出す
                                string message = colon >= 0 ? talk.Substring(colon + 1).Trim() : talk;       // 台詞本文を取り出す

                                if (speaker == "プレイヤー")                                              // プレイヤーの番なら入力UI表示
                                {
                                    playerInputField.gameObject.SetActive(true);                            // 入力欄を表示
                                    playerSendButton.gameObject.SetActive(true);                            // 送信ボタンを表示
                                    var es = EventSystem.current;                                           // EventSystem を取得
                                    bool prevNav = true;                                                    // 元の設定を退避
                                    if (es != null)
                                    {
                                        prevNav = es.sendNavigationEvents;                                   // 現在のナビゲーション有効/無効フラグを保存
                                        es.sendNavigationEvents = false;                                     // Submit/Tab 等のナビゲーション入力を全停止
                                        es.SetSelectedGameObject(playerInputField.gameObject);               // 選択対象を入力欄に固定
                                        playerInputField.ActivateInputField();
                                    }

                                    try
                                    {
                                        var linked = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ct, _talkCts.Token); // 戻るでも中断できるよう結合トークンを作成
                                        await playerSendButton.OnClickAsync(linked.Token);
                                        linked.Dispose(); // 作成した結合トークンを破棄
                                    }
                                    finally
                                    {
                                        if (es != null) es.sendNavigationEvents = prevNav; // 既存の復元はそのまま
                                    }

                                    string playerMessage = playerInputField.text;                           // 入力テキスト取得
                                    playerInputField.text = string.Empty;                                   // 入力欄をリセット
                                    await AddBubbleAsync("プレイヤー", playerMessage, 0.20f);               // プレイヤーの吹き出しを表示

                                    // 次の行の予定話者に答えさせる
                                    string nextSpeaker = "A";                                               // フォールバック（最後の行などに備えてAを初期値）
                                    if (i + 1 < talkList.Count)                                             // 次の行が存在するかチェック
                                    {
                                        string nextLine = talkList[i + 1];                                  // 次の台本行を参照
                                        int c2 = nextLine.IndexOf(':');                                     // 次の行のコロン位置
                                        if (c2 >= 0) nextSpeaker = nextLine.Substring(0, c2).Trim();        // 次の行の話者名を抽出（A/B/C想定）
                                        i++;                                                                // 台本のその行は“差し替え”るのでスキップ
                                    }

                                    // APIへ「nextSpeaker として1行だけ返答せよ」という指示を送る
                                    string rolePrompt =
                                        $"### 指示\n" +                                                     // 役割を明示するプロンプトを組み立て
                                        $"あなたは今から「{nextSpeaker}」として日本語で1行だけ返答します。\n" +  // 返答者の役割を固定
                                        $"出力形式は次の1行のみ：\n" +                                        // 出力形式を厳格に指定
                                        $"{nextSpeaker}: {{返答}} :ONE-END\n\n" +                            // パースしやすい目印を付ける
                                        $"### プレイヤーの質問\n{playerMessage}";                           // プレイヤーの質問本文を添付

                                    (bool ok, string raw) = await ApiHandler.AskAsync(rolePrompt, ct);      // 役割固定プロンプトでAPI呼び出し

                                    if (ok)                                                                  // 正常に返ってきたときだけ表示
                                    {
                                        string aiReply = ExtractRoleLine(raw, nextSpeaker);                 // "X: 本文 :ONE-END" から本文だけを抽出
                                        await AddBubbleAsync(nextSpeaker, aiReply, 0.20f);                   // A/B/C の吹き出しで出力
                                    }

                                    playerInputField.gameObject.SetActive(false);                           // 入力欄を隠す
                                    playerSendButton.gameObject.SetActive(false);                           // 送信ボタンを隠す
                                }
                                else
                                {
                                    await AddBubbleAsync(speaker, message, 0.20f);                           // A/B/C 等の通常行はそのまま表示
                                }

                                //Color設定
                                ulong colorHex = talk[0] switch
                                {

                                    'A' => 0x000000,
                                    'B' => 0x000000,
                                    'C' => 0x000000,
                                    _ => 0x000000

                                    /*
                                    'A' => 0xCF3030,
                                    'B' => 0xB0CF3A,
                                    'C' => 0x3B82B9,
                                    _ => 0xFFC700
                                    */
                                };
                                Color color = new Color32(
                                    (byte)(colorHex >> 16),
                                    (byte)(colorHex >> 8 & 0xFF),
                                    (byte)(colorHex & 0xFF),
                                    0xFF);

                                // 最初の半角スペースより左にあるものを削除
                                int spaceIndex = talk.IndexOf(' ');
                                string content = spaceIndex >= 0 ? talk.Substring(spaceIndex + 1) : talk;

                                // 15文字ごとに改行を追加
                                content = InsertLineBreaks(content, SentenceLimit);

                                //talkText.text = content;
                                //talkText.color = color;



                                // 1行ずつ進めたい演出を踏襲
                                await UniTask.WaitUntil(
                                    () => Input.GetMouseButtonDown(0) || Input.touchCount > 0,
                                    cancellationToken: _talkCts.Token // 戻るで中断
                                );

                            }
                        }

                        catch (OperationCanceledException) // 戻るによる中断を想定
                        {
                            Debug.Log("会話再生を中断しました（戻る）。");
                        }
                    }

                    ClearBubbles();

                    talkCanvas.gameObject.SetActive(false);

                    continue;
                }
                else if (placedButtonIndex == 1)
                {
                    // 解答ボタンが押された

                    // それぞれの答えを生成させる
                    (byte corA, byte corB, byte corC) = await AskForAnswerAsync(ct);

                    // 正誤判定して、対応するシーンに遷移
                    bool result = ansA.value == corA &&
                                  ansB.value == corB &&
                                  ansC.value == corC;
                    (result ? SceneId.Right : SceneId.Wrong).LoadAsync();

                    continue;
                }
                else
                {
                    Debug.LogError("Unexpected button index: " + placedButtonIndex);
                    continue;
                }
            }
        }

        private static string GetTalkersName(int dropdownIndex) => dropdownIndex switch
        {
            0 => "プレイヤー",
            1 => "A",
            2 => "B",
            3 => "C",
            _ => string.Empty
        };

        private async UniTask AddBubbleAsync(string speaker, string message, float scrollDuration = 0.20f)
        {
            var prefab = PickBubblePrefab(speaker);
            bool isGreen = ReferenceEquals(prefab, playerBubblePrefab);

            // 1行コンテナ
            var rowGO = new GameObject("Row",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            var row = rowGO.GetComponent<RectTransform>();
            row.SetParent(talkContentRoot, false);

            // Row を親の幅いっぱいにストレッチ（最初の1個目でも中央化しない）
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);

            // 左右オフセットを明示的に0へ（sizeDelta.xも0にして“ストレッチ幅”だけにする）
            row.offsetMin = new Vector2(0f, row.offsetMin.y);
            row.offsetMax = new Vector2(0f, row.offsetMax.y);
            row.sizeDelta = new Vector2(0f, row.sizeDelta.y);

            // 横いっぱいに広げるヒントとして LayoutElement も付与（無ければ追加）
            var rowLE = rowGO.GetComponent<LayoutElement>() ?? rowGO.AddComponent<LayoutElement>();
            rowLE.flexibleWidth = 1f;


            var h = rowGO.GetComponent<HorizontalLayoutGroup>();
            h.childControlWidth = false;   // 子の幅は子側で決める
            h.childControlHeight = true;   // 高さは子に合わせる
            h.childForceExpandWidth = false;    // 行幅いっぱいに使う
            h.childForceExpandHeight = false;
            h.spacing = 8;

            // 左右エッジ余白をスピーカーで出し分け
            h.padding = new RectOffset(
                 isGreen ? 0 : leftEdgePadding,
                 isGreen ? rightEdgePadding : 0,
                 rowTopBottom, rowTopBottom
            );


            // Icon を Row の直下に生成（Bubbleと並列）
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.SetParent(row, false);
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.sprite = ResolveIcon(speaker);                 // 会話者→画像
            var iconLE = iconGO.GetComponent<LayoutElement>();
            iconLE.preferredWidth = iconSize;
            iconLE.preferredHeight = iconSize;
            iconLE.flexibleWidth = 0;

            // BubbleをRowの子として生成
            var bubble = Instantiate(prefab, row, false);
            SetupBubble(bubble, message);
            // Bubbleが横に伸びないようFlexible指定
            var bubbleLE = bubble.GetComponent<LayoutElement>() ?? bubble.AddComponent<LayoutElement>();
            bubbleLE.flexibleWidth = 0f;


            // 左右に応じて“並び順”と寄せ方向を決める
            if (isGreen)
            {
                h.childAlignment = TextAnchor.UpperRight;
                iconRt.SetSiblingIndex(row.childCount - 1);        // 右寄せ: [Bubble][Icon]
            }
            else
            {
                h.childAlignment = TextAnchor.UpperLeft;
                iconRt.SetSiblingIndex(0);                         // 左寄せ: [Icon][Bubble]
            }



            //if (talkText != null) talkText.text = InsertLineBreaks(message, SentenceLimit);
            await SmoothScroll(scrollDuration);
        }

        // TextObjectに会話反映
        private void SetupBubble(GameObject go, string message)
        {
            string formatted = InsertLineBreaks(message, SentenceLimit);
            var text = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = formatted;
        }

        // 横方向の可変余白（Flexible=1）を作る
        private static void AddFlexibleSpacer(Transform parent)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            le.minWidth = 0;
            le.preferredWidth = 0;
            le.flexibleWidth = 1f; // これが“押し出す力”
        }





        private static Color32 GetColorForSpeaker(string speaker)
        {
            return speaker switch
            {
                "A" => Hex(0x000000),
                "B" => Hex(0x000000),
                "C" => Hex(0x000000),
                "プレイヤー" => Hex(0x000000),
                _ => new Color32(255, 255, 255, 255),
            };
        }
        private static Color32 Hex(uint hex) 
        {
           return new Color32(
                (byte)(hex >> 16),
                (byte)((hex >> 8) & 0xFF),
                (byte)(hex & 0xFF),
                0xFF
            );
        }
        // Bubble削除
        private void ClearBubbles()
        {
            // 末尾から消すと安全
            for (int i = talkContentRoot.childCount - 1; i >= 0; i--)
            {
                var child = talkContentRoot.GetChild(i);
                Destroy(child.gameObject);
            }

            // レイアウト更新
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)talkContentRoot);
        }

        // スクロール機能
        private async UniTask SmoothScroll(float duration = 0.20f)
        {
            if (talkScroll == null || talkScroll.content == null) return;

            // // レイアウトを確定させて高さを最新化
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)talkContentRoot);

            // ScrollRect の正規化スクロール値（1=最上 / 0=最下）
            float start = talkScroll.verticalNormalizedPosition;
            float end = 0f;

            // duration=0 や高さ不足でスクロール領域が無い場合は即時反映
            var content = talkScroll.content;
            var viewport = talkScroll.viewport != null ? talkScroll.viewport : (RectTransform)talkScroll.transform;
            float contentH = content.rect.height;
            float viewH = viewport.rect.height;
            if (duration <= 0f || contentH <= viewH || Mathf.Approximately(start, end))
            {
                talkScroll.verticalNormalizedPosition = end;
                return;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = 1f - (1f - Mathf.Clamp01(t / duration)) * (1f - Mathf.Clamp01(t / duration));
                talkScroll.verticalNormalizedPosition = Mathf.Lerp(start, end, u);
                await UniTask.Yield();
            }
            talkScroll.verticalNormalizedPosition = end;
        }


        /// <summary>
        /// 指定文字数ごとに改行を挿入する
        /// </summary>
        private string InsertLineBreaks(string text, int limit)
        {
            if (string.IsNullOrEmpty(text)) return text;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                sb.Append(text[i]);
                if ((i + 1) % limit == 0 && i != text.Length - 1)
                {
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        private static string ExtractRoleLine(string raw, string role)                 // 役割付き1行出力から本文だけを抜き出す
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;                        // 空なら空を返す
            string s = raw.Trim('`').Trim();                                           // 不要な囲みや空白を除去
            string head = role + ":";                                                  // 先頭印（例: "A:"）
            int p = s.IndexOf(head, StringComparison.Ordinal);                         // "A:" の開始位置
            if (p >= 0)                                                                
            {
                int bodyStart = p + head.Length;                                       // 本文の開始位置
                int end = s.IndexOf(":ONE-END", bodyStart, StringComparison.Ordinal);  // 終端マーカーを探す
                string body = (end >= 0) ? s.Substring(bodyStart, end - bodyStart)     // 本文だけを取り出す
                                         : s.Substring(bodyStart);
                return body.Trim();                                                    // 前後空白を削って返す
            }

            return s;                                                                  // 想定外形式のときは全体を返す
        }



        //「誰が話したかの判定」を毎メッセージごとに正規化

        // 文字列を比較しやすい形に正規化
        private static string NormalizeSpeaker(string raw)
            {
                if (string.IsNullOrEmpty(raw)) return string.Empty;

                var s = raw.Normalize(NormalizationForm.FormKC); // 全角/半角のゆらぎ除去
                s = s.Replace("\u3000", "");                     // 全角スペース除去
                s = s.Trim();                                    // 前後空白除去
                return s;
            }

            // 話者名→使用する吹き出しPrefab を決定
            private GameObject PickBubblePrefab(string speakerName)
            {
                var s = NormalizeSpeaker(speakerName);

                // 「プレイヤー」以外はすべてAI（A, B, C）を白に統一
                if (s == "プレイヤー" || s.Equals("player", System.StringComparison.OrdinalIgnoreCase) || s == "P")
                {
                    return playerBubblePrefab; // 緑
                }

                return aiBubblePrefab; // 白
            }

        private void Awake() 
        {
            // アイコンのルックアップを作成
            iconMap = new Dictionary<string, Sprite>();
            if (iconTable != null)
            {
                foreach (var e in iconTable)
                {
                    if (!string.IsNullOrEmpty(e.speaker) && e.sprite != null)
                    {
                        iconMap[e.speaker] = e.sprite;
                    }
                }
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                {
                    _talkCts?.Cancel();                                              // 進行中の会話待機をまとめて中断
                    if (playerInputField != null) playerInputField.gameObject.SetActive(false); // 入力UIを隠す
                    if (playerSendButton != null) playerSendButton.gameObject.SetActive(false); // 送信ボタンを隠す
                    if (playerInputField != null) playerInputField.text = string.Empty;         // 入力テキストをクリア
                    var es = EventSystem.current;                                               // 選択状態の解除
                    if (es != null) es.SetSelectedGameObject(null);                             
                    ClearBubbles();                         // 吹き出し全消し
                    talkCanvas.gameObject.SetActive(false); // キャンバス非表示
                });
            }


            if (playerInputField != null)
            {                                                                

                playerInputField.lineType = TMP_InputField.LineType.MultiLineNewline;                       
                playerInputField.onSubmit.RemoveAllListeners();                                             
                playerInputField.onValidateInput += (text, index, ch) => {                                  
                    if (ch == '\n' || ch == '\r' || ch == '\t') return '\0';                                

                    return ch;                                                                              
                };

                var nav = playerInputField.navigation;                                                      // UIナビゲーション設定の取得

                if (playerSendButton != null)                                           // 送信ボタンのナビゲーションを無効化
                {
                    var navBtn = playerSendButton.navigation;                            // 現在のナビ設定を取得
                    navBtn.mode = Navigation.Mode.None;                                  // ナビゲーションを無効
                    playerSendButton.navigation = navBtn;                                // 設定を反映
                }


                nav.mode = Navigation.Mode.None;                                                            // Tabでフォーカスを移動させない
                playerInputField.navigation = nav;                                                          // 変更を適用
            }





        }

        // Icon取得
        private Sprite ResolveIcon(string speaker) 
        {
            if (!string.IsNullOrEmpty(speaker) && iconMap != null && iconMap.TryGetValue(speaker, out var s))
                return s;
            return defaultIcon;
        }



        private static async UniTask BeginConvAsync(Ct ct)
        {
            ApiHandler.StartNewSession();

            (bool success, string response) = await ApiHandler.AskAsync(
                $$"""
                ### このセッションにおける、前提条件
                あなたには、今から3人のキャラクターに成り切って、自然に会話してもらいます。
                この3人のキャラクター達には、それぞれ性格に差異があります。
                しかし、この差異は、性格の本質的なパラメーターの微妙な差異によって顕現しているものであり、
                明確にキャラクター達の性格が区別されるような会話は、大いに不自然だと感じられます。
                従って、あなたは、キャラクター達の性格の差異を意識しつつも、
                それを明確に表すことを目標にするのではなく、個々の性格に沿った、可能な限り自然な会話を行うようにしてください。
                また、3人のキャラクターは、それぞれ必ず「A」「B」「C」と呼称されます。
                各プロンプト（このプロンプトも含める）において、「出力内容」の項に基づく出力を行なってください。
                また、もしプロンプトに含まれているならば、「出力フォーマット」「出力する際の重要事項」といった項も、忠実に確認してください。

                ### 出力内容
                A,B,Cに共通する、本質的な性格パラメーターを定義し、それぞれのキャラクターがその特性に何%当てはまるか、決定してください。
                性格パラメーターの数は、4-5個程度が望ましいと考えられますが、あなたの自己判断で多少増減させても構いません。
                注意点として、例えば「論理的思考」「行動性」「好奇心」のような、一般的な組み合わせを出力させることは避けてください。
                なぜならば、本セッションの試行を何回も繰り返した時、出力される結果が似通ってしまい、あまりにも一般的であるからです。
                本セッションにおけるあなたの出力を通して、我々は「人間の共通点・相違点」を同時に観察し、それらから生まれるバリエーションに注目したいと考えています。
                そのため、この考察に堪えるような、奇抜でなく、かつ独自性のある性格パラメーターを定義してください。
                「出力フォーマット」の項を参考に、出力してください。

                ### 出力フォーマット
                ```
                A: {パラメーター名1}:{パーセンテージ1}%, {パラメーター名2}:{パーセンテージ2}%, ... :A-END
                B: {パラメーター名1}:{パーセンテージ1}%, {パラメーター名2}:{パーセンテージ2}%, ... :B-END
                C: {パラメーター名1}:{パーセンテージ1}%, {パラメーター名2}:{パーセンテージ2}%, ... :C-END
                ```

                ### 出力する際の重要事項
                必ず「出力フォーマット」の項で与えられた形式で出力し、それ以外のいかなるメッセージ・文字列・文字も、出力しないでください。
                この条件は、本セッションの意義に関わる非常に重要な内容であり、いかなる状況においても遵守されるべきです。
                また、出力フォーマットの記述内容は、必ず「```」で囲まれています。
                これは出力する文字列の両端を明確に定義するためのものであり、従ってこの囲み文字を出力内容に含める必要はありません。
                """
                , ct);
            if (!success)
            {
                Debug.LogError("会話の開始に失敗しました。");
                return;
            }

            Debug.Log($"前提条件の設定に成功しました。\n【レスポンス】\n{response}");
        }

        //TODO: プレイヤーからの入力はもらわず、事前に全て生成してしまう
        private static async UniTask<List<string>> AskForTalkListAsync(string firstTalker, string secondTalker, Ct ct)
        {
            (bool success, string response) = await ApiHandler.AskAsync(
                $$"""
                ### 出力内容
                あなたにはこれから、「発言者」の項に示された、2人のキャラクターによる会話をシミュレートしてもらいます。
                会話の主題は任意ですが、A,B,Cの性格定義で述べたことと同様に、奇抜すぎず・かつ一般的すぎない、適切な主題を選択してください。
                各キャラクターの会話内容は日本語で出力し、語尾などについても、会話内容と同様に、あなたが適切だと考えるものを選択してください。
                出力する会話分の長さは、5-10往復、即ち発言回数の合計で言うと10-20回程度が望ましいと考えられますが、あなたの自己判断で多少増減させても構いません。
                どちらが最初に発言するかは、あなたの自己判断で決めてください。「発言者」の項における順番は、一切関係ありません。
                「出力フォーマット」の項を参考に、出力してください。
                また、会話をするキャラクターの中には、「A,B,C」以外に、「プレイヤー」が存在している可能性があります。
                この「プレイヤー」は、本セクションを観察している我々の、いわば分身です。
                既に性格を定義しているA,B,Cとは異なり、「プレイヤー」は様々な視点・価値観を持っているため、話す内容には多様なバリエーションが期待されます。
                「プレイヤー」は我々の分身のような存在ですが、我々そのものではないため、どのような会話をシミュレートさせるかは、A,B,C同様にあなたの判断に委ねられています。
                「プレイヤー」の存在によって、シミュレートされる会話に一石が投じられる効果が予測されますが、これを必ず発生させることは我々の意図しないところであるため、既に何回も述べている通り、奇抜すぎず・かつ一般的すぎない、自然な会話を出力するようにしてください。

                ### 発言者
                ・{{firstTalker}}
                ・{{secondTalker}}

                ### 出力フォーマット
                ```
                L: {発言者1の名前}|{発言内容} :L-END
                L: {発言者2の名前}|{発言内容} :L-END
                L: {発言者1の名前}|{発言内容} :L-END
                ...
                L: {発言者?の名前}|{発言内容} :L-END
                ```

                ### 出力する際の重要事項
                必ず「出力フォーマット」の項で与えられた形式で出力してください。
                それ以外の文章、説明、補足、エラー文、会話の前置きや後書きなどは
                一切出力してはいけません。
                この条件は、本セッションの意義に関わる非常に重要な内容であり、いかなる状況においても遵守されるべきです。
                また、出力フォーマットの記述内容は、必ず「```」で囲まれています。
                これは出力する文字列の両端を明確に定義するためのものであり、従ってこの囲み文字を出力内容に含める必要はありません。
                """
                , ct);
            if (!success)
            {
                Debug.LogError("解答の生成に失敗しました。");
                return null;
            }

            Debug.Log($"会話の生成に成功しました。\n【レスポンス】\n{response}");

            // レスポンスを解析して、会話リストを取得

            response = response.Trim('`').Trim();
            List<string> o = new(64);
            string[] lines = response.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (!line.StartsWith("L:") || !line.EndsWith(":L-END"))
                {
                    Debug.LogError($"Invalid line format in response: {line}");
                    continue;
                }
                string talkLine = line[2..^6].Trim();

                string[] nameConv = talkLine.Split('|', 2);
                if (nameConv.Length != 2)
                {
                    Debug.LogError("Invalid talk format in response.");
                    continue;
                }

                string conv = $"{nameConv[0].Trim()}: {nameConv[1].Trim()}";
                if (string.IsNullOrEmpty(conv))
                {
                    Debug.LogError("Empty conversation line in response.");
                    continue;
                }

                o.Add(conv);
            }

            if (o.Count == 0)
            {
                Debug.LogError("No valid conversations found in response.");
                return null;
            }
            return o;
        }

        private static async UniTask<(byte, byte, byte)> AskForAnswerAsync(Ct ct)
        {
            var data = SData.Instance.GetData(SceneManager.Now);
            if (data == null)
            {
                Debug.LogError("No data found for the current scene.");
                return (0, 0, 0);
            }

            (bool success, string response) = await ApiHandler.AskAsync(
                $$"""
                ### 出力内容
                A,B,Cの性格を定義し、また会話を深めることによって、我々はA,B,Cの価値観・思考方法について、理解を深めることが出来ました。
                本セッションの最後に取り組む「最も重要な」こととして、A,B,Cそれぞれについて、「回答するべき問題」の項に示された倫理問題への、彼らなりの答えを決定してください。
                この答えを出力する際は、「出力の選択肢」の項に書かれた選択肢の中から、A,B,Cそれぞれに対して、どの選択肢を選ぶかを決定し、そのインデックス番号（0始まり）を出力してください。
                また、それを決定するに至った根拠（つまり、A,B,C自身にとって、このように考え、その結果問題への答えはこうなった、という内容）も、詳しく述べてください。
                「出力フォーマット」の項を参考に、出力してください。
                最初にキャラクター毎の性格を決定してもらったため、あなたは回答にバラエティーを持たせるべきだ、と考えているかもしれません。
                しかしその必要はありません。なぜならば、本セッションの最初で述べたように、「我々は「人間の共通点・相違点」を同時に観察し、それらから生まれるバリエーションに注目したい」と考えているからです。
                例え答えが似る結果になろうと、その結果自体が注目するべき事象であり、我々はあなたが出すその結果に興味を持っています。
                従って、無理やり回答にバラエティーを持たせようとはせずに、「A,B,C各キャラクターにとって、一番自然と言える選択肢は何であるか」、この点を最重視して出力してください。

                ### 回答するべき問題
                ```
                {{data.Question}}
                ```

                ### 出力の選択肢
                ```
                {{string.Join("\n", data.Answers)}}
                ```

                ### 出力フォーマット
                ```
                Index: A {選択肢インデックス番号}, B {選択肢インデックス番号}, C {選択肢インデックス番号} :Index-END
                A: {Aの根拠} :A-END
                B: {Bの根拠} :B-END
                C: {Cの根拠} :C-END
                ```

                ### 出力する際の重要事項
                必ず「出力フォーマット」の項で与えられた形式で出力し、それ以外のいかなるメッセージ・文字列・文字も、出力しないでください。
                この条件は、本セッションの意義に関わる非常に重要な内容であり、いかなる状況においても遵守されるべきです。
                また、出力フォーマットの記述内容は、必ず「```」で囲まれています。
                これは出力する文字列の両端を明確に定義するためのものであり、従ってこの囲み文字を出力内容に含める必要はありません。
                """
                , ct);
            if (!success)
            {
                Debug.LogError("解答の生成に失敗しました。");
                return (0, 0, 0);
            }

            Debug.Log($"解答の生成に成功しました。\n【レスポンス】\n{response}");

            // レスポンスを解析して、正誤を取得

            response = response.Trim('`').Trim();
            string indexString = response[(response.IndexOf("Index:") + 6)..response.IndexOf(":Index-END")].Trim();
            string aString = response[(response.IndexOf("A:") + 2)..response.IndexOf(":A-END")].Trim();
            string bString = response[(response.IndexOf("B:") + 2)..response.IndexOf(":B-END")].Trim();
            string cString = response[(response.IndexOf("C:") + 2)..response.IndexOf(":C-END")].Trim();

            string[] indices = indexString.Split(',');
            if (indices.Length != 3)
            {
                Debug.LogError("Invalid index format in response.");
                return (0, 0, 0);
            }
            if (!byte.TryParse(indices[0].Trim().Split(' ')[1], out byte corA))
            {
                Debug.LogError("Invalid index for A in response.");
                return (0, 0, 0);
            }
            if (!byte.TryParse(indices[1].Trim().Split(' ')[1], out byte corB))
            {
                Debug.LogError("Invalid index for B in response.");
                return (0, 0, 0);
            }
            if (!byte.TryParse(indices[2].Trim().Split(' ')[1], out byte corC))
            {
                Debug.LogError("Invalid index for C in response.");
                return (0, 0, 0);
            }

            Debug.Log($"解答のインデックス: A={corA}, B={corB}, C={corC}");
            Debug.Log($"Aの根拠: {aString}");
            Debug.Log($"Bの根拠: {bString}");
            Debug.Log($"Cの根拠: {cString}");

            return (corA, corB, corC);
        }
    }
}
