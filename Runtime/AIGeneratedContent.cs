using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace SiegeUp.Core
{
    public interface IReferenceable
    {
        string Name { get; }
        string Id { get; }
        string Reference { get; }
    }

    public class BaseAIGeneratedContent : ScriptableObjectWithId, IReferenceable
    {
        [SerializeField, TextArea(10, 10)] public string prompt;
        [SerializeField, HideInInspector] string lastJson;
        [SerializeField] List<Object> relevantItems;

        public string Json
        {
            get => Serialize();
            set
            {
                Deserialize(lastJson);
                lastJson = value;
            }
        }

        public string Reference => $"{lastJson}";
        public string Name => name;
        string IReferenceable.Id => Id;

        public virtual string GetPrompt()
        {
            StringBuilder accumulatedPrompt = new();
            accumulatedPrompt.AppendLine("References: ");
            foreach (var item in relevantItems)
            {
                var referenceable = item as IReferenceable ?? (item as GameObject)?.GetComponent<IReferenceable>();
                if (referenceable != null)
                {
                    accumulatedPrompt.AppendLine($"{referenceable.Name}:");
                    accumulatedPrompt.AppendLine($"{referenceable.Reference}");
                }
            }
            accumulatedPrompt.AppendLine("Prompt: ");
            accumulatedPrompt.AppendLine(prompt);
            return accumulatedPrompt.ToString();
        }

        public virtual void Deserialize(string json) { }
        public virtual string Serialize() { return "{}"; }

        protected virtual void OnEnable()
        {
            UpdateId();
        }
    }

    public class AIGeneratedContent<T> : BaseAIGeneratedContent where T : struct
    {
        [SerializeField] T content;

        public T Content => content;

        public override void Deserialize(string json)
        {
            T contentTmp = JsonUtility.FromJson<T>(json);
            VerifyOrThrow(contentTmp);
            content = contentTmp;
        }

        public override string Serialize()
        {
            return JsonUtility.ToJson(content);
        }

        public virtual void VerifyOrThrow(T content) {}
    }
}
