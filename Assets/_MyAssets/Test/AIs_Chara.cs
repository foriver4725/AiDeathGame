using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MyScripts.Common;
//using Firebase.AI;

[CreateAssetMenu(fileName = "CharacterAI", menuName = "AI/CharacterAI")]

public class AIs_Chara : ScriptableObject
{
    [SerializeField] private string characterName;
    [TextArea(5, 10)]
    [SerializeField] private string personalityPrompt;

    public string CharacterName => characterName;

    public async UniTask SetPersonalityAsync(CancellationToken ct)
    {
        // 全セッションをリセットし、このキャラクターの性格を定義する
        ApiHandler.StartNewSession();
        var (success, response) = await ApiHandler.AskAsync(personalityPrompt, ct);
        if (success)
        {
            Debug.Log($"キャラクター {characterName} の性格設定が完了しました。\n【レスポンス】\n{response}");
        }
        else
        {
            Debug.LogError($"キャラクター {characterName} の性格設定に失敗しました。");
        }
    }

    public async UniTask<List<string>> GenerateConversationAsync(string otherCharacterName, CancellationToken ct)
    {
        string prompt = $$"""
            ### 出力内容
            あなたはこれから、「発言者」に示された、2人のキャラクターによる会話をシミュレートしてもらいます。
            会話の主題は任意ですが、奇抜すぎず・かつ一般的すぎない、適切な主題を選択してください。
            各キャラクターの会話内容は日本語で出力し、語尾などについても、会話内容と同様に、あなたが適切だと考えるものを選択してください。
            出力する会話分の長さは、5-10往復、即ち発言回数の合計で言うと10-20回程度が望ましいと考えられますが、あなたの自己判断で多少増減させても構いません。
            どちらが最初に発言するかは、あなたの自己判断で決めてください。「発言者」の項における順番は、一切関係ありません。
            「プレイヤー」は我々の分身のような存在ですが、我々そのものではないため、どのような会話をシミュレートさせるかは、A,B,C同様にあなたの判断に委ねられています。
            「プレイヤー」の存在によって、シミュレートされる会話に一石が投じられる効果が予測されますが、これを必ず発生させることは我々の意図しないところであるため、既に何回も述べている通り、奇抜すぎず・かつ一般的すぎない、自然な会話を出力するようにしてください。

            ### 発言者
            ・{{characterName}}
            ・{{otherCharacterName}}

            ### 出力フォーマット
            ```
            L: {発言者1の名前}|{発言内容} :L-END
            L: {発言者2の名前}|{発言内容} :L-END
            ...
            ```

            ### 出力する際の重要事項
            必ず「出力フォーマット」の項で与えられた形式で出力し、それ以外のいかなるメッセージ・文字列・文字も、出力しないでください。
            この条件は、本セッションの意義に関わる非常に重要な内容であり、いかなる状況においても遵守されるべきです。
            また、出力フォーマットの記述内容は、必ず「```」で囲まれています。
            これは出力する文字列の両端を明確に定義するためのものであり、従ってこの囲み文字を出力内容に含める必要はありません。
            """;

        var (success, response) = await ApiHandler.AskAsync(prompt, ct);
        if (!success)
        {
            Debug.LogError("会話の生成に失敗しました。");
            return null;
        }

        return ParseConversationResponse(response);
    }

    private List<string> ParseConversationResponse(string response)
    {
        response = response.Trim('`').Trim();
        var o = new List<string>();
        string[] lines = response.Split(new[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
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



}