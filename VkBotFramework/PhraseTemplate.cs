using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VkNet.Model;

namespace VkBotFramework
{
    public partial class VkBot
    {
        private class PhraseTemplate
        {
            public readonly List<string> Answers;
            public readonly Action<Message> Callback;

            public readonly string PhraseRegexPattern;
            public readonly RegexOptions PhraseRegexPatternOptions;

            public PhraseTemplate(string phraseRegexPattern, string answer, RegexOptions phraseRegexPatternOptions)
            {
                PhraseRegexPattern = phraseRegexPattern;
                Answers = new List<string>();
                Answers.Add(answer);
                PhraseRegexPatternOptions = phraseRegexPatternOptions;
            }


            public PhraseTemplate(string phraseRegexPattern, List<string> answers,
                RegexOptions phraseRegexPatternOptions)
            {
                PhraseRegexPattern = phraseRegexPattern;
                Answers = answers;
                PhraseRegexPatternOptions = phraseRegexPatternOptions;
            }

            public PhraseTemplate(string phraseRegexPattern, Action<Message> callback,
                RegexOptions phraseRegexPatternOptions)
            {
                PhraseRegexPattern = phraseRegexPattern;
                PhraseRegexPatternOptions = phraseRegexPatternOptions;
                Callback = callback;
            }
        }
    }
}