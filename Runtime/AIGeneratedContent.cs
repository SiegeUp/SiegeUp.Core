using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace SiegeUp.Core
{
    public interface IReferenceable
    {
        string Name { get; }
        string Id { get; }
        string PrefabId { get => null; } 
        string Reference { get; }
    }

    public class BaseAIGeneratedContent : ScriptableObjectWithId, IReferenceable
    {
        [SerializeField, TextArea(10, 10)] public string prompt;
        [SerializeField] bool addSelfToPrompt;
        [SerializeField] List<UnityEngine.Object> relevantItems;

        public string Reference => $"{Serialize()}";
        public string Name => name;
        string IReferenceable.Id => Id;

        public virtual string GetPrompt()
        {
            StringBuilder accumulatedPrompt = new();
            accumulatedPrompt.AppendLine("Object references: \n");

            foreach (var item in relevantItems)
            {
                var referenceable = item as IReferenceable ?? (item as GameObject)?.GetComponent<IReferenceable>();
                if (referenceable != null)
                {
                    accumulatedPrompt.AppendLine($"{referenceable.Name}:");
                    accumulatedPrompt.AppendLine($"{referenceable.Reference}");
                    accumulatedPrompt.AppendLine("");
                }
            }

            accumulatedPrompt.AppendLine("Prompt: ");
            accumulatedPrompt.AppendLine(prompt);
            accumulatedPrompt.AppendLine("");

            if (addSelfToPrompt)
            {
                accumulatedPrompt.AppendLine($"{Name}:");
                accumulatedPrompt.AppendLine($"{Reference}");
            }

            return accumulatedPrompt.ToString();
        }

        public virtual void Deserialize(string json) { }
        public virtual string Serialize() { return "{}"; }

        protected virtual void OnEnable()
        {
            UpdateId();
        }
    }

    public class ValidationException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public ValidationException(string message, int line, int column) : base(message)
        {
            Line = line;
            Column = column;
        }

        public ValidationException(string message) : base(message)
        {
            Line = -1;
            Column = -1;
        }
    }

    public class AIGeneratedContent<T> : BaseAIGeneratedContent where T : struct
    {
        [SerializeField] Sprite icon;
        [SerializeField] T content;

        public T Content => content;
        public Sprite Icon => icon;

        public void SetContent(T content, Sprite icon)
        {
            this.content = content;
            this.icon = icon;
        }

        public override void Deserialize(string json)
        {
            T contentTmp = JsonUtility.FromJson<T>(json);
            VerifyOrThrow(contentTmp, null);
            content = contentTmp;
        }

        public override string Serialize()
        {
            return JsonUtility.ToJson(content);
        }

        public virtual void VerifyOrThrow(T content, Dictionary<object, (int line, int column)> sourceMap) { }

        protected void ThrowValidation(string message, Dictionary<object, (int line, int column)> sourceMap, object key)
        {
            if (sourceMap != null && sourceMap.TryGetValue(key, out var pos))
                throw new ValidationException(message, pos.line, pos.column);
            else
                throw new ValidationException(message);
        }
    }
}
