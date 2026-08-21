#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    public enum MinigameResolutionState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }

    [Serializable]
    public sealed class MinigameAnswerRecord
    {
        [SerializeField] private string _questionId;
        [SerializeField] private string _answer;
        [SerializeField] private string _expectedAnswer;
        [SerializeField] private bool _isCorrect;
        [SerializeField] private int _attempt;
        [SerializeField] private float _time;

        public string QuestionId => _questionId;
        public string Answer => _answer;
        public string ExpectedAnswer => _expectedAnswer;
        public bool IsCorrect => _isCorrect;
        public int Attempt => _attempt;
        public float Time => _time;

        public MinigameAnswerRecord(
            string questionId,
            string answer,
            string expectedAnswer,
            bool isCorrect,
            int attempt,
            float time)
        {
            _questionId = questionId;
            _answer = answer;
            _expectedAnswer = expectedAnswer;
            _isCorrect = isCorrect;
            _attempt = Mathf.Max(1, attempt);
            _time = time;
        }

        public MinigameAnswerRecord()
        {
        }
    }

    [Serializable]
    public sealed class MinigameStateRecord
    {
        [SerializeField] private string _minigameId = "minigame";
        [SerializeField] private MinigameResolutionState _resolutionState = MinigameResolutionState.NotStarted;
        [SerializeField] private List<MinigameAnswerRecord> _answers = new();

        public string MinigameId => _minigameId;
        public MinigameResolutionState ResolutionState => _resolutionState;
        public IReadOnlyList<MinigameAnswerRecord> Answers => _answers;

        public MinigameStateRecord(string minigameId)
        {
            _minigameId = minigameId;
        }

        public MinigameStateRecord()
        {
        }

        public void SetResolutionState(MinigameResolutionState state)
        {
            _resolutionState = state;
        }

        public MinigameAnswerRecord AddAnswer(string questionId, string answer, string expectedAnswer, bool isCorrect)
        {
            int attempt = GetAttemptCount(questionId) + 1;
            MinigameAnswerRecord record = new(
                questionId,
                answer,
                expectedAnswer,
                isCorrect,
                attempt,
                Application.isPlaying ? Time.time : 0f);

            _answers.Add(record);
            return record;
        }

        private int GetAttemptCount(string questionId)
        {
            int count = 0;
            for (int i = 0; i < _answers.Count; i++)
            {
                if (string.Equals(_answers[i].QuestionId, questionId, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }

    public sealed class MinigameObjectState : MonoBehaviour
    {
        [SerializeField] private string _objectId;
        [SerializeField] private List<MinigameStateRecord> _minigames = new();

        public string ObjectId => string.IsNullOrWhiteSpace(_objectId) ? gameObject.name : _objectId;
        public IReadOnlyList<MinigameStateRecord> Minigames => _minigames;

        public MinigameStateRecord GetOrCreateRecord(string minigameId)
        {
            string normalizedId = NormalizeMinigameId(minigameId);

            if (TryGetRecord(normalizedId, out MinigameStateRecord existingRecord))
            {
                return existingRecord;
            }

            MinigameStateRecord record = new(normalizedId);
            _minigames.Add(record);
            return record;
        }

        public bool TryGetRecord(string minigameId, out MinigameStateRecord record)
        {
            string normalizedId = NormalizeMinigameId(minigameId);

            for (int i = 0; i < _minigames.Count; i++)
            {
                if (string.Equals(_minigames[i].MinigameId, normalizedId, StringComparison.OrdinalIgnoreCase))
                {
                    record = _minigames[i];
                    return true;
                }
            }

            record = null;
            return false;
        }

        public MinigameResolutionState GetResolutionState(string minigameId)
        {
            return TryGetRecord(minigameId, out MinigameStateRecord record)
                ? record.ResolutionState
                : MinigameResolutionState.NotStarted;
        }

        public bool IsCompleted(string minigameId)
        {
            return GetResolutionState(minigameId) == MinigameResolutionState.Completed;
        }

        public void SetResolutionState(string minigameId, MinigameResolutionState state)
        {
            GetOrCreateRecord(minigameId).SetResolutionState(state);
            Debug.Log($"[Minigames] '{ObjectId}' -> '{NormalizeMinigameId(minigameId)}' state: {state}.", this);
        }

        public MinigameAnswerRecord RecordAnswer(
            string minigameId,
            string questionId,
            string answer,
            bool isCorrect,
            string expectedAnswer = "")
        {
            MinigameStateRecord record = GetOrCreateRecord(minigameId);
            MinigameAnswerRecord answerRecord = record.AddAnswer(
                string.IsNullOrWhiteSpace(questionId) ? "answer" : questionId,
                answer ?? string.Empty,
                expectedAnswer ?? string.Empty,
                isCorrect);

            if (record.ResolutionState == MinigameResolutionState.NotStarted)
            {
                record.SetResolutionState(MinigameResolutionState.InProgress);
            }

            Debug.Log(
                $"[Minigames] '{ObjectId}' saved answer '{answerRecord.QuestionId}' for '{record.MinigameId}'. Correct: {answerRecord.IsCorrect}. Attempt: {answerRecord.Attempt}.",
                this);

            return answerRecord;
        }

        private static string NormalizeMinigameId(string minigameId)
        {
            return string.IsNullOrWhiteSpace(minigameId) ? "minigame" : minigameId.Trim();
        }
    }
}
