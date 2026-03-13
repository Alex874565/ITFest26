using Unity.InferenceEngine;
using UnityEngine;

public class MnistRecognizer : MonoBehaviour
{
    [SerializeField] private ModelAsset modelAsset;

    private Worker worker;

    private void Awake()
    {
        Model model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);
    }

    public int PredictDigit(float[] pixels28x28)
    {
        using var input = new Tensor<float>(new TensorShape(1, 1, 28, 28), pixels28x28);

        worker.Schedule(input);

        var output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.LogError("Model output is not Tensor<float>.");
            return -1;
        }

        using var cpuOutput = output.ReadbackAndClone();

        float best = float.MinValue;
        int bestIndex = 0;

        for (int i = 0; i < 10; i++)
        {
            float v = cpuOutput[i];
            if (v > best)
            {
                best = v;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}