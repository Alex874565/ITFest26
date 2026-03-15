using UnityEngine;
using System.Collections;

public class DifficultyLevelGameData
{
    public int Level { get; set; }
    public int CorrectAnswers { get; set; }
    public float AnswerTimeAverage { get; set; }
    
    public DifficultyLevelGameData(int level, int correctAnswers, float answerTimeAverage)
    {
        Level = level;
        CorrectAnswers = correctAnswers;
        AnswerTimeAverage = answerTimeAverage;
    }
    
    public Queue RecentAnswerTimes = new Queue();
}