using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using Core.Save_Management.SaveObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Localization.Scripts
{
    [Serializable, DataContract, InlineProperty]
    public struct LocalizedText
    {
        [SerializeField, DataMember, HideLabel]
        private CleanString key;
        public CleanString Key => key;

        public LocalizedText(string key) => this.key = new CleanString(key);
        public LocalizedText(CleanString key) => this.key = key;

        [Pure]
        public TranslationResult Translate() => key.IsSome() ? TranslationDatabase.Get(key) : TranslationResult.Empty;

        public override string ToString()
        {
            Debug.LogWarning("Do not call ToString() directly on LocalizedText. Use Translate().GetText() instead.");
            return Translate().GetText();
        }
        
        public static readonly LocalizedText Empty = new(string.Empty);
    }
}