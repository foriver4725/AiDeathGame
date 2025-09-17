using UnityEngine;

public class Player_Move : MonoBehaviour
{
    public float moveSpeed = 5f;
    private CharaAI nearbyCharacter;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Character"))
        {
            nearbyCharacter = other.GetComponent<CharaAI>();
            if (nearbyCharacter != null)
            {
                Debug.Log($"キャラクター {nearbyCharacter.CharacterName} に近づきました。");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Character"))
        {
            if (nearbyCharacter != null && nearbyCharacter.gameObject == other.gameObject)
            {
                Debug.Log($"キャラクター {nearbyCharacter.CharacterName} から離れました。");
                nearbyCharacter = null;
            }
        }
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, vertical, 0);
        transform.position += movement * moveSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.E) && nearbyCharacter != null)
        {
            MG.Instance.StartConversationWithCharacter(nearbyCharacter.CharacterAI);
        }
    }
}
