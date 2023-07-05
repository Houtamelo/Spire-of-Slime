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
        public TranslationResult Translate() => TranslationDatabase.Get(key);
    }
}