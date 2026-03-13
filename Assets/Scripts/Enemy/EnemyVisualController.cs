using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    private EquationData _equationData;
    
    public void ResetValues(EquationData data)
    {
        _equationData = data;
    }
}