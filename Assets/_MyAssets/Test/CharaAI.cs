using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharaAI : MonoBehaviour
{
    [SerializeField] private AIs_Chara AIs_Chara;

    public string CharacterName => AIs_Chara.CharacterName;
    public AIs_Chara CharacterAI => AIs_Chara;

    private void Awake()
    {
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"キャラクターオブジェクト {gameObject.name} に Collider2D が見つかりません。");
        }

        gameObject.tag = "Character";
    }
}
