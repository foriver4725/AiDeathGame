using System.Collections.Generic;
using MyScripts.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using System.Threading;
using UnityEngine.TextCore.Text;

public class MG : MonoBehaviour
{
    public static MG Instance { get; private set; }

    [SerializeField] private Canvas talkCanvas;
    [SerializeField] private TextMeshProUGUI talkText;

    [SerializeField] private AIs_Chara characterA;
    [SerializeField] private AIs_Chara characterB;
    [SerializeField] private AIs_Chara characterC;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FlowAsync(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid FlowAsync(CancellationToken ct)
    {
        await UniTask.WhenAll(
            SetPersonalityForCharacter(characterA, ct),
            SetPersonalityForCharacter(characterB, ct),
            SetPersonalityForCharacter(characterC, ct)
        );

        talkCanvas.gameObject.SetActive(false);
    }

    private async UniTask SetPersonalityForCharacter(AIs_Chara character, CancellationToken ct)
    {
        // ここではキャラクターAIのPersonalityPromptをセットアップするだけ
        // 実際のセッションは会話開始時に行われる
        Debug.Log($"キャラクター {character.CharacterName} の性格プロンプトを準備しました。");
        // NOTE: 元の設計ではこの時点でLLMにリクエストを送っていましたが、ここではUI表示を待つため、性格定義のプロンプトを個別に送る必要はありません。
    }

    public async UniTask StartConversationWithCharacter(AIs_Chara character)
    {
        await StartConversationWithCharacter(character, "プレイヤー", destroyCancellationToken);
    }

    private async UniTask StartConversationWithCharacter(AIs_Chara character, string otherCharacterName, CancellationToken ct)
    {
        // 新しいキャラクターと会話を始める前に、セッションをリセットし性格を再定義
        await character.SetPersonalityAsync(ct);

        List<string> talkList = await character.GenerateConversationAsync(otherCharacterName, ct);
        if (talkList != null)
        {
            talkCanvas.gameObject.SetActive(true);
            foreach (string talk in talkList)
            {
                ulong colorHex = talk[0] switch
                {
                    'A' => 0xCF3030,
                    'B' => 0xB0CF3A,
                    'C' => 0x3B82B9,
                    _ => 0xFFC700
                };
                Color color = new Color32(
                    (byte)(colorHex >> 16),
                    (byte)(colorHex >> 8 & 0xFF),
                    (byte)(colorHex & 0xFF),
                    0xFF);

                int spaceIndex = talk.IndexOf(' ');
                if (spaceIndex >= 0)
                    talkText.text = talk.Substring(spaceIndex + 1);
                talkText.color = color;

                await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) || Input.touchCount > 0);
            }
            talkCanvas.gameObject.SetActive(false);
        }
    }
}