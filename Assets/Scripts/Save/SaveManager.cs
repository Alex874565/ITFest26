using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private SaveData saveData;

    private readonly Dictionary<EquationType, int> equationLevels = new();
    private readonly Dictionary<EquationType, int> equationHighScores = new();
    private readonly Dictionary<UnlockableType, List<CosmeticState>> unlocks = new();

    public int Money => saveData != null ? saveData.Money : 0;

    // Expose Dictionary directly so your other scripts can keep using them as before
    public Dictionary<EquationType, int> EquationLevels => equationLevels;
    public Dictionary<EquationType, int> EquationHighScores => equationHighScores;
    public Dictionary<UnlockableType, List<CosmeticState>> Unlocks => unlocks;

    public List<EquationType> SelectedEquations
    {
        get
        {
            List<EquationType> selectedEquations = new();

            if (saveData == null || saveData.SelectedEquations == null)
                return selectedEquations;

            for (int i = 0; i < saveData.SelectedEquations.Count; i++)
            {
                if (saveData.SelectedEquations[i])
                    selectedEquations.Add((EquationType)i);
            }

            return selectedEquations;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private string GetSavePath()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string dir = "/idbfs/MyGameSave";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return Path.Combine(dir, "SaveData.json");
#else
        return Path.Combine(Application.persistentDataPath, "SaveData.json");
#endif
    }

    public void Load()
    {
        string path = GetSavePath();
        Debug.Log("Loading from: " + path);
        Debug.Log("persistentDataPath: " + Application.persistentDataPath);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            saveData = JsonUtility.FromJson<SaveData>(json);
        }

        if (saveData == null)
            saveData = new SaveData();

        ValidateSaveData();
        RebuildCaches();
    }

    public void Save(SaveData newSaveData)
    {
        saveData = newSaveData;

        if (saveData == null)
            saveData = new SaveData();

        ValidateSaveData();
        RebuildCaches();

        string json = JsonUtility.ToJson(saveData, true);
        string path = GetSavePath();

        File.WriteAllText(path, json);

        Debug.Log("Saved to: " + path);
        Debug.Log(json);
    }

    private void ValidateSaveData()
    {
        if (saveData.EquationLevels == null)
            saveData.EquationLevels = new List<int>();

        if (saveData.EquationHighScores == null)
            saveData.EquationHighScores = new List<int>();

        if (saveData.SelectedEquations == null)
            saveData.SelectedEquations = new List<bool>();

        if (saveData.Unlocks == null)
            saveData.Unlocks = new List<IntList>(); 
        // Replace IntArrayWrapperCompatible with your actual existing unlock list item type

        while (saveData.EquationLevels.Count < 4)
            saveData.EquationLevels.Add(0);

        while (saveData.EquationHighScores.Count < 4)
            saveData.EquationHighScores.Add(0);

        while (saveData.SelectedEquations.Count < 4)
            saveData.SelectedEquations.Add(false);

        int unlockableCount = System.Enum.GetValues(typeof(UnlockableType)).Length;
        while (saveData.Unlocks.Count < unlockableCount)
            saveData.Unlocks.Add(new IntList()); 
        // Replace with your actual existing type constructor

        for (int i = 0; i < saveData.Unlocks.Count; i++)
        {
            if (saveData.Unlocks[i] == null)
                saveData.Unlocks[i] = new IntList(); 
            // Replace with your actual existing type constructor

            if (saveData.Unlocks[i].Values == null)
                saveData.Unlocks[i].Values = new List<int>();
        }
    }

    private void RebuildCaches()
    {
        equationLevels.Clear();
        equationHighScores.Clear();
        unlocks.Clear();

        for (int i = 0; i < 4; i++)
        {
            EquationType type = (EquationType)i;
            equationLevels[type] = saveData.EquationLevels[i];
            equationHighScores[type] = saveData.EquationHighScores[i];
        }

        for (int i = 0; i < saveData.Unlocks.Count; i++)
        {
            UnlockableType unlockableType = (UnlockableType)i;
            List<CosmeticState> states = new();

            if (saveData.Unlocks[i] != null && saveData.Unlocks[i].Values != null)
            {
                foreach (int state in saveData.Unlocks[i].Values)
                {
                    states.Add((CosmeticState)state);
                }
            }

            unlocks[unlockableType] = states;
        }
    }

    public int GetEquationLevel(EquationType type)
    {
        return equationLevels.TryGetValue(type, out int value) ? value : 0;
    }

    public int GetEquationHighScore(EquationType type)
    {
        return equationHighScores.TryGetValue(type, out int value) ? value : 0;
    }

    public void SetEquationLevel(EquationType type, int value)
    {
        int index = (int)type;
        EnsureEquationIndex(index);

        saveData.EquationLevels[index] = value;
        equationLevels[type] = value;
    }

    public void SetEquationHighScore(EquationType type, int value)
    {
        int index = (int)type;
        EnsureEquationIndex(index);

        saveData.EquationHighScores[index] = value;
        equationHighScores[type] = value;
    }

    public bool IsEquationSelected(EquationType type)
    {
        int index = (int)type;

        if (saveData == null || saveData.SelectedEquations == null)
            return false;

        if (index < 0 || index >= saveData.SelectedEquations.Count)
            return false;

        return saveData.SelectedEquations[index];
    }

    public void SetEquationSelected(EquationType type, bool selected)
    {
        int index = (int)type;
        EnsureEquationIndex(index);

        saveData.SelectedEquations[index] = selected;
    }

    public void SetMoney(int value)
    {
        saveData.Money = value;
    }

    private void EnsureEquationIndex(int index)
    {
        while (saveData.EquationLevels.Count <= index)
            saveData.EquationLevels.Add(0);

        while (saveData.EquationHighScores.Count <= index)
            saveData.EquationHighScores.Add(0);

        while (saveData.SelectedEquations.Count <= index)
            saveData.SelectedEquations.Add(false);
    }
}