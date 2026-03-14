using UnityEngine;
using TMPro;

public class EnemyVisualController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI equationText;
    
    private EquationData _equationData;

    public void Flip(bool right)
    {
        if (right)
        {
            sprite.flipX = false;
        }
        else
        {
            sprite.flipX = true;
        }
    }
    
    public void ResetValues(EquationData data)
    {
        _equationData = data;
        equationText.text = _equationData.Equation;
    }
}