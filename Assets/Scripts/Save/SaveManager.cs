using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public SaveData SaveData;
    
    public int Money => SaveData.Money;
    public Dictionary<EquationType, int> EquationLevels => new Dictionary<EquationType, int>
    {
        { EquationType.Addition, SaveData.EquationLevels[0] },
        { EquationType.Subtraction, SaveData.EquationLevels[1] },
        { EquationType.Multiplication, SaveData.EquationLevels[2] },
        { EquationType.Division, SaveData.EquationLevels[3] }
    };
    public Dictionary<EquationType, int> EquationHighScores => new Dictionary<EquationType, int>
    {
        { EquationType.Addition, SaveData.EquationHighScores[0] },
        { EquationType.Subtraction, SaveData.EquationHighScores[1] },
        { EquationType.Multiplication, SaveData.EquationHighScores[2] },
        { EquationType.Division, SaveData.EquationHighScores[3] }
    };

    public List<EquationType> SelectedEquations
    {
        get
        {
            List<EquationType> selectedEquations = new List<EquationType>();

            //for (int i = 0; i < SaveData.SelectedEquations.Count; i++)
            //{
             //   if (SaveData.SelectedEquations[i])
             //       selectedEquations.Add((EquationType)i);
            //}

            //return selectedEquations;
            return new List<EquationType>()
            {
                EquationType.Addition,
                EquationType.Subtraction,
                EquationType.Multiplication,
                EquationType.Division,
            };
        }
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Load();
    }
    
    public void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, "SaveData.json");

        if (File.Exists(path))
        {
            Debug.Log("Save loaded");
            string json = File.ReadAllText(path);
            SaveData = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            SaveData = new SaveData();
        }
    }

    public void Save(SaveData saveData)
    {
        SaveData = saveData;
        
        string json = JsonUtility.ToJson(SaveData, true);

        string path = Path.Combine(Application.persistentDataPath, "SaveData.json");

        File.WriteAllText(path, json);

        Debug.Log("Saved to: " + path);
    }
}