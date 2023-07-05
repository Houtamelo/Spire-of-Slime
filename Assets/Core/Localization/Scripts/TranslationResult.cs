using System;
using System.Diagnostics.Contracts;
using System.Text;
using Core.Utils.Extensions;
using UnityEngine;

namespace Core.Localization.Scripts
{
    public readonly struct TranslationResult
    {
        private static readonly StringBuilder Builder = new();
        
        public int Substitutions { get; init; }
        
        private string _Text { get; init; }
        public string Text
        {
            init => _Text = value;
        }

        public TranslationResult(string text, int substitutions)
        {
            _Text = text;
            Substitutions = substitutions;
        }
        
        public static TranslationResult Empty => new(string.Empty, 0);

        public bool IsSome() => _Text.IsSome();
        public bool IsNone() => _Text.IsNone();

        [Pure]
        public string GetText()
        {
            if (Substitutions != 0)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result."
                               + $"Translation: {_Text}");
            }
            
            return _Text;
        }

        [Pure]
        public string GetText(string substitution)
        {
            if (Substitutions != 1)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            return _Text.Replace(TranslationDatabase.Substitutions[0], substitution);
        }
        
        [Pure]
        public string GetText(string substitution_1, string substitution_2)
        {
            if (Substitutions != 2)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            Builder.Override(_Text);
            Builder.Replace(TranslationDatabase.Substitutions[0], substitution_1);
            Builder.Replace(TranslationDatabase.Substitutions[1], substitution_2);
            return Builder.ToString();
        }
        
        [Pure]
        public string GetText(string substitution_1, string substitution_2, string substitution_3)
        {
            if (Substitutions != 3)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            Builder.Override(_Text);
            Builder.Replace(TranslationDatabase.Substitutions[0], substitution_1);
            Builder.Replace(TranslationDatabase.Substitutions[1], substitution_2);
            Builder.Replace(TranslationDatabase.Substitutions[2], substitution_3);
            return Builder.ToString();
        }
        
        [Pure]
        public string GetText(string substitution_1, string substitution_2, string substitution_3, string substitution_4)
        {
            if (Substitutions != 4)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            Builder.Override(_Text);
            Builder.Replace(TranslationDatabase.Substitutions[0], substitution_1);
            Builder.Replace(TranslationDatabase.Substitutions[1], substitution_2);
            Builder.Replace(TranslationDatabase.Substitutions[2], substitution_3);
            Builder.Replace(TranslationDatabase.Substitutions[3], substitution_4);
            return Builder.ToString();
        }
        
        [Pure]
        public string GetText(string substitution_1, string substitution_2, string substitution_3, string substitution_4, string substitution_5)
        {
            if (Substitutions != 5)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            Builder.Override(_Text);
            Builder.Replace(TranslationDatabase.Substitutions[0], substitution_1);
            Builder.Replace(TranslationDatabase.Substitutions[1], substitution_2);
            Builder.Replace(TranslationDatabase.Substitutions[2], substitution_3);
            Builder.Replace(TranslationDatabase.Substitutions[3], substitution_4);
            Builder.Replace(TranslationDatabase.Substitutions[4], substitution_5);
            return Builder.ToString();
        }
        
        [Pure]
        public string GetText(string substitution_1, string substitution_2, string substitution_3, string substitution_4, string substitution_5, string substitution_6)
        {
            if (Substitutions != 6)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            Builder.Override(_Text);
            Builder.Replace(TranslationDatabase.Substitutions[0], substitution_1);
            Builder.Replace(TranslationDatabase.Substitutions[1], substitution_2);
            Builder.Replace(TranslationDatabase.Substitutions[2], substitution_3);
            Builder.Replace(TranslationDatabase.Substitutions[3], substitution_4);
            Builder.Replace(TranslationDatabase.Substitutions[4], substitution_5);
            Builder.Replace(TranslationDatabase.Substitutions[5], substitution_6);
            return Builder.ToString();
        }
        
        [Pure]
        public string GetText(string substitution_1, string substitution_2, string substitution_3, string substitution_4, string substitution_5, string substitution_6, string substitution_7)
        {
            if (Substitutions != 7)
            {
                Debug.LogWarning($"Substitutions length ({Substitutions}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }
            
            Builder.Override(_Text);
            Builder.Replace(TranslationDatabase.Substitutions[0], substitution_1);
            Builder.Replace(TranslationDatabase.Substitutions[1], substitution_2);
            Builder.Replace(TranslationDatabase.Substitutions[2], substitution_3);
            Builder.Replace(TranslationDatabase.Substitutions[3], substitution_4);
            Builder.Replace(TranslationDatabase.Substitutions[4], substitution_5);
            Builder.Replace(TranslationDatabase.Substitutions[5], substitution_6);
            Builder.Replace(TranslationDatabase.Substitutions[6], substitution_7);
            return Builder.ToString();
        }
        
        [Pure]
        public string GetText(params string[] substitutions)
        {
            ReadOnlySpan<string> span = new(substitutions);
            return GetText(ref span);
        }

        [Pure]
        public string GetText(ref ReadOnlySpan<string> substitutions)
        {
            if (substitutions.Length != Substitutions)
            {
                Debug.LogWarning($"Substitutions length ({substitutions.Length}) does not match the number of substitutions ({Substitutions}) in the translation result, refusing to perform substitutions."
                               + $"Translation: {_Text}");
                
                return _Text;
            }

            Builder.Override(_Text);
            
            for (int i = 0; i < Substitutions; i++)
                Builder.Replace(TranslationDatabase.Substitutions[i], substitutions[i]);

            return Builder.ToString();
        }
    }
}